using System.Net;
using System.Text;
using Ganss.Xss;
using MimeKit;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// Shows a mail receipt (<c>.eml</c>) in the browser (ADR-009): header and the cleaned HTML body.
/// Images only from the mail itself (<c>cid:</c> as <c>data:</c>), nothing from other servers.
/// </summary>
/// <remarks>
/// The page must go out with <see cref="ContentSecurityPolicy"/>: if the cleaning misses something, the browser still runs no script,
/// sends no cookie and loads nothing.
/// </remarks>
public static class MailView
{
    public const string ContentSecurityPolicy =
        "sandbox allow-popups allow-popups-to-escape-sandbox; default-src 'none'; img-src data:; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'";

    private const int MaxInlineImageSize = 2 * 1024 * 1024;

    public static string Render(byte[] mail, string originalUrl)
    {
        using var stream = new MemoryStream(mail);
        var message = MimeMessage.Load(stream);

        var body = message.HtmlBody is { } html
            ? Sanitizer(InlineImages(message)).Sanitize(html)
            : $"<pre class=\"text\">{Encode(message.TextBody ?? "")}</pre>";

        return $$"""
            <!doctype html>
            <html lang="de">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <base target="_blank">
            <title>{{Encode(message.Subject ?? "E-Mail")}}</title>
            <style>
              body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; background: #fff; color: #222; }
              .flowtr-mail { padding: 16px; border-bottom: 1px solid #ddd; background: #f5f5f8; font-size: 14px; line-height: 1.5; }
              .flowtr-mail h1 { margin: 0 0 8px; font-size: 18px; }
              .flowtr-mail dl { display: grid; grid-template-columns: max-content 1fr; gap: 0 12px; margin: 0; }
              .flowtr-mail dt { color: #666; }
              .flowtr-mail dd { margin: 0; overflow-wrap: anywhere; }
              .flowtr-mail a { color: #3f51b5; }
              .flowtr-body { padding: 16px; overflow-x: auto; }
              .text { white-space: pre-wrap; font-family: inherit; }
            </style>
            </head>
            <body>
            <header class="flowtr-mail">
              <h1>{{Encode(message.Subject ?? "")}}</h1>
              <dl>
                <dt>Von</dt><dd>{{Encode(message.From.ToString())}}</dd>
                <dt>An</dt><dd>{{Encode(message.To.ToString())}}</dd>
                <dt>Datum</dt><dd>{{message.Date.ToLocalTime():dd.MM.yyyy HH:mm}}</dd>
              </dl>
              <p style="margin: 8px 0 0"><a href="{{Encode(originalUrl)}}" target="_self">Original herunterladen (.eml)</a></p>
            </header>
            <main class="flowtr-body">
            {{body}}
            </main>
            </body>
            </html>
            """;
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static HtmlSanitizer Sanitizer(IReadOnlyDictionary<string, string> images)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("style");
        sanitizer.AllowedSchemes.Add("data");
        sanitizer.AllowedSchemes.Add("cid");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.FilterUrl += (_, args) =>
        {
            var url = args.OriginalUrl.Trim();
            var isImage = args.Tag?.LocalName == "img";
            if (url.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
            {
                args.SanitizedUrl = isImage && images.TryGetValue(url[4..].Trim('<', '>'), out var data) ? data : null;
            }
            else if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                args.SanitizedUrl = isImage && url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ? url : null;
            }
            else if (isImage || args.Tag?.LocalName != "a")
            {
                // No images, backgrounds or styles from other servers: they can track who reads the mail.
                args.SanitizedUrl = null;
            }
        };

        return sanitizer;
    }

    /// <summary>
    /// The images of the mail by their content ID, as <c>data:</c> URLs.
    /// </summary>
    private static Dictionary<string, string> InlineImages(MimeMessage message)
    {
        var images = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            if (string.IsNullOrEmpty(part.ContentId) || !part.ContentType.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || part.ContentType.MimeType.Contains("svg", StringComparison.OrdinalIgnoreCase) || part.Content is null)
            {
                continue;
            }

            using var buffer = new MemoryStream();
            part.Content.DecodeTo(buffer);
            if (buffer.Length <= MaxInlineImageSize)
            {
                images[part.ContentId] = $"data:{part.ContentType.MimeType};base64,{Convert.ToBase64String(buffer.GetBuffer(), 0, (int)buffer.Length)}";
            }
        }

        return images;
    }
}
