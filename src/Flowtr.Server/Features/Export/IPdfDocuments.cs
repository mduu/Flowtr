using Flowtr.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Flowtr.Server.Features.Export;

/// <summary>
/// PDF documents of the export. An adapter: another library can replace QuestPDF (ADR-009).
/// </summary>
public interface IPdfDocuments
{
    byte[] Journal(ExportReport report);

    byte[] Mail(MailPrint mail);
}

/// <summary>
/// <see cref="IPdfDocuments"/> with QuestPDF. Community License: individuals and companies under USD 1 million revenue.
/// QuestPDF has its own font (Lato): it needs no fonts in the container.
/// </summary>
public sealed class QuestPdfDocuments : IPdfDocuments
{
    private const float FontSize = 8.5f;

    static QuestPdfDocuments()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Journal(ExportReport report) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(text => text.FontSize(FontSize));

            page.Header().PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    if (report.Business is { } business)
                    {
                        column.Item().Text(business).FontSize(11).SemiBold();
                    }

                    column.Item().Text(report.Title).FontSize(14).SemiBold();
                    column.Item().Text(report.Status).Italic().FontColor(report.Status.StartsWith("Provisorisch") ? Colors.Orange.Darken3 : Colors.Grey.Darken2);
                });
                row.AutoItem().AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text($"Einnahmen {SwissFormat.Amount(report.Income)} · Ausgaben {SwissFormat.Amount(report.Expense)} · Gewinn {SwissFormat.Amount(report.Profit)} CHF");
                    column.Item().AlignRight().Text($"{report.Rows.Count} Buchungen, {report.WithoutReceipt} ohne Beleg").FontColor(Colors.Grey.Darken2);
                });
            });

            page.Content().Column(column =>
            {
                column.Item().Table(table => Bookings(table, report));
                column.Item().PaddingTop(16).Text("Summen pro Kategorie").FontSize(11).SemiBold();
                column.Item().PaddingTop(4).Width(12, Unit.Centimetre).Table(table => Categories(table, report));
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text(report.Created).FontColor(Colors.Grey.Medium);
                row.AutoItem().Text(text =>
                {
                    text.Span("Seite ").FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontColor(Colors.Grey.Medium);
                    text.Span(" von ").FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontColor(Colors.Grey.Medium);
                });
            });
        });
    }).GeneratePdf();

    public byte[] Mail(MailPrint mail) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().PaddingBottom(12).Column(column =>
            {
                column.Item().Text(mail.Subject).FontSize(13).SemiBold();
                Header(column, "Von", mail.From);
                Header(column, "An", mail.To);
                Header(column, "Datum", mail.Date is { } date ? date.ToString("dd.MM.yyyy HH:mm") : null);
                column.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            });

            page.Content().Text(mail.Body);

            page.Footer().AlignRight().Text(text =>
            {
                text.Span("E-Mail als Beleg, Original als .eml · Seite ").FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber().FontColor(Colors.Grey.Medium);
            });
        });
    }).GeneratePdf();

    private static void Bookings(TableDescriptor table, ExportReport report)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(52);
            columns.ConstantColumn(58);
            columns.RelativeColumn(3);
            columns.RelativeColumn(2);
            columns.RelativeColumn(2);
            columns.ConstantColumn(62);
            columns.ConstantColumn(62);
            columns.ConstantColumn(62);
        });

        table.Header(header =>
        {
            foreach (var title in new[] { "Datum", "Beleg", "Beschreibung", "Kategorie", "Gegenpartei" })
            {
                header.Cell().Element(HeaderCell).Text(title).SemiBold();
            }

            foreach (var title in new[] { "Original", "Einnahmen", "Ausgaben" })
            {
                header.Cell().Element(HeaderCell).AlignRight().Text(title).SemiBold();
            }
        });

        foreach (var row in report.Rows)
        {
            table.Cell().Element(Cell).Text(SwissFormat.Date(row.Date));
            if (row.ReceiptNumber is { } number)
            {
                table.Cell().Element(Cell).Text(number);
            }
            else
            {
                table.Cell().Element(Cell).Text("ohne Beleg").Italic().FontColor(Colors.Orange.Darken3);
            }

            table.Cell().Element(Cell).Text(row.Note is null ? row.Description : $"{row.Description} ({row.Note})");
            table.Cell().Element(Cell).Text(row.Category);
            table.Cell().Element(Cell).Text(row.Counterparty ?? "");
            table.Cell().Element(Cell).AlignRight().Text(row.OriginalAmount is { } original ? $"{row.OriginalCurrency} {SwissFormat.Amount(original)}" : "");
            table.Cell().Element(Cell).AlignRight().Text(row.Kind == Shared.Journal.BookingKind.Income ? SwissFormat.Amount(row.AmountChf) : "");
            table.Cell().Element(Cell).AlignRight().Text(row.Kind == Shared.Journal.BookingKind.Expense ? SwissFormat.Amount(row.AmountChf) : "");
        }

        table.Cell().ColumnSpan(6).Element(TotalCell).Text($"Total ({report.Rows.Count} Buchungen)").SemiBold();
        table.Cell().Element(TotalCell).AlignRight().Text(SwissFormat.Amount(report.Income)).SemiBold();
        table.Cell().Element(TotalCell).AlignRight().Text(SwissFormat.Amount(report.Expense)).SemiBold();
    }

    private static void Categories(TableDescriptor table, ExportReport report)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);
            columns.RelativeColumn(1.5f);
            columns.ConstantColumn(50);
            columns.ConstantColumn(70);
        });

        table.Header(header =>
        {
            header.Cell().Element(HeaderCell).Text("Kategorie").SemiBold();
            header.Cell().Element(HeaderCell).Text("Art").SemiBold();
            header.Cell().Element(HeaderCell).AlignRight().Text("Anzahl").SemiBold();
            header.Cell().Element(HeaderCell).AlignRight().Text("CHF").SemiBold();
        });

        foreach (var total in report.Categories)
        {
            table.Cell().Element(Cell).Text(total.Category);
            table.Cell().Element(Cell).Text(total.KindName);
            table.Cell().Element(Cell).AlignRight().Text(total.Bookings.ToString());
            table.Cell().Element(Cell).AlignRight().Text(SwissFormat.Amount(total.AmountChf));
        }
    }

    private static void Header(ColumnDescriptor column, string label, string? value)
    {
        if (value is not null)
        {
            column.Item().Text(text =>
            {
                text.Span($"{label}: ").SemiBold();
                text.Span(value);
            });
        }
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(0.75f).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).PaddingHorizontal(2);

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2);

    private static IContainer TotalCell(IContainer container) =>
        container.BorderTop(0.75f).BorderColor(Colors.Grey.Darken1).PaddingVertical(3).PaddingHorizontal(2);
}
