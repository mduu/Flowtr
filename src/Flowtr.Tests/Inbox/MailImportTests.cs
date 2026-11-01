using Flowtr.Server.Features.Inbox;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Inbox.Model;
using Flowtr.Server.Features.Inbox.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Tests.Receipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using static Flowtr.Tests.Inbox.MailTestData;

namespace Flowtr.Tests.Inbox;

/// <summary>
/// Story #010: Flowtr fetches forwarded mails and puts their receipts into the inbox (ADR-009).
/// </summary>
public class MailImportTests
{
    private const string Processed = "Flowtr/Verarbeitet";

    private const string Rejected = "Flowtr/Abgelehnt";

    [Fact]
    public async Task Pdf_attachment_of_a_user_comes_into_the_inbox()
    {
        // Arrange
        using var app = new TestApp();
        await app.Invite(User);
        var pdf = ReceiptTestData.Pdf("Azure");
        app.Mailbox.Receive(Mail(subject: "WG: Ihre Azure-Rechnung", text: "---------- Forwarded message ---------\nVon: Microsoft <billing@microsoft.com>\nBetreff: Rechnung",
            attachments: ("rechnung.pdf", pdf)));

        // Act
        var result = await app.FetchOk();

        // Assert
        result.ShouldBe(new FetchResult(1, 0));
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        var file = item.Files.ShouldHaveSingleItem();
        (file.FileName, file.ContentType, item.Sender, item.OriginalSender, item.Subject, item.Analysis)
            .ShouldBe(("rechnung.pdf", "application/pdf", User, "Microsoft <billing@microsoft.com>", "WG: Ihre Azure-Rechnung", AnalysisState.Pending));
        app.Store.Uploads[file.UploadId].Content.ShouldBe(pdf);
        app.Mailbox.Inbox.ShouldBeEmpty();
        app.Mailbox.Folder(Processed).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Html_mail_without_attachment_is_the_receipt_itself()
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(subject: "Your receipt from Stripe", html: StripeHtml, text: null));

        // Act
        await app.FetchOk();

        // Assert
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        var file = item.Files.ShouldHaveSingleItem();
        (file.FileName, file.ContentType).ShouldBe(("your-receipt-from-stripe.eml", MailContentTypes.Mail));
        System.Text.Encoding.UTF8.GetString(app.Store.Uploads[file.UploadId].Content).ShouldContain("Subject: Your receipt from Stripe");
    }

    [Theory]
    [InlineData("Weitergeleitet", "<p>---------- Forwarded message ---------<br>Von: <b>Stripe</b> &lt;receipts@stripe.com&gt;</p>")]
    [InlineData("Begin forwarded message:\n\n> From: Stripe <receipts@stripe.com>", null)]
    public async Task Original_sender_of_a_forwarded_mail_is_found(string text, string? html)
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(text: text, html: html));

        // Act
        await app.FetchOk();

        // Assert
        (await app.Inbox()).Items.Single().OriginalSender.ShouldBe("Stripe <receipts@stripe.com>");
    }

    [Fact]
    public async Task Each_attachment_is_a_receipt_logos_and_icons_are_not()
    {
        // Arrange
        using var app = await AppWithSender();
        var body = new BodyBuilder { HtmlBody = "<p>Rechnungen</p><img src=\"cid:logo\">" };
        body.Attachments.Add("rechnung-1.pdf", ReceiptTestData.Pdf("1"));
        body.Attachments.Add("quittung.png", Image(50_000));
        body.Attachments.Add("icon.png", Image(2_000));
        body.Attachments.Add("notiz.txt", "Hallo"u8.ToArray());
        body.LinkedResources.Add("logo.png", Image(40_000)).ContentId = "logo";
        var mail = Mail();
        mail.Body = body.ToMessageBody();
        app.Mailbox.Receive(mail);

        // Act
        await app.FetchOk();

        // Assert
        // One mail, one receipt with its files (story #012).
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        item.Files.Select(f => f.FileName).ShouldBe(["rechnung-1.pdf", "quittung.png"]);
    }

    [Fact]
    public async Task Mail_of_an_unknown_sender_is_rejected()
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(from: "spam@example.com", attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));

        // Act
        var result = await app.FetchOk();

        // Assert
        result.ShouldBe(new FetchResult(0, 1));
        (await app.Inbox()).Items.ShouldBeEmpty();
        app.Store.Uploads.ShouldBeEmpty();
        app.Mailbox.Folder(Rejected).Count.ShouldBe(1);
        var mail = await app.Db.ImportedMails.SingleAsync();
        (mail.Outcome, mail.Reason).ShouldBe((MailOutcome.Rejected, SenderCheck.NotAllowed));
    }

    [Theory]
    [InlineData("mx.hostpoint.ch; spf=pass smtp.mailfrom=example.ch; dkim=pass header.d=example.ch; dmarc=pass", true)]
    [InlineData("mx.hostpoint.ch; spf=softfail smtp.mailfrom=example.ch; dkim=pass header.d=example.ch", true)]
    [InlineData("mx.hostpoint.ch; spf=fail smtp.mailfrom=example.ch; dkim=none", false)]
    [InlineData("mx.hostpoint.ch; spf=pass smtp.mailfrom=example.ch; dkim=pass header.d=example.ch; dmarc=fail", false)]
    public async Task Forged_sender_is_rejected(string authenticationResults, bool accepted)
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(authenticationResults: authenticationResults, attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));

        // Act
        await app.FetchOk();

        // Assert
        (await app.Inbox()).Items.Count.ShouldBe(accepted ? 1 : 0);
        app.Mailbox.Folder(accepted ? Processed : Rejected).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Allowed_domain_lets_all_its_addresses_send()
    {
        // Arrange
        using var app = new TestApp();
        await app.AllowSender(" @Stripe.com ");
        app.Mailbox.Receive(Mail(from: "receipts@stripe.com", attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));
        app.Mailbox.Receive(Mail(from: "receipts@stripe.com.evil.ch", attachments: ("rechnung.pdf", ReceiptTestData.Pdf("2"))));

        // Act
        var result = await app.FetchOk();

        // Assert
        result.ShouldBe(new FetchResult(1, 1));
    }

    [Fact]
    public async Task Same_mail_and_same_file_come_in_only_once()
    {
        // Arrange
        using var app = await AppWithSender();
        var pdf = ReceiptTestData.Pdf("Swisscom");
        app.Mailbox.Receive(Mail(messageId: "a@example.ch", attachments: ("rechnung.pdf", pdf)));
        await app.FetchOk();

        // Act: the same mail again (for example moved back), and the same file in a new mail
        app.Mailbox.Receive(Mail(messageId: "a@example.ch", attachments: ("rechnung.pdf", pdf)));
        app.Mailbox.Receive(Mail(messageId: "b@example.ch", subject: "Nochmals", attachments: ("kopie.pdf", pdf)));
        var result = await app.FetchOk();

        // Assert
        result.ShouldBe(new FetchResult(0, 0));
        (await app.Inbox()).Items.Count.ShouldBe(1);
        app.Mailbox.Folder(Processed).Count.ShouldBe(3);
        (await app.Db.ImportedMails.SingleAsync(m => m.MessageId == "b@example.ch")).Outcome.ShouldBe(MailOutcome.Duplicate);
    }

    [Fact]
    public async Task Failed_move_only_moves_the_mail_next_time()
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));
        app.Mailbox.MoveError = new IOException("Verbindung unterbrochen");
        await app.FetchOk();
        app.Mailbox.Inbox.Count.ShouldBe(1);

        // Act
        await app.FetchOk();

        // Assert
        (await app.Inbox()).Items.Count.ShouldBe(1);
        app.Mailbox.Inbox.ShouldBeEmpty();
        app.Mailbox.Folder(Processed).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Unreachable_mailbox_shows_in_the_inbox_and_keeps_the_mail()
    {
        // Arrange
        using var app = await AppWithSender();
        app.Mailbox.Receive(Mail(attachments: ("rechnung.pdf", ReceiptTestData.Pdf())));
        app.Mailbox.OpenError = new MailKit.Security.AuthenticationException("Bad credentials");

        // Act
        var failed = await app.Fetch();
        var inboxAfterFailure = await app.Inbox();
        app.Mailbox.OpenError = null;
        await app.FetchOk();

        // Assert
        failed.Error.ShouldBe("Die Anmeldung an der Mailbox ist fehlgeschlagen. Bitte Benutzer und Passwort prüfen.");
        inboxAfterFailure.Mailbox.LastError.ShouldBe(failed.Error);
        var inbox = await app.Inbox();
        inbox.Mailbox.LastError.ShouldBeNull();
        inbox.Mailbox.LastFetch.ShouldBe(app.Clock.Now);
        inbox.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Without_configuration_nothing_is_fetched()
    {
        // Arrange
        using var app = new TestApp(configure: services => services.Configure<MailboxOptions>(options => options.Password = null));

        // Act
        var result = await app.Fetch();

        // Assert
        result.Error.ShouldBe(FetchMailHandler.NotConfigured);
        (await app.Inbox()).Mailbox.IsConfigured.ShouldBeFalse();
    }

    internal static async Task<TestApp> AppWithSender()
    {
        var app = new TestApp();
        await app.AllowSender(User);
        return app;
    }
}

internal static class InboxTestSteps
{
    public static async Task AllowSender(this TestApp app, string address)
    {
        var result = await app.Command<AddAllowedSender, Result<Guid>>(new AddAllowedSender(new SenderInput(address)));
        result.IsSuccess.ShouldBeTrue(result.Error);
    }

    public static async Task<InboxView> Inbox(this TestApp app) => (await app.Query<GetInbox, InboxView>(new GetInbox())).Value!;
}
