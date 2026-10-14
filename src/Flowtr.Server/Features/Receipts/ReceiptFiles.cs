using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// Which files Flowtr accepts as receipts, and how it names them.
/// </summary>
public static partial class ReceiptFiles
{
    public const long MaxSize = 20 * 1024 * 1024;

    /// <summary>
    /// Finds the type from the first bytes of the file. The file name and the browser's type are not trusted.
    /// </summary>
    /// <returns>Content type and file extension, or null for a type that Flowtr does not accept.</returns>
    public static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> head)
    {
        if (head.StartsWith("%PDF-"u8))
        {
            return ("application/pdf", ".pdf");
        }

        if (head.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return ("image/jpeg", ".jpg");
        }

        if (head.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ("image/png", ".png");
        }

        if (head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8))
        {
            return ("image/webp", ".webp");
        }

        if (head.Length >= 12 && head[4..8].SequenceEqual("ftyp"u8) && IsHeif(head[8..12]))
        {
            return ("image/heic", ".heic");
        }

        return null;
    }

    /// <summary>
    /// The blob name of a filed receipt, for example <c>2026/ausgaben/A-2026-007_azure-abo.pdf</c> (ADR-001).
    /// </summary>
    public static string BlobName(int year, bool income, string number, string description, string extension) =>
        $"{year}/{(income ? "einnahmen" : "ausgaben")}/{number}_{Slug(description)}{extension}";

    /// <summary>
    /// Lower case ASCII with dashes, at most 40 characters. «Büro-Stuhl (neu)» gives <c>buero-stuhl-neu</c>.
    /// </summary>
    public static string Slug(string text)
    {
        var replaced = text.ToLowerInvariant().Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var ascii = new StringBuilder();
        foreach (var c in replaced.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                ascii.Append(c);
            }
        }

        var slug = NotLetterOrDigit().Replace(ascii.ToString(), "-").Trim('-');
        slug = slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
        return slug.Length == 0 ? "beleg" : slug;
    }

    private static bool IsHeif(ReadOnlySpan<byte> brand) =>
        brand.SequenceEqual("heic"u8) || brand.SequenceEqual("heix"u8) || brand.SequenceEqual("mif1"u8) || brand.SequenceEqual("msf1"u8);

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotLetterOrDigit();
}
