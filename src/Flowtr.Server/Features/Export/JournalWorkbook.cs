using ClosedXML.Excel;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Export;

/// <summary>
/// The journal as Excel (story #013): real numbers and dates, so that the fiduciary can calculate with them.
/// </summary>
public static class JournalWorkbook
{
    private const string AmountFormat = "#,##0.00";

    private static readonly string[] Columns =
    [
        "Datum", "Belegnummer", "Art", "Kategorie", "Beschreibung", "Gegenpartei", "Zahlungsart",
        "Einnahmen CHF", "Ausgaben CHF", "Originalbetrag", "Währung", "Notiz", "Dateien",
    ];

    public static byte[] Build(ExportReport report)
    {
        using var workbook = new XLWorkbook();
        Info(workbook.AddWorksheet("Info"), report);
        Journal(workbook.AddWorksheet("Journal"), report);
        Categories(workbook.AddWorksheet("Kategorien"), report);
        Files(workbook.AddWorksheet("Belege"), report);
        workbook.Worksheet("Journal").SetTabActive();
        workbook.Properties.Title = report.Title;
        workbook.Properties.Author = report.CreatedBy;
        workbook.Properties.Company = report.Business ?? "";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void Journal(IXLWorksheet sheet, ExportReport report)
    {
        for (var c = 0; c < Columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = Columns[c];
        }

        var r = 2;
        foreach (var row in report.Rows)
        {
            sheet.Cell(r, 1).Value = row.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(r, 2).Value = row.ReceiptNumber ?? "ohne Beleg";
            sheet.Cell(r, 3).Value = row.KindName;
            sheet.Cell(r, 4).Value = row.Category;
            sheet.Cell(r, 5).Value = row.Description;
            sheet.Cell(r, 6).Value = row.Counterparty;
            sheet.Cell(r, 7).Value = row.PaymentMethod;
            if (row.Kind == BookingKind.Income)
            {
                sheet.Cell(r, 8).Value = row.AmountChf;
            }
            else
            {
                sheet.Cell(r, 9).Value = row.AmountChf;
            }

            sheet.Cell(r, 10).Value = row.OriginalAmount;
            sheet.Cell(r, 11).Value = row.OriginalCurrency;
            sheet.Cell(r, 12).Value = row.Note;
            sheet.Cell(r, 13).Value = row.Files;
            r++;
        }

        // Totals as formulas: they stay right when the fiduciary changes a row.
        var last = r - 1;
        sheet.Cell(r, 7).Value = "Total";
        sheet.Cell(r, 8).FormulaA1 = report.Rows.Count == 0 ? "0" : $"SUM(H2:H{last})";
        sheet.Cell(r, 9).FormulaA1 = report.Rows.Count == 0 ? "0" : $"SUM(I2:I{last})";
        sheet.Cell(r + 1, 7).Value = "Gewinn";
        sheet.Cell(r + 1, 8).FormulaA1 = $"H{r}-I{r}";
        sheet.Range(r, 7, r + 1, 9).Style.Font.Bold = true;

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(1).Style.DateFormat.Format = "dd.MM.yyyy";
        sheet.Columns(8, 10).Style.NumberFormat.Format = AmountFormat;
        sheet.SheetView.FreezeRows(1);
        if (report.Rows.Count > 0)
        {
            sheet.Range(1, 1, last, Columns.Length).SetAutoFilter();
        }

        sheet.Columns().AdjustToContents(1, Math.Max(1, last), 8, 60);
    }

    /// <summary>
    /// What the export is: business, period, when and by whom, whether it is final.
    /// </summary>
    private static void Info(IXLWorksheet sheet, ExportReport report)
    {
        (string Label, XLCellValue Value)[] rows =
        [
            ("Geschäft", report.Business ?? ""),
            ("Zeitraum", $"{Shared.SwissFormat.Date(report.From)} – {Shared.SwissFormat.Date(report.To)}"),
            ("Status", report.Status),
            ("Erstellt am", report.CreatedAt),
            ("Erstellt von", report.CreatedBy),
            ("Buchungen", report.Rows.Count),
            ("Buchungen ohne Beleg", report.WithoutReceipt),
            ("Dateien der Belege", report.Files.Count),
            ("Einnahmen CHF", report.Income),
            ("Ausgaben CHF", report.Expense),
            ("Gewinn CHF", report.Profit),
        ];

        for (var r = 0; r < rows.Length; r++)
        {
            sheet.Cell(r + 1, 1).Value = rows[r].Label;
            sheet.Cell(r + 1, 2).Value = rows[r].Value;
        }

        sheet.Column(1).Style.Font.Bold = true;
        sheet.Cell(4, 2).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        sheet.Range(9, 2, 11, 2).Style.NumberFormat.Format = AmountFormat;
        sheet.Cell(rows.Length + 2, 2).Value = "Die Datei Belegverzeichnis.csv und das Blatt «Belege» nennen den SHA-256 jeder Datei: Er zeigt, dass eine Datei unverändert ist.";
        sheet.Columns().AdjustToContents(8, 80);
    }

    /// <summary>
    /// The index of the receipt files (Belegverzeichnis) with their SHA-256.
    /// </summary>
    private static void Files(IXLWorksheet sheet, ExportReport report)
    {
        string[] columns = ["Belegnummer", "Datei", "Datum", "Art", "Beschreibung", "Datei im ZIP", "Originalname", "Dateityp", "Grösse (Bytes)", "SHA-256"];
        for (var c = 0; c < columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c];
        }

        var r = 2;
        foreach (var file in report.Files)
        {
            sheet.Cell(r, 1).Value = file.Number;
            sheet.Cell(r, 2).Value = file.File;
            sheet.Cell(r, 3).Value = file.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(r, 4).Value = file.KindName;
            sheet.Cell(r, 5).Value = file.Description;
            sheet.Cell(r, 6).Value = file.Path;
            sheet.Cell(r, 7).Value = file.FileName;
            sheet.Cell(r, 8).Value = file.ContentType;
            sheet.Cell(r, 9).Value = file.Size;
            sheet.Cell(r, 10).Value = file.Sha256;
            r++;
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(3).Style.DateFormat.Format = "dd.MM.yyyy";
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(8, 70);
    }

    private static void Categories(IXLWorksheet sheet, ExportReport report)
    {
        string[] columns = ["Kategorie", "Art", "Anzahl", "Betrag CHF"];
        for (var c = 0; c < columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c];
        }

        var r = 2;
        foreach (var total in report.Categories)
        {
            sheet.Cell(r, 1).Value = total.Category;
            sheet.Cell(r, 2).Value = total.KindName;
            sheet.Cell(r, 3).Value = total.Bookings;
            sheet.Cell(r, 4).Value = total.AmountChf;
            r++;
        }

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Column(4).Style.NumberFormat.Format = AmountFormat;
        sheet.Columns().AdjustToContents(8, 60);
    }
}
