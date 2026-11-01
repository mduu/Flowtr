using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Server.Features.Receipts;
using Flowtr.Server.Features.Receipts.Model;
using Flowtr.Server.Features.Receipts.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Tests.Inbox;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using static Flowtr.Tests.Inbox.MailTestData;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Receipts;

/// <summary>
/// Story #012: a receipt has one number and one or more files, for example invoice and payment receipt.
/// </summary>
public class MultipleFilesTests
{
    [Fact]
    public async Task Two_files_get_one_number()
    {
        // Arrange
        using var app = new TestApp();
        var invoice = await app.UploadPdf("Invoice");
        var receipt = await app.UploadPdf("Receipt");

        // Act
        await app.Create(Expense(description: "Claude Abo") with { UploadIds = [invoice, receipt] });

        // Assert
        var booking = (await Journal(app)).Bookings.Single();
        booking.ReceiptNumber.ShouldBe("A-2026-001");
        booking.ReceiptFiles.Count.ShouldBe(2);
        app.Store.Receipts.Keys.ShouldBe(["2026/ausgaben/A-2026-001_claude-abo.pdf", "2026/ausgaben/A-2026-001_claude-abo_2.pdf"], ignoreOrder: true);
        app.Store.Uploads.ShouldBeEmpty();
        (await app.Db.Receipts.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Adding_and_removing_files_keeps_the_number()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(description: "Laptop") with { UploadIds = [await app.UploadPdf("Rechnung")] });
        var first = (await Journal(app)).Bookings.Single().ReceiptFiles.Single().Id;

        // Act
        var added = await Update(app, id, Expense(description: "Laptop") with { UploadIds = [await app.UploadPdf("Quittung")] });
        var removed = await Update(app, id, Expense(description: "Laptop") with { RemovedFileIds = [first] });
        var again = await Update(app, id, Expense(description: "Laptop") with { UploadIds = [await app.UploadPdf("Garantie")] });

        // Assert: the position of a removed file is not used again.
        (added.IsSuccess, removed.IsSuccess, again.IsSuccess).ShouldBe((true, true, true));
        (await Journal(app)).Bookings.Single().ReceiptNumber.ShouldBe("A-2026-001");
        app.Store.Receipts.Keys.ShouldBe(["2026/ausgaben/A-2026-001_laptop_2.pdf", "2026/ausgaben/A-2026-001_laptop_3.pdf"], ignoreOrder: true);
        (await app.Db.ReceiptFiles.Select(f => f.Position).OrderBy(p => p).ToListAsync()).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task A_receipt_has_at_most_ten_files()
    {
        // Arrange
        using var app = new TestApp();
        var uploads = new List<Guid>();
        for (var i = 0; i <= Receipt.MaxFiles; i++)
        {
            uploads.Add(await app.UploadPdf($"Seite {i}"));
        }

        // Act
        var result = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense() with { UploadIds = uploads }));

        // Assert
        result.Error.ShouldBe(ReceiptFiling.TooManyFiles);
        (await app.Db.Bookings.CountAsync()).ShouldBe(0);
        app.Store.Receipts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Year_can_change_when_all_files_go()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(date: "2026-10-01") with { UploadIds = [await app.UploadPdf()] });
        var file = (await Journal(app)).Bookings.Single().ReceiptFiles.Single().Id;

        // Act
        var kept = await Update(app, id, Expense(date: "2025-10-01") with { UploadIds = [await app.UploadPdf("neu")] });
        var replaced = await Update(app, id, Expense(date: "2025-10-01") with { UploadIds = [await app.UploadPdf("neu 2")], RemovedFileIds = [file] });

        // Assert
        kept.Error!.ShouldStartWith("Jahr und Art einer Buchung mit Beleg lassen sich nicht ändern");
        replaced.IsSuccess.ShouldBeTrue(replaced.Error);
        (await app.Db.Receipts.Select(r => r.Number).ToListAsync()).ShouldBe(["A-2025-001"]);
    }

    [Fact]
    public async Task Second_file_is_named_with_its_position()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense() with { UploadIds = [await app.UploadPdf("Invoice"), await app.UploadPdf("Receipt")] });
        var second = await app.Db.ReceiptFiles.SingleAsync(f => f.Position == 2);

        // Act
        var file = (await app.Query<GetReceiptFile, FileContent>(new GetReceiptFile(second.Id))).Value!;

        // Assert
        file.FileName.ShouldBe("A-2026-001_2.pdf");
        new StreamReader(file.Content).ReadToEnd().ShouldContain("Receipt");
    }

    [Fact]
    public async Task Mail_with_invoice_and_receipt_is_one_receipt_in_the_inbox()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Reader.Next = Result.Success(new ReceiptReading("2026-10-31", "Expense", 20m, "CHF", "Claude Pro", "Anthropic", Software.ToString(), null));
        app.Mailbox.Receive(Mail(subject: "Your receipt from Anthropic",
            attachments: [("Invoice-2412.pdf", ReceiptTestData.Pdf("Invoice")), ("Receipt-2412.pdf", ReceiptTestData.Pdf("Receipt"))]));

        // Act
        var fetched = await app.FetchOk();
        await app.Command<Server.Features.Inbox.Commands.AnalyzeInbox, Result<int>>(new Server.Features.Inbox.Commands.AnalyzeInbox());
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        await app.Create(Expense(description: "Claude Pro") with { UploadIds = item.Files.Select(f => f.UploadId).ToList() });

        // Assert
        fetched.Receipts.ShouldBe(1);
        item.Files.Select(f => f.FileName).ShouldBe(["Invoice-2412.pdf", "Receipt-2412.pdf"]);
        app.Reader.LastFiles.Count.ShouldBe(2);
        app.Reader.LastContext!.Origin!.ShouldContain("files \"Invoice-2412.pdf\", \"Receipt-2412.pdf\"");
        item.Proposal!.AmountChf.ShouldBe(20m);
        (await app.Inbox()).Items.ShouldBeEmpty();
        (await Journal(app)).Bookings.Single().ReceiptFiles.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Ai_reads_at_most_five_files()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(new ReceiptReading("2026-10-01", "Expense", 10m, "CHF", "Fotos", null, Software.ToString(), null));
        var uploads = new List<Guid>();
        for (var i = 0; i < 7; i++)
        {
            uploads.Add(await app.UploadPdf($"Seite {i}"));
        }

        // Act
        var result = await app.Query<AnalyzeUploads, BookingProposal>(new AnalyzeUploads(uploads));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
        app.Reader.LastFiles.Count.ShouldBe(AnalyzeUploadsHandler.MaxFiles);
    }

    private static Task<Result> Update(TestApp app, Guid id, BookingInput input) =>
        app.Command<UpdateBooking, Result>(new UpdateBooking(id, input));

    private static async Task<JournalView> Journal(TestApp app) =>
        (await app.Query<GetJournal, JournalView>(new GetJournal(new JournalFilter(2026)))).Value!;
}
