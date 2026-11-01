using System.IO.Compression;
using System.Text;
using Flowtr.Server.Features.Export.Queries;
using Flowtr.Server.Features.Extraction;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Receipts;
using Flowtr.Server.Features.Receipts.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Tests.Inbox;
using Microsoft.EntityFrameworkCore;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Receipts;

/// <summary>
/// Story #014: a table as text (CSV, TSV) is a receipt, for example the statement of App Store Connect.
/// </summary>
public class TableReceiptTests
{
    private const string Apple = """
        Country or Region	Units Sold	Earned	Currency	Exchange Rate	Proceeds
        Switzerland (CHF)	3	5.19	CHF	1.00	5.19
        Euro-Zone (EUR)	2	2.34	EUR	0.92	2.16
        Total			CHF		7.35
        """;

    [Theory]
    [InlineData("Datum;Betrag\n01.10.2026;12.50\n")]
    [InlineData("Datum,Betrag\r\n2026-10-01,12.50")]
    [InlineData(Apple)]
    [InlineData("\"Beschreibung; mit Semikolon\";Betrag\n\"Zeile\nmit Umbruch\";1")]
    public void Utf8_table_is_a_receipt(string table)
    {
        ReceiptFiles.Detect(Encoding.UTF8.GetBytes(table)).ShouldBe((ReceiptContentTypes.Table, ".csv"));
    }

    [Fact]
    public void Byte_order_marks_of_utf8_and_utf16_are_accepted()
    {
        // Arrange
        const string table = "Grösse;Betrag\nÄpfel;1.50";

        // Assert
        ReceiptFiles.Detect([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(table)])?.ContentType.ShouldBe(ReceiptContentTypes.Table);
        ReceiptFiles.Detect([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(table)])?.ContentType.ShouldBe(ReceiptContentTypes.Table);
        ReceiptFiles.Detect([.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes(table)])?.ContentType.ShouldBe(ReceiptContentTypes.Table);
    }

    [Theory]
    [InlineData("Hallo Welt")]
    [InlineData("Datum;Betrag")]
    [InlineData("<html><body>a;b\nc;d</body></html>")]
    [InlineData("a;b\0\nc;d")]
    [InlineData("a;b\u001B\nc;d")]
    public void Other_text_is_no_receipt(string text)
    {
        ReceiptFiles.Detect(Encoding.UTF8.GetBytes(text)).ShouldBeNull();
    }

    [Fact]
    public void Binary_file_and_invalid_utf8_are_no_receipt()
    {
        // Assert
        ReceiptFiles.Detect([0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x3B, 0x0A, 0x3B, 0x3B]).ShouldBeNull();
        ReceiptFiles.Detect(Encoding.Latin1.GetBytes("Grösse;Betrag\nÄpfel;1.50")).ShouldBeNull();
    }

    [Fact]
    public void Rows_take_quoted_cells_with_separators_and_line_breaks()
    {
        // Act
        var rows = TextTable.Rows("A;B\n\"x;y\";\"zwei\r\nZeilen\"\n\"mit \"\"Zitat\"\"\";3\n\n");

        // Assert
        rows.Select(row => string.Join("|", row)).ShouldBe(["A|B", "x;y|zwei\r\nZeilen", "mit \"Zitat\"|3"]);
    }

    [Fact]
    public async Task Upload_takes_a_table_and_the_ai_reads_it_as_text()
    {
        // Arrange
        using var app = new TestApp();
        app.Reader.Next = Result.Success(new ReceiptReading("2026-09-30", "Income", 7.35m, "CHF", "App Store September 2026", "Apple", null, null));

        // Act
        var upload = await app.Upload(Encoding.UTF8.GetBytes(Apple), "Apple_September_2026.csv");
        var proposal = await app.Query<AnalyzeUploads, BookingProposal>(new AnalyzeUploads([upload.Value!.UploadId]));

        // Assert
        upload.Value.ContentType.ShouldBe(ReceiptContentTypes.Table);
        proposal.Value!.AmountChf.ShouldBe(7.35m);
        app.Reader.LastContentType.ShouldBe(ReceiptContentTypes.Table);
        Encoding.UTF8.GetString(app.Reader.LastContent!).ShouldBe(Apple);
    }

    [Fact]
    public void Long_table_goes_to_the_ai_shortened()
    {
        // Arrange
        var table = "A;B\n" + string.Concat(Enumerable.Repeat("1234567890;1234567890\n", 20_000));

        // Act
        var text = AnalyzeUploadsHandler.TableForReader([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(table)]);

        // Assert
        text.Length.ShouldBe(AnalyzeUploadsHandler.MaxTableText);
        text.ShouldStartWith("A;B\n");
    }

    [Fact]
    public async Task Filed_table_stays_unchanged_and_comes_into_the_export()
    {
        // Arrange
        using var app = new TestApp();
        var table = Encoding.UTF8.GetBytes(Apple);
        var upload = await app.Upload(table, "Apple_September_2026.csv");

        // Act
        await app.Create(Income(description: "App Store September 2026") with { UploadIds = [upload.Value!.UploadId] });
        var file = (await app.Query<GetReceiptFile, FileContent>(new GetReceiptFile((await app.Db.ReceiptFiles.SingleAsync()).Id))).Value!;
        var export = await app.Query<ExportJournal, ExportFile>(new ExportJournal(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

        // Assert
        (file.FileName, file.ContentType).ShouldBe(("E-2026-001.csv", ReceiptContentTypes.Table));
        await using var content = export.Value!.Content;
        using var zip = new ZipArchive(content, ZipArchiveMode.Read);
        await using var entry = await zip.GetEntry("Belege/Einnahmen/E-2026-001_app-store-september-2026.csv")!.OpenAsync();
        using var copy = new MemoryStream();
        await entry.CopyToAsync(copy);
        copy.ToArray().ShouldBe(table);
    }

    [Fact]
    public async Task Csv_attachment_of_a_mail_is_a_file_of_the_receipt()
    {
        // Arrange
        using var app = await MailImportTests.AppWithSender();
        app.Mailbox.Receive(MailTestData.Mail(subject: "Apple Payments", attachments: ("payments.csv", Encoding.UTF8.GetBytes(Apple))));

        // Act
        await app.FetchOk();

        // Assert
        var file = (await app.Inbox()).Items.ShouldHaveSingleItem().Files.ShouldHaveSingleItem();
        (file.FileName, file.ContentType).ShouldBe(("payments.csv", ReceiptContentTypes.Table));
    }

    [Fact]
    public void View_shows_cells_as_text_only()
    {
        // Arrange
        var table = Encoding.UTF8.GetBytes("Name;Betrag\n<script>alert(1)</script>;1'234.50\n=HYPERLINK(\"https://evil.example\");x");

        // Act
        var page = TableView.Render(table, "<b>bericht</b>.csv", "?original=true");

        // Assert
        page.ShouldNotContain("<script");
        page.ShouldContain("<th>Name</th><th>Betrag</th>");
        page.ShouldContain("<td>&lt;script&gt;alert(1)&lt;/script&gt;</td><td class=\"num\">1&#39;234.50</td>");
        page.ShouldContain("<td>=HYPERLINK(");
        page.ShouldNotContain("<a href=\"https://evil.example");
        page.ShouldContain("<title>&lt;b&gt;bericht&lt;/b&gt;.csv</title>");
        page.ShouldContain("href=\"?original=true\"");
    }

    [Fact]
    public void View_policy_stops_scripts_and_external_content()
    {
        TableView.ContentSecurityPolicy.ShouldStartWith("sandbox ");
        TableView.ContentSecurityPolicy.ShouldNotContain("allow-scripts");
        TableView.ContentSecurityPolicy.ShouldNotContain("allow-same-origin");
        TableView.ContentSecurityPolicy.ShouldContain("default-src 'none'");
    }
}
