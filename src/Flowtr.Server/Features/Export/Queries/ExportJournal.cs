using System.Globalization;
using System.IO.Compression;
using System.Text;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Flowtr.Server.Features.Export.Queries;

/// <summary>
/// Journal and receipts of a period as ZIP (story #013): <c>Journal.xlsx</c>, <c>Journal.pdf</c>, <c>Belegverzeichnis.csv</c>
/// and the receipts in <c>Belege/Ausgaben</c> and <c>Belege/Einnahmen</c>, unchanged. A mail as receipt also comes as PDF (ADR-009).
/// </summary>
/// <remarks>
/// The ZIP is built in a temporary file: the response streams it, and the file goes when the stream closes.
/// </remarks>
/// <param name="CreatedBy">The user who exports, for the header of the documents.</param>
public sealed record ExportJournal(DateOnly From, DateOnly To, bool WithReceipts = true, string CreatedBy = "");

public sealed class ExportJournalHandler(
    ApplicationDbContext db,
    IReceiptStore store,
    IPdfDocuments pdf,
    TimeProvider time,
    ILogger<ExportJournalHandler> logger)
    : IQueryHandler<ExportJournal, ExportFile>
{
    public const string MissingFiles = "Fehlende Belege.txt";

    public const string Index = "Belegverzeichnis.csv";

    private static readonly TimeZoneInfo swissTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    public async Task<Result<ExportFile>> Handle(ExportJournal query, CancellationToken cancellationToken)
    {
        if (query.From > query.To)
        {
            return Result.Failure<ExportFile>("Das Ende des Zeitraums liegt vor dem Anfang.");
        }

        var report = await Report(query, cancellationToken);
        var path = Path.GetTempFileName();
        try
        {
            await using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                await Add(zip, "Journal.xlsx", JournalWorkbook.Build(report), cancellationToken);
                await Add(zip, "Journal.pdf", pdf.Journal(report), cancellationToken);
                await Add(zip, Index, IndexCsv(report), cancellationToken);
                if (query.WithReceipts)
                {
                    await AddReceipts(zip, report, cancellationToken);
                }
            }

            var content = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            logger.LogInformation("Exported {Count} bookings from {From} to {To}", report.Rows.Count, query.From, query.To);
            return Result.Success(new ExportFile(content, $"Flowtr-Export_{query.From:yyyy-MM-dd}_{query.To:yyyy-MM-dd}_erstellt-{report.CreatedAt:yyyy-MM-dd-HHmm}.zip"));
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    private async Task<ExportReport> Report(ExportJournal query, CancellationToken cancellationToken)
    {
        var bookings = db.Bookings.AsNoTracking().Where(b => b.Date >= query.From && b.Date <= query.To);

        var rows = await bookings
            .OrderBy(b => b.Date).ThenBy(b => b.CreatedAt)
            .Select(b => new ExportRow(
                b.Date,
                b.Receipt != null ? b.Receipt.Number : null,
                b.Kind,
                b.Category!.Name,
                b.Description,
                b.Counterparty != null ? b.Counterparty.Name : null,
                b.PaymentMethod != null ? b.PaymentMethod.Name : null,
                b.AmountChf,
                b.OriginalAmount,
                b.OriginalCurrency,
                b.Note,
                db.ReceiptFiles.Count(f => f.ReceiptId == b.ReceiptId)))
            .ToListAsync(cancellationToken);

        var files = await bookings
            .Where(b => b.ReceiptId != null)
            .Join(db.ReceiptFiles, b => b.ReceiptId, f => f.ReceiptId, (b, f) => new
            {
                Number = b.Receipt!.Number, b.Date, b.Kind, b.Description, f.Position, f.BlobName, f.FileName, f.ContentType, f.Size, f.Sha256,
            })
            .OrderBy(f => f.Date).ThenBy(f => f.Number).ThenBy(f => f.Position)
            .ToListAsync(cancellationToken);

        var counts = files.GroupBy(f => f.Number).ToDictionary(g => g.Key, g => g.Count());
        var index = files.Select((f, i) => new ReceiptIndexRow(
                f.BlobName,
                f.Number,
                $"{files.Take(i + 1).Count(other => other.Number == f.Number)} von {counts[f.Number]}", f.Date, f.Kind, f.Description, query.WithReceipts ? ZipPath(f.Kind, f.BlobName) : null, f.FileName, f.ContentType, f.Size, f.Sha256))
            .ToList();

        var business = await db.BusinessSettings.AsNoTracking().Select(b => b.Name).FirstOrDefaultAsync(cancellationToken);
        var createdAt = TimeZoneInfo.ConvertTime(time.GetUtcNow(), swissTime).DateTime;
        var createdBy = string.IsNullOrWhiteSpace(query.CreatedBy) ? "unbekannt" : query.CreatedBy;

        return new ExportReport(query.From, query.To, rows, index, business, createdAt, createdBy, await Status(query, cancellationToken));
    }

    /// <summary>
    /// A closed year is final. An open year can still change: the export is provisional.
    /// </summary>
    private async Task<string> Status(ExportJournal query, CancellationToken cancellationToken)
    {
        var years = Enumerable.Range(query.From.Year, query.To.Year - query.From.Year + 1).ToList();
        var closed = await db.ClosedYears.AsNoTracking().Where(c => years.Contains(c.Year)).ToListAsync(cancellationToken);
        var open = years.Except(closed.Select(c => c.Year)).ToList();

        return open.Count == 0
            ? $"Abgeschlossen am {TimeZoneInfo.ConvertTime(closed.Max(c => c.ClosedAt), swissTime):dd.MM.yyyy}."
            : $"Provisorisch: {string.Join(", ", open)} {(open.Count == 1 ? "ist" : "sind")} nicht abgeschlossen. Buchungen können sich noch ändern.";
    }

    /// <summary>
    /// All files of the receipts of the bookings in the period, in folders by kind. A missing blob goes to a list in the ZIP:
    /// the export still works, and the fiduciary sees what is missing.
    /// </summary>
    private async Task AddReceipts(ZipArchive zip, ExportReport report, CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        foreach (var file in report.Files)
        {
            await using var content = await store.OpenReceipt(file.BlobName, cancellationToken);
            if (content is null)
            {
                logger.LogError("Receipt file {Path} is missing in the export", file.Path);
                missing.Add(Path.GetFileName(file.Path!));
                continue;
            }

            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            await Add(zip, file.Path!, buffer.ToArray(), cancellationToken);

            if (file.ContentType == MailContentTypes.Mail)
            {
                await Add(zip, Path.ChangeExtension(file.Path!, ".pdf"), pdf.Mail(Print(buffer.ToArray())), cancellationToken);
            }
        }

        if (missing.Count > 0)
        {
            var text = "Diese Dateien fehlen in der Ablage von Flowtr:\n" + string.Join("\n", missing) + "\n";
            await Add(zip, MissingFiles, Encoding.UTF8.GetBytes(text), cancellationToken);
        }
    }

    private static string ZipPath(BookingKind kind, string blobName) =>
        $"Belege/{(kind == BookingKind.Income ? "Einnahmen" : "Ausgaben")}/{Path.GetFileName(blobName)}";

    /// <summary>
    /// The index of the receipt files as CSV: semicolons and a byte order mark, so that Excel opens it directly.
    /// </summary>
    public static byte[] IndexCsv(ExportReport report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Belegnummer;Datei;Datum;Art;Beschreibung;Datei im ZIP;Originalname;Dateityp;Grösse (Bytes);SHA-256");
        foreach (var file in report.Files)
        {
            csv.AppendLine(string.Join(";",
                file.Number, file.File, SwissFormat.Date(file.Date), file.KindName, Csv(file.Description), Csv(file.Path ?? ""), Csv(file.FileName),
                file.ContentType, file.Size.ToString(CultureInfo.InvariantCulture), file.Sha256));
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    private static string Csv(string value) =>
        value.IndexOfAny([';', '"', '\n', '\r']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// A mail as text for the PDF: header and body, the HTML without markup.
    /// </summary>
    public static MailPrint Print(byte[] eml)
    {
        using var stream = new MemoryStream(eml);
        var message = MimeMessage.Load(stream);
        var body = message.TextBody is { Length: > 0 } text
            ? text
            : message.HtmlBody is { Length: > 0 } html ? MailText.PlainText(html) : "";

        return new MailPrint(
            message.From.ToString(),
            message.To.ToString(),
            message.Date == DateTimeOffset.MinValue ? null : message.Date,
            message.Subject ?? "",
            body.Trim());
    }

    private static async Task Add(ZipArchive zip, string name, byte[] content, CancellationToken cancellationToken)
    {
        await using var entry = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
        await entry.WriteAsync(content, cancellationToken);
    }
}

/// <summary>
/// The caller disposes <see cref="Content"/>. Closing it deletes the temporary file.
/// </summary>
public sealed record ExportFile(Stream Content, string FileName);
