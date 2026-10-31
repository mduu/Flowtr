using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using MimeKit;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// A mail as text for the AI (ADR-009): header and body. HTML keeps only its structure (tables, lists, paragraphs),
/// without styles, scripts, images and attributes. This is short and keeps amounts next to their labels.
/// </summary>
public static partial class MailText
{
    public const int MaxLength = 100_000;

    private static readonly HashSet<string> structure =
        ["table", "tr", "td", "th", "ul", "ol", "li", "p", "h1", "h2", "h3", "h4", "h5", "h6"];

    private static readonly HashSet<string> blocks =
        ["div", "section", "article", "header", "footer", "center", "blockquote", "pre", "address", "dl", "dt", "dd"];

    private static readonly HashSet<string> ignored =
        ["head", "script", "style", "noscript", "template", "img", "svg", "title", "meta", "link"];

    public static string ForReader(byte[] mail)
    {
        using var stream = new MemoryStream(mail);
        var message = MimeMessage.Load(stream);

        var text = new StringBuilder();
        Append(text, message);
        foreach (var attached in message.BodyParts.OfType<MessagePart>().Select(part => part.Message).OfType<MimeMessage>())
        {
            text.AppendLine().AppendLine("--- Attached mail ---");
            Append(text, attached);
        }

        return MailReceipts.Shorten(text.ToString(), MaxLength);
    }

    /// <summary>
    /// The text of an HTML body with line breaks, for example to find the sender of a forwarded mail.
    /// </summary>
    public static string PlainText(string html) => Simplify(html, markup: false);

    private static void Append(StringBuilder text, MimeMessage message)
    {
        text.AppendLine($"From: {message.From}");
        text.AppendLine($"To: {message.To}");
        text.AppendLine($"Date: {message.Date:yyyy-MM-dd HH:mm zzz}");
        text.AppendLine($"Subject: {message.Subject}");
        text.AppendLine();
        text.AppendLine(message.HtmlBody is { } html ? Simplify(html, markup: true) : message.TextBody?.Trim() ?? "");
    }

    private static string Simplify(string html, bool markup)
    {
        var document = new HtmlParser().ParseDocument(html);
        var text = new StringBuilder();
        Write(document.Body ?? (INode)document.DocumentElement, text, markup);
        return BlankLines().Replace(Spaces().Replace(text.ToString(), " "), "\n").Trim();
    }

    private static void Write(INode node, StringBuilder text, bool markup)
    {
        switch (node)
        {
            case IText textNode:
                text.Append(textNode.Data);
                break;
            case IElement element when ignored.Contains(element.LocalName):
                break;
            case IElement { LocalName: "br" or "hr" }:
                text.Append(markup ? "<br>" : "\n");
                break;
            case IElement element when structure.Contains(element.LocalName):
                text.Append(markup ? $"<{element.LocalName}{Span(element)}>" : Separator(element.LocalName));
                WriteChildren(element, text, markup);
                text.Append(markup ? $"</{element.LocalName}>\n" : "\n");
                break;
            case IElement element:
                WriteChildren(element, text, markup);
                if (blocks.Contains(element.LocalName))
                {
                    text.Append('\n');
                }

                break;
        }
    }

    private static void WriteChildren(INode node, StringBuilder text, bool markup)
    {
        foreach (var child in node.ChildNodes)
        {
            Write(child, text, markup);
        }
    }

    private static string Span(IElement element) =>
        element.GetAttribute("colspan") is { } colspan && colspan != "1" ? $" colspan=\"{colspan}\"" : "";

    private static string Separator(string tag) => tag is "td" or "th" ? " | " : "\n";

    [GeneratedRegex(@"[ \t \r]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\s*\n\s*")]
    private static partial Regex BlankLines();
}
