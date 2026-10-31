using System.Security.Claims;
using System.Text;
using Flowtr.Client.Features.Inbox;
using Flowtr.Server.Features.Audit.Queries;
using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Flowtr.Shared.Users;
using Flowtr.Tests.Journal;
using Flowtr.Tests.Receipts;
using Microsoft.AspNetCore.Http;
using static Flowtr.Tests.Inbox.MailTestData;

namespace Flowtr.Tests.Inbox;

/// <summary>
/// Story #010: the inbox in the journal. Its receipts are not bookings until the user saves them.
/// </summary>
public class InboxTests
{
    [Fact]
    public async Task Analysis_stores_the_proposal_with_the_origin_as_hint()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(subject: "Rechnung 4711", attachments: ("azure.pdf", ReceiptTestData.Pdf())));
        await app.FetchOk();
        app.Reader.Next = Result.Success(new ReceiptReading("2026-10-01", "Expense", 64.20m, "CHF", "Azure Abo", "Microsoft", null, null));

        // Act
        var analyzed = await app.Command<AnalyzeInbox, Result<int>>(new AnalyzeInbox());

        // Assert
        analyzed.Value.ShouldBe(1);
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        item.Analysis.ShouldBe(AnalysisState.Done);
        (item.Proposal!.AmountChf, item.Proposal.Counterparty).ShouldBe((64.20m, "Microsoft"));
        app.Reader.LastContext!.Origin.ShouldBe($"E-mail from {User}, subject \"Rechnung 4711\", file \"azure.pdf\"");
    }

    [Fact]
    public async Task Mail_as_receipt_goes_to_the_ai_as_text_without_markup_noise()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(subject: "Your receipt", html: StripeHtml, text: null));
        await app.FetchOk();
        app.Reader.Next = Result.Failure<ReceiptReading>("Die KI ist nicht erreichbar. Bitte fülle die Felder selbst aus.");

        // Act
        await app.Command<AnalyzeInbox, Result<int>>(new AnalyzeInbox());

        // Assert
        app.Reader.LastContentType.ShouldBe("text/plain");
        var text = Encoding.UTF8.GetString(app.Reader.LastContent!);
        text.ShouldContain("Subject: Your receipt");
        text.ShouldContain("<table><tr><td>Betrag</td>");
        text.ShouldContain("<td>CHF 33.00</td>");
        text.ShouldNotContain("alert");
        text.ShouldNotContain("color: red");
        text.ShouldNotContain("onclick");
        text.ShouldNotContain("pixel.gif");
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        (item.Analysis, item.AnalysisError).ShouldBe((AnalysisState.Failed, "Die KI ist nicht erreichbar. Bitte fülle die Felder selbst aus."));
    }

    [Fact]
    public async Task Booking_files_the_receipt_and_takes_it_from_the_inbox()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(subject: "Stripe", html: StripeHtml, text: null));
        await app.FetchOk();
        var item = (await app.Inbox()).Items.Single();

        // Act
        await app.Create(JournalTestData.Expense(description: "Stripe Gebühren") with { UploadId = item.UploadId });

        // Assert
        (await app.Inbox()).Items.ShouldBeEmpty();
        var booking = (await app.Query<GetJournal, JournalView>(new GetJournal(new JournalFilter(2026)))).Value!.Bookings.Single();
        (booking.ReceiptNumber, booking.ReceiptContentType).ShouldBe(("A-2026-001", MailContentTypes.Mail));
        app.Store.Receipts.Keys.ShouldBe(["2026/ausgaben/A-2026-001_stripe-gebuehren.eml"]);
        app.Store.Uploads.ShouldBeEmpty();
    }

    [Fact]
    public async Task Inbox_receipts_do_not_count_in_the_journal()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));
        await app.FetchOk();
        await app.Command<AnalyzeInbox, Result<int>>(new AnalyzeInbox());

        // Act
        var journal = (await app.Query<GetJournal, JournalView>(new GetJournal(new JournalFilter(2026)))).Value!;

        // Assert
        journal.Bookings.ShouldBeEmpty();
        (journal.Income, journal.Expense).ShouldBe((0m, 0m));
    }

    [Fact]
    public async Task Discard_removes_the_receipt_and_its_file()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(attachments: ("werbung.pdf", ReceiptTestData.Pdf())));
        await app.FetchOk();
        var item = (await app.Inbox()).Items.Single();

        // Act
        var result = await app.Command<DiscardInboxItem, Result>(new DiscardInboxItem(item.Id));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await app.Inbox()).Items.ShouldBeEmpty();
        app.Store.Uploads.ShouldBeEmpty();
    }

    [Fact]
    public async Task Receipts_expire_after_30_days()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(attachments: ("alt.pdf", ReceiptTestData.Pdf("alt"))));
        await app.FetchOk();
        app.Clock.Now = app.Clock.Now.AddDays(29);
        app.Mailbox.Receive(Mail(attachments: ("neu.pdf", ReceiptTestData.Pdf("neu"))));
        await app.FetchOk();
        app.Clock.Now = app.Clock.Now.AddDays(1);

        // Act
        var expired = await app.Command<ExpireInboxItems, Result<int>>(new ExpireInboxItems());

        // Assert
        expired.Value.ShouldBe(1);
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        item.FileName.ShouldBe("neu.pdf");
        item.ExpiresAt.ShouldBe(app.Clock.Now.AddDays(29));
        app.Store.Uploads.Keys.ShouldBe([item.UploadId]);
    }

    [Fact]
    public async Task Import_rejection_and_discard_are_in_the_audit_log()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(Mail(subject: "Rechnung", attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));
        app.Mailbox.Receive(Mail(from: "spam@example.com", subject: "Gewinn!", attachments: ("spam.pdf", ReceiptTestData.Pdf("spam"))));
        await app.FetchOk();

        // Act
        await app.Command<DiscardInboxItem, Result>(new DiscardInboxItem((await app.Inbox()).Items.Single().Id));

        // Assert
        var entries = (await app.Query<GetAuditEntries, AuditPage>(new GetAuditEntries(new AuditFilter(null, null, null, null)))).Value!.Entries;
        entries.Select(e => (e.Action, e.Area, e.Subject)).ShouldBe(
        [
            ("Gelöscht", "Eingang", "rechnung.pdf"),
            ("Erstellt", "E-Mail", "Gewinn!"),
            ("Erstellt", "Eingang", "rechnung.pdf"),
            ("Erstellt", "E-Mail", "Rechnung"),
            ("Erstellt", "Erlaubter Absender", User),
        ], ignoreOrder: true);
        var rejected = entries.Single(e => e.Subject == "Gewinn!");
        rejected.Changes.ShouldContain(new AuditChangeView("Ergebnis", null, "Abgelehnt"));
        rejected.Changes.ShouldContain(new AuditChangeView("Grund", null, "Absender nicht erlaubt"));
        entries.First(e => e.Area == "Eingang").Changes.ShouldContain(new AuditChangeView("E-Mail", "Rechnung", null));
    }

    [Theory]
    [InlineData("rechnungen@firma.ch", true)]
    [InlineData("@firma.ch", true)]
    [InlineData("firma.ch", false)]
    [InlineData("a@b", false)]
    [InlineData("zwei@@firma.ch", false)]
    public async Task Allowed_sender_is_an_address_or_a_domain(string address, bool valid)
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var result = await app.Command<AddAllowedSender, Result<Guid>>(new AddAllowedSender(new SenderInput(address)));

        // Assert
        result.IsSuccess.ShouldBe(valid);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(Roles.Contributor, true, false)]
    [InlineData(Roles.Admin, true, true)]
    public async Task Roles_decide_about_inbox_and_senders(string? role, bool inbox, bool senders)
    {
        // Arrange
        using var app = new TestApp();
        app.Get<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                role is null ? [new Claim(ClaimTypes.Name, "anna@example.ch")] : [new Claim(ClaimTypes.Name, "anna@example.ch"), new Claim(ClaimTypes.Role, role)],
                "Test")),
        };
        var api = app.Get<IInboxApi>();

        // Act
        var inboxResult = await api.GetInbox();
        var sendersResult = await api.GetSenderSettings();

        // Assert
        inboxResult.IsSuccess.ShouldBe(inbox);
        sendersResult.IsSuccess.ShouldBe(senders);
    }
}
