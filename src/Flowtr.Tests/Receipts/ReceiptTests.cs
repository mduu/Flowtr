using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Server.Features.Receipts;
using Flowtr.Server.Features.Receipts.Queries;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Receipts;

/// <summary>
/// Story #008: upload a receipt in the booking dialog; the save numbers and files it (ADR-001, ADR-007).
/// </summary>
public class ReceiptTests
{
    [Fact]
    public async Task Upload_checks_the_content_not_the_name()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var pdf = await app.Upload(ReceiptTestData.Pdf(), "scan.txt");
        var text = await app.Upload("Hallo"u8.ToArray(), "rechnung.pdf");
        var empty = await app.Upload([], "leer.pdf");

        // Assert
        pdf.Value!.ContentType.ShouldBe("application/pdf");
        text.Error.ShouldBe("Flowtr nimmt nur PDF und Bilder (JPEG, PNG, HEIC, WebP) als Beleg.");
        empty.Error.ShouldBe("Die Datei ist leer.");
        app.Store.Uploads.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Upload_rejects_files_over_20_mb()
    {
        // Arrange
        using var app = new TestApp();
        var big = new byte[ReceiptFiles.MaxSize + 1];
        ReceiptTestData.Pdf().CopyTo(big, 0);

        // Act
        var result = await app.Upload(big);

        // Assert
        result.Error.ShouldBe("Die Datei ist zu gross (höchstens 20 MB).");
    }

    [Fact]
    public async Task Save_numbers_and_files_the_receipt()
    {
        // Arrange
        using var app = new TestApp();
        var uploadId = await app.UploadPdf();

        // Act
        await app.Create(Expense(date: "2026-10-01", description: "Azure Abo") with { UploadId = uploadId });

        // Assert
        var booking = (await Journal(app)).Bookings.ShouldHaveSingleItem();
        booking.ReceiptNumber.ShouldBe("A-2026-001");
        booking.ReceiptContentType.ShouldBe("application/pdf");
        app.Store.Receipts.Keys.ShouldBe(["2026/ausgaben/A-2026-001_azure-abo.pdf"]);
        app.Store.Uploads.ShouldBeEmpty();
        var receipt = await app.Db.Receipts.SingleAsync();
        (receipt.FileName, receipt.Size, receipt.Sha256.Length).ShouldBe(("rechnung.pdf", ReceiptTestData.Pdf().Length, 64));
    }

    [Fact]
    public async Task Numbers_count_per_year_and_kind()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        await app.Create(Expense(date: "2026-01-05") with { UploadId = await app.UploadPdf() });
        await app.Create(Expense(date: "2026-02-05") with { UploadId = await app.UploadPdf() });
        await app.Create(Income(date: "2026-02-06") with { UploadId = await app.UploadPdf() });
        await app.Create(Expense(date: "2025-12-30") with { UploadId = await app.UploadPdf() });
        await app.Create(Expense(date: "2026-03-01"));

        // Assert
        (await app.Db.Receipts.Select(r => r.Number).OrderBy(n => n).ToListAsync())
            .ShouldBe(["A-2025-001", "A-2026-001", "A-2026-002", "E-2026-001"]);
    }

    [Fact]
    public async Task Parallel_save_gets_the_next_number()
    {
        // Arrange: the second save starts with an old sequence, as if it ran at the same time.
        using var app = new TestApp(transactional: true);
        await app.Create(Expense() with { UploadId = await app.UploadPdf() });
        using var second = app.NewScope();
        var secondDb = second.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stale = await secondDb.ReceiptSequences.SingleAsync();
        await app.Create(Expense() with { UploadId = await app.UploadPdf() });
        var uploadId = await app.UploadPdf();

        // Act
        var result = await second.ServiceProvider.GetRequiredService<ICommandHandler<CreateBooking, Result<Guid>>>()
            .Handle(new CreateBooking(Expense() with { UploadId = uploadId }), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
        stale.LastNumber.ShouldBe(3);
        (await app.Db.Receipts.Select(r => r.Number).OrderBy(n => n).ToListAsync()).ShouldBe(["A-2026-001", "A-2026-002", "A-2026-003"]);
        app.Store.Receipts.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Replace_keeps_the_number_and_remove_deletes_the_file()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(description: "Laptop") with { UploadId = await app.UploadPdf("alt") });
        var newUpload = await app.Upload(ReceiptTestData.Png, "foto.png");

        // Act
        var replaced = await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(description: "Laptop") with { UploadId = newUpload.Value!.UploadId }));

        // Assert
        replaced.IsSuccess.ShouldBeTrue(replaced.Error);
        app.Store.Receipts.Keys.ShouldBe(["2026/ausgaben/A-2026-001_laptop.png"]);

        // Act
        var removed = await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(description: "Laptop") with { RemoveReceipt = true }));

        // Assert
        removed.IsSuccess.ShouldBeTrue(removed.Error);
        app.Store.Receipts.ShouldBeEmpty();
        (await app.Db.Receipts.CountAsync()).ShouldBe(0);
        (await Journal(app)).Bookings.Single().ReceiptNumber.ShouldBeNull();
    }

    [Fact]
    public async Task Year_and_kind_stay_while_a_receipt_is_attached()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense(date: "2026-10-01") with { UploadId = await app.UploadPdf() });

        // Act
        var otherYear = await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(date: "2025-10-01")));
        var sameYear = await app.Command<UpdateBooking, Result>(new UpdateBooking(id, Expense(date: "2026-11-01", amount: 30m)));

        // Assert
        otherYear.Error!.ShouldStartWith("Jahr und Art einer Buchung mit Beleg lassen sich nicht ändern");
        sameYear.IsSuccess.ShouldBeTrue(sameYear.Error);
    }

    [Fact]
    public async Task Delete_removes_booking_and_receipt()
    {
        // Arrange
        using var app = new TestApp();
        var id = await app.Create(Expense() with { UploadId = await app.UploadPdf() });

        // Act
        var result = await app.Command<DeleteBooking, Result>(new DeleteBooking(id));

        // Assert
        result.IsSuccess.ShouldBeTrue(result.Error);
        app.Store.Receipts.ShouldBeEmpty();
        (await app.Db.Receipts.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Missing_upload_saves_nothing()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var result = await app.Command<CreateBooking, Result<Guid>>(new CreateBooking(Expense() with { UploadId = Guid.NewGuid() }));

        // Assert
        result.Error.ShouldBe(ReceiptFiling.UploadGone);
        (await app.Db.Bookings.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Receipt_file_is_named_by_its_number()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense() with { UploadId = await app.UploadPdf("Inhalt") });
        var receiptId = (await app.Db.Receipts.SingleAsync()).Id;

        // Act
        var file = (await app.Query<GetReceiptFile, ReceiptFile>(new GetReceiptFile(receiptId))).Value!;

        // Assert
        file.FileName.ShouldBe("A-2026-001.pdf");
        file.ContentType.ShouldBe("application/pdf");
        new StreamReader(file.Content).ReadToEnd().ShouldContain("Inhalt");
    }

    [Fact]
    public async Task Housekeeping_deletes_uploads_after_30_days()
    {
        // Arrange
        using var app = new TestApp();
        var old = await app.UploadPdf();
        app.Clock.Now = app.Clock.Now.AddDays(30);
        var fresh = await app.UploadPdf();
        var housekeeping = new ReceiptHousekeeping(app.Get<IServiceScopeFactory>(), app.Clock, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReceiptHousekeeping>.Instance);

        // Act
        await housekeeping.Run(app.Get<IServiceProvider>(), CancellationToken.None);

        // Assert
        app.Store.Uploads.Keys.ShouldBe([fresh]);
    }

    [Fact]
    public async Task Receipt_is_in_the_audit_log()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        await app.Create(Expense() with { UploadId = await app.UploadPdf() });

        // Assert
        (await app.Db.AuditEntries.Select(e => e.EntityType).ToListAsync()).ShouldContain("Receipt");
    }

    private static async Task<JournalView> Journal(TestApp app) =>
        (await app.Query<GetJournal, JournalView>(new GetJournal(new JournalFilter(2026)))).Value!;
}
