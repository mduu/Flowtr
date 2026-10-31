using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared.Inbox;
using MimeKit;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// Finds the receipts in a mail (ADR-009): attachments as PDF or image. A mail without such an attachment is itself the receipt (<c>.eml</c>).
/// </summary>
public static partial class MailReceipts
{
    /// <summary>
    /// Smaller images are logos or icons, not receipts.
    /// </summary>
    public const int MinImageSize = 10 * 1024;

    public static IReadOnlyList<MailFile> Files(MimeMessage message)
    {
        var files = new List<MailFile>();
        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            if (part is TextPart || part.Content is null)
            {
                continue;
            }

            using var buffer = new MemoryStream();
            part.Content.DecodeTo(buffer);
            var content = buffer.ToArray();
            if (content.Length is 0 or > (int)ReceiptFiles.MaxSize
                || ReceiptFiles.Detect(content.AsSpan(0, Math.Min(content.Length, 32))) is not var (contentType, extension)
                || (contentType.StartsWith("image/", StringComparison.Ordinal) && IsDecoration(part, content.Length)))
            {
                continue;
            }

            var fileName = string.IsNullOrWhiteSpace(part.FileName) ? $"anhang-{files.Count + 1}{extension}" : Path.GetFileName(part.FileName);
            files.Add(new MailFile(fileName, contentType, content));
        }

        return files.Count > 0 ? files : [MailItself(message)];
    }

    /// <summary>
    /// The key that finds a mail again: the <c>Message-ID</c>, else sender, date and subject.
    /// </summary>
    public static string MessageKey(MimeMessage message)
    {
        var id = string.IsNullOrWhiteSpace(message.MessageId)
            ? $"{message.From}|{message.Date:O}|{message.Subject}"
            : message.MessageId;
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(id)));
    }

    public static string? Sender(MimeMessage message) =>
        message.From.Mailboxes.FirstOrDefault()?.Address?.Trim().ToLowerInvariant();

    /// <summary>
    /// The sender of a forwarded mail: the sender of an attached mail, or the first «From:» line in the text.
    /// </summary>
    public static string? OriginalSender(MimeMessage message)
    {
        if (message.BodyParts.OfType<MessagePart>().FirstOrDefault()?.Message?.From.ToString() is { Length: > 0 } attached)
        {
            return Shorten(attached, 300);
        }

        foreach (var text in new[] { message.TextBody, message.HtmlBody is { } html ? MailText.PlainText(html) : null })
        {
            if (text is not null && ForwardedFrom().Match(text) is { Success: true } match)
            {
                return Shorten(match.Groups["sender"].Value.Replace("*", "").Trim(), 300);
            }
        }

        return null;
    }

    public static string Shorten(string text, int length) => text.Length <= length ? text : text[..length];

    private static bool IsDecoration(MimePart part, int size) =>
        !string.IsNullOrEmpty(part.ContentId) || size < MinImageSize;

    private static MailFile MailItself(MimeMessage message)
    {
        using var buffer = new MemoryStream();
        message.WriteTo(buffer);
        var name = ReceiptFiles.Slug(string.IsNullOrWhiteSpace(message.Subject) ? "e-mail" : message.Subject);
        return new MailFile($"{name}.eml", MailContentTypes.Mail, buffer.ToArray());
    }

    [GeneratedRegex(@"^[ \t>*]*(?:From|Von|De|Da)[ \t*]*:[ \t*]*(?<sender>\S.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex ForwardedFrom();
}

/// <summary>
/// A receipt from a mail. <see cref="ContentType"/> is the detected type, not the type that the mail states.
/// </summary>
public sealed record MailFile(string FileName, string ContentType, byte[] Content);
