using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Flowtr.Tests.Inbox;
using Flowtr.Tests.Receipts;
using Microsoft.EntityFrameworkCore;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Extraction;

/// <summary>
/// Story #017: one receipt gives more bookings, for example income and fees of a statement of Stripe.
/// </summary>
public class MoreBookingsTests
{
    private static readonly Guid BankFees = Guid.Parse("0199d6a0-0002-7000-8000-00000000000b");

    private static ReceiptReading Fees(decimal total = 4.48m, string? categoryId = null) =>
        new("2026-10-31", "Expense", total, "CHF", "Stripe Gebühren Oktober 2026", "Stripe", categoryId ?? BankFees.ToString(), null);

    private static ReceiptReading Statement(params ReceiptReading[] more) =>
        new("2026-10-31", "Income", 9.00m, "CHF", "Stripe Einnahmen Oktober 2026", "Stripe", Revenue.ToString(), null,
            "2 Zahlungen 108.00, 1 Rückerstattung −99.00", more);

    [Fact]
    public async Task Proposal_has_the_more_bookings_checked_like_the_first()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Statement(Fees(), Fees(total: -1m, categoryId: Revenue.ToString())));

        // Act
        var proposal = (await app.Query<AnalyzeUploads, BookingProposal>(new AnalyzeUploads([await app.UploadPdf()]))).Value!;

        // Assert
        (proposal.Kind, proposal.AmountChf, proposal.CategoryId, proposal.Note)
            .ShouldBe((BookingKind.Income, 9.00m, Revenue, "2 Zahlungen 108.00, 1 Rückerstattung −99.00"));
        proposal.MoreBookings!.Count.ShouldBe(2);
        proposal.MoreBookings[0].ShouldBe(new BookingProposal(
            new DateOnly(2026, 10, 31), BookingKind.Expense, 4.48m, null, null, "Stripe Gebühren Oktober 2026", BankFees, "Stripe", null, null));

        // An invalid amount and a category of the wrong kind stay empty.
        (proposal.MoreBookings[1].AmountChf, proposal.MoreBookings[1].CategoryId).ShouldBe((null, null));
    }

    [Fact]
    public async Task Proposal_has_at_most_five_bookings()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Statement([.. Enumerable.Repeat(Fees(), 9)]));

        // Act
        var proposal = (await app.Query<AnalyzeUploads, BookingProposal>(new AnalyzeUploads([await app.UploadPdf()]))).Value!;

        // Assert
        proposal.MoreBookings!.Count.ShouldBe(AnalyzeUploadsHandler.MaxBookings - 1);
    }

    [Fact]
    public async Task One_booking_has_no_more_bookings()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(Fees());

        // Act
        var proposal = (await app.Query<AnalyzeUploads, BookingProposal>(new AnalyzeUploads([await app.UploadPdf()]))).Value!;

        // Assert
        proposal.MoreBookings.ShouldBeNull();
    }

    [Fact]
    public async Task Each_booking_files_the_same_upload_as_its_own_receipt()
    {
        // Arrange
        using var app = new TestApp();
        var uploadId = await app.UploadPdf("Saldenübersicht");
        var content = app.Store.Uploads[uploadId].Content;

        // Act
        await app.Create(Income(date: "2026-10-31", amount: 9.00m) with { UploadIds = [uploadId], KeepUploads = true });
        var kept = app.Store.Uploads.ContainsKey(uploadId);
        await app.Create(Expense(date: "2026-10-31", amount: 4.48m, category: BankFees, counterparty: "Stripe") with { UploadIds = [uploadId] });

        // Assert
        kept.ShouldBeTrue();
        app.Store.Uploads.ShouldBeEmpty();
        var receipts = await app.Db.Receipts.Include(r => r.Files).OrderBy(r => r.Number).ToListAsync();
        receipts.Select(r => r.Number).ShouldBe(["A-2026-001", "E-2026-001"]);
        receipts.ShouldAllBe(r => r.Files.Count == 1);
        receipts.Select(r => app.Store.Receipts[r.Files[0].BlobName]).ShouldAllBe(file => file.SequenceEqual(content));
    }

    [Fact]
    public async Task Inbox_keeps_the_more_bookings_and_the_first_booking_takes_the_item()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(MailTestData.Mail(subject: "Stripe Oktober", attachments: ("balance_summary.pdf", ReceiptTestData.Pdf())));
        await app.FetchOk();
        app.Reader.Next = Result.Success(Statement(Fees()));
        await app.Command<AnalyzeInbox, Result<int>>(new AnalyzeInbox());
        var item = (await app.Inbox()).Items.ShouldHaveSingleItem();
        var uploadId = item.Files.ShouldHaveSingleItem().UploadId;

        // Act
        await app.Create(Income(date: "2026-10-31", amount: 9.00m) with { UploadIds = [uploadId], KeepUploads = true });

        // Assert
        item.Proposal!.MoreBookings.ShouldHaveSingleItem().AmountChf.ShouldBe(4.48m);
        (await app.Inbox()).Items.ShouldBeEmpty();
        app.Store.Uploads.ShouldContainKey(uploadId);
    }
}
