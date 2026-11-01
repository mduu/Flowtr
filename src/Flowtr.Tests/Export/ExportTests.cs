using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Flowtr.Server.Features.Business.Commands;
using Flowtr.Server.Features.Export.Queries;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Shared.Business;
using Flowtr.Shared;
using Flowtr.Tests.Inbox;
using Flowtr.Tests.Receipts;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Export;

/// <summary>
/// Story #013: journal and receipts of a period as ZIP, for the fiduciary or the tax return.
/// </summary>
public class ExportTests
{
    [Fact]
    public async Task Zip_has_journal_and_receipts_in_folders()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(description: "Claude Pro") with { UploadIds = [await app.UploadPdf("Invoice"), await app.UploadPdf("Receipt")] });
        await app.Create(Income() with { UploadIds = [await app.UploadPdf("Auszahlung")] });
        await app.Create(Expense(description: "Ohne Beleg", amount: 5m));

        // Act
        var zip = await Export(app);

        // Assert
        zip.Entries.Select(e => e.FullName).ShouldBe(
        [
            "Journal.xlsx",
            "Journal.pdf",
            "Belegverzeichnis.csv",
            "Belege/Ausgaben/A-2026-001_claude-pro.pdf",
            "Belege/Ausgaben/A-2026-001_claude-pro_2.pdf",
            "Belege/Einnahmen/E-2026-001_app-verkaeufe.pdf",
        ], ignoreOrder: true);
        Text(zip, "Belege/Ausgaben/A-2026-001_claude-pro_2.pdf").ShouldContain("Receipt");
        Text(zip, ExportJournalHandler.Index).ShouldContain("A-2026-001;2 von 2;");
        Text(zip, "Journal.pdf").ShouldStartWith("%PDF-");
    }

    [Fact]
    public async Task Excel_has_numbers_totals_and_categories()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(date: "2026-03-01", amount: 20.50m) with { UploadIds = [await app.UploadPdf()] });
        await app.Create(Expense(date: "2026-02-01", amount: 9.50m, description: "Domain"));
        await app.Create(Income(date: "2026-04-01", amount: 100m));

        // Act
        var zip = await Export(app);

        // Assert
        using var workbook = new XLWorkbook(Entry(zip, "Journal.xlsx"));
        var journal = workbook.Worksheet("Journal");
        journal.Cell(2, 1).GetDateTime().ShouldBe(new DateTime(2026, 2, 1));
        journal.Cell(2, 5).GetString().ShouldBe("Domain");
        journal.Cell(3, 2).GetString().ShouldBe("A-2026-001");
        journal.Cell(3, 9).GetValue<decimal>().ShouldBe(20.50m);
        journal.Cell(4, 8).GetValue<decimal>().ShouldBe(100m);
        journal.Cell(5, 9).FormulaA1.ShouldBe("SUM(I2:I4)");
        journal.Cell(6, 8).Value.GetNumber().ShouldBe(70.0);
        var categories = workbook.Worksheet("Kategorien");
        (categories.Cell(2, 1).GetString(), categories.Cell(2, 4).GetValue<decimal>()).ShouldBe(("Umsatz und Honorare", 100m));
        (categories.Cell(3, 1).GetString(), categories.Cell(3, 3).GetValue<int>(), categories.Cell(3, 4).GetValue<decimal>())
            .ShouldBe(("Software und Abos", 2, 30.00m));
    }

    [Fact]
    public async Task Only_the_period_and_optional_receipts()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(date: "2025-12-31", description: "Alt") with { UploadIds = [await app.UploadPdf()] });
        await app.Create(Expense(date: "2026-01-01", description: "Neu") with { UploadIds = [await app.UploadPdf()] });

        // Act
        var year = await Export(app);
        var journalOnly = await Export(app, withReceipts: false);

        // Assert
        year.Entries.Select(e => e.FullName).ShouldBe(["Journal.xlsx", "Journal.pdf", "Belegverzeichnis.csv", "Belege/Ausgaben/A-2026-001_neu.pdf"], ignoreOrder: true);
        using var workbook = new XLWorkbook(Entry(year, "Journal.xlsx"));
        workbook.Worksheet("Journal").Cell(2, 5).GetString().ShouldBe("Neu");
        journalOnly.Entries.Select(e => e.FullName).ShouldBe(["Journal.xlsx", "Journal.pdf", "Belegverzeichnis.csv"], ignoreOrder: true);
    }

    [Fact]
    public async Task Mail_as_receipt_comes_as_original_and_as_pdf()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(MailTestData.Mail(subject: "Your receipt from Stripe", html: MailTestData.StripeHtml, text: null));
        await app.FetchOk();
        var item = (await app.Inbox()).Items.Single();
        await app.Create(Expense(description: "Stripe") with { UploadIds = [item.Files.Single().UploadId] });

        // Act
        var zip = await Export(app);

        // Assert
        Text(zip, "Belege/Ausgaben/A-2026-001_stripe.eml").ShouldContain("Subject: Your receipt from Stripe");
        Text(zip, "Belege/Ausgaben/A-2026-001_stripe.pdf").ShouldStartWith("%PDF-");
    }

    [Fact]
    public void Mail_prints_header_and_text_without_markup()
    {
        // Arrange
        var mail = MailTestData.Mail(subject: "Rechnung", html: MailTestData.StripeHtml, text: null);
        using var stream = new MemoryStream();
        mail.WriteTo(stream);

        // Act
        var print = ExportJournalHandler.Print(stream.ToArray());

        // Assert
        (print.Subject, print.From).ShouldBe(("Rechnung", MailTestData.User));
        print.Body.ShouldContain("CHF 33.00");
        print.Body.ShouldNotContain("<");
        print.Body.ShouldNotContain("alert");
    }

    [Fact]
    public async Task Missing_file_is_listed_and_the_export_works()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(description: "Laptop") with { UploadIds = [await app.UploadPdf()] });
        app.Store.Receipts.Clear();

        // Act
        var zip = await Export(app);

        // Assert
        Text(zip, ExportJournalHandler.MissingFiles).ShouldContain("A-2026-001_laptop.pdf");
    }

    [Fact]
    public async Task Empty_period_gives_an_empty_journal()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var zip = await Export(app);

        // Assert
        zip.Entries.Select(e => e.FullName).ShouldBe(["Journal.xlsx", "Journal.pdf", "Belegverzeichnis.csv"], ignoreOrder: true);
    }

    [Fact]
    public async Task End_before_start_is_an_error()
    {
        using var app = new TestApp();

        var result = await app.Query<ExportJournal, ExportFile>(new ExportJournal(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1)));

        result.Error.ShouldBe("Das Ende des Zeitraums liegt vor dem Anfang.");
    }

    [Fact]
    public async Task Documents_tell_business_creation_and_status()
    {
        // Arrange
        using var app = new TestApp();
        await app.Command<SetBusiness, Result>(new SetBusiness(new BusinessInput("Marc Dürst, Softwareentwicklung")));
        await app.Create(Expense(description: "Ohne Beleg"));

        // Act
        var zip = await Export(app);

        // Assert
        using var workbook = new XLWorkbook(Entry(zip, "Journal.xlsx"));
        var info = workbook.Worksheet("Info");
        info.Cell(1, 2).GetString().ShouldBe("Marc Dürst, Softwareentwicklung");
        info.Cell(3, 2).GetString().ShouldStartWith("Provisorisch: 2026 ist nicht abgeschlossen.");
        info.Cell(4, 2).GetDateTime().ShouldBe(new DateTime(2026, 10, 12, 10, 0, 0));
        info.Cell(5, 2).GetString().ShouldBe("marc@example.ch");
        info.Cell(7, 2).GetValue<int>().ShouldBe(1);
        workbook.Worksheet("Journal").Cell(2, 2).GetString().ShouldBe("ohne Beleg");
    }

    [Fact]
    public async Task Closed_year_is_final()
    {
        // Arrange
        using var app = new TestApp();
        app.Db.ClosedYears.Add(new ClosedYear { Year = 2026, ClosedAt = new DateTimeOffset(2027, 3, 1, 9, 0, 0, TimeSpan.Zero) });
        await app.Db.SaveChangesAsync();

        // Act
        var zip = await Export(app);

        // Assert
        using var workbook = new XLWorkbook(Entry(zip, "Journal.xlsx"));
        workbook.Worksheet("Info").Cell(3, 2).GetString().ShouldBe("Abgeschlossen am 01.03.2027.");
    }

    [Fact]
    public async Task Index_lists_each_file_with_its_sha256()
    {
        // Arrange
        using var app = new TestApp();
        var pdf = ReceiptTestData.Pdf("Rechnung; mit Semikolon");
        var upload = await app.Upload(pdf, "rechnung.pdf");
        await app.Create(Expense(description: "Laptop; neu") with { UploadIds = [upload.Value!.UploadId] });

        // Act
        var zip = await Export(app);

        // Assert
        var lines = Text(zip, ExportJournalHandler.Index).TrimStart('\uFEFF').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldStartWith("Belegnummer;Datei;Datum;Art;Beschreibung;Datei im ZIP");
        var sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(pdf));
        lines[1].TrimEnd('\r').ShouldBe($"A-2026-001;1 von 1;01.10.2026;Ausgabe;\"Laptop; neu\";Belege/Ausgaben/A-2026-001_laptop-neu.pdf;rechnung.pdf;application/pdf;{pdf.Length};{sha256}");
        using var workbook = new XLWorkbook(Entry(zip, "Journal.xlsx"));
        workbook.Worksheet("Belege").Cell(2, 10).GetString().ShouldBe(sha256);
    }

    private static async Task<ZipArchive> Export(TestApp app, bool withReceipts = true)
    {
        var result = await app.Query<ExportJournal, ExportFile>(new ExportJournal(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), withReceipts, "marc@example.ch"));
        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Value!.FileName.ShouldBe("Flowtr-Export_2026-01-01_2026-12-31_erstellt-2026-10-12-1000.zip");

        var copy = new MemoryStream();
        await using (result.Value.Content)
        {
            await result.Value.Content.CopyToAsync(copy);
        }

        copy.Position = 0;
        return new ZipArchive(copy, ZipArchiveMode.Read);
    }

    private static MemoryStream Entry(ZipArchive zip, string name)
    {
        var copy = new MemoryStream();
        using (var entry = zip.GetEntry(name)!.Open())
        {
            entry.CopyTo(copy);
        }

        copy.Position = 0;
        return copy;
    }

    private static string Text(ZipArchive zip, string name) => Encoding.UTF8.GetString(Entry(zip, name).ToArray());
}
