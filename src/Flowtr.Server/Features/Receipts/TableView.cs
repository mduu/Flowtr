using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// Shows a table receipt (CSV, story #014) in the browser as an HTML table. Each cell is text: no formulas, no links.
/// </summary>
/// <remarks>
/// The page must go out with <see cref="ContentSecurityPolicy"/>: the browser runs no script and loads nothing.
/// </remarks>
public static partial class TableView
{
    public const string ContentSecurityPolicy =
        "sandbox allow-downloads; default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'";

    /// <summary>
    /// A longer table shows only its first rows. The original has all.
    /// </summary>
    public const int MaxRows = 5000;

    public static string Render(byte[] table, string fileName, string originalUrl)
    {
        var rows = TextTable.Decode(table) is { } text ? TextTable.Rows(text, MaxRows + 1) : [];
        var body = new StringBuilder();
        foreach (var (row, index) in rows.Take(MaxRows).Select((row, index) => (row, index)))
        {
            body.Append("<tr>");
            foreach (var cell in row)
            {
                var tag = index == 0 ? "th" : "td";
                body.Append(Number().IsMatch(cell) ? $"<{tag} class=\"num\">" : $"<{tag}>").Append(Encode(cell)).Append($"</{tag}>");
            }

            body.Append("</tr>\n");
        }

        var more = rows.Count > MaxRows ? $"<p class=\"more\">Die Ansicht zeigt die ersten {MaxRows} Zeilen. Das Original enthält alle.</p>" : "";

        return $$"""
            <!doctype html>
            <html lang="de">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{Encode(fileName)}}</title>
            <style>
              body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; background: #fff; color: #222; font-size: 13px; }
              header { padding: 16px; border-bottom: 1px solid #ddd; background: #f5f5f8; line-height: 1.5; }
              header h1 { margin: 0 0 4px; font-size: 18px; overflow-wrap: anywhere; }
              header a { color: #3f51b5; }
              main { padding: 16px; overflow-x: auto; }
              table { border-collapse: collapse; }
              th, td { padding: 4px 8px; border: 1px solid #ddd; text-align: left; vertical-align: top; white-space: pre-wrap; }
              th { background: #f5f5f8; font-weight: 600; }
              .num { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
              .more { color: #666; }
            </style>
            </head>
            <body>
            <header>
              <h1>{{Encode(fileName)}}</h1>
              <a href="{{Encode(originalUrl)}}">Original herunterladen (.csv)</a>
            </header>
            <main>
            <table>
            {{body}}</table>
            {{more}}
            </main>
            </body>
            </html>
            """;
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    [GeneratedRegex(@"^\s*[-+]?[\d'’.,]*\d\s*$")]
    private static partial Regex Number();
}
