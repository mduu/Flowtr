using System.Text;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// A table as text (CSV or TSV) as receipt (story #014). Such a file has no signature: Flowtr checks the content.
/// </summary>
public static class TextTable
{
    private static readonly char[] Separators = [';', ',', '\t', '|'];

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly Encoding Utf16LittleEndian = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

    private static readonly Encoding Utf16BigEndian = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

    /// <summary>
    /// True for text with at least two rows of at least two cells, without control characters and without markup.
    /// </summary>
    public static bool IsTable(ReadOnlySpan<byte> content) => Decode(content) is { } text && IsTable(text);

    /// <summary>
    /// The text of the file: UTF-8 with or without byte order mark, or UTF-16 with byte order mark. Null for other content.
    /// </summary>
    public static string? Decode(ReadOnlySpan<byte> content)
    {
        try
        {
            return content switch
            {
                [0xEF, 0xBB, 0xBF, ..] => Utf8.GetString(content[3..]),
                [0xFF, 0xFE, ..] => Utf16LittleEndian.GetString(content[2..]),
                [0xFE, 0xFF, ..] => Utf16BigEndian.GetString(content[2..]),
                _ => Utf8.GetString(content),
            };
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>
    /// The rows of the table, at most <paramref name="maxRows"/>. Cells in double quotes can contain separators and line breaks (RFC 4180).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> Rows(string text, int maxRows = int.MaxValue)
    {
        var separator = Separator(text);
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < text.Length && rows.Count < maxRows; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c != '"')
                {
                    cell.Append(c);
                }
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    quoted = false;
                }
            }
            else if (c == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (c == separator)
            {
                row.Add(cell.ToString());
                cell.Clear();
            }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                EndRow();
            }
            else
            {
                cell.Append(c);
            }
        }

        if (rows.Count < maxRows)
        {
            EndRow();
        }

        return rows;

        void EndRow()
        {
            row.Add(cell.ToString());
            cell.Clear();
            if (row.Count > 1 || row[0].Length > 0)
            {
                rows.Add(row);
            }

            row = [];
        }
    }

    private static bool IsTable(string text)
    {
        if (text.TrimStart().StartsWith('<') || text.Any(c => char.IsControl(c) && c is not ('\t' or '\r' or '\n')))
        {
            return false;
        }

        var rows = Rows(text, maxRows: 50);
        return rows.Count >= 2 && rows[0].Count >= 2 && rows.Skip(1).Any(row => row.Count >= 2);
    }

    /// <summary>
    /// The separator that the first line has most often. Semicolon when it has none.
    /// </summary>
    private static char Separator(string text)
    {
        var end = text.IndexOfAny(['\r', '\n']);
        var firstLine = end < 0 ? text : text[..end];
        return Separators.MaxBy(separator => firstLine.Count(c => c == separator));
    }
}
