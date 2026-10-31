using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Inbox.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Tests.Receipts;
using MimeKit;

namespace Flowtr.Tests.Inbox;

internal static class MailTestData
{
    public const string User = "marc@example.ch";

    public const string StripeHtml = """
        <html><head><style>.x { color: red }</style><script>alert('x')</script></head>
        <body><h1>Ihre Rechnung</h1><img src="https://track.example.com/pixel.gif">
        <table><tr><td>Betrag</td><td>CHF 33.00</td></tr></table>
        <p onclick="evil()">Danke</p><img src="cid:logo@stripe"></body></html>
        """;

    /// <summary>
    /// A mail from <paramref name="from"/>. Each attachment is (file name, content); <paramref name="html"/> is the body.
    /// </summary>
    public static MimeMessage Mail(
        string from = User,
        string subject = "Rechnung Oktober",
        string? html = null,
        string? text = "Siehe Anhang.",
        string? messageId = null,
        string? authenticationResults = null,
        params (string Name, byte[] Content)[] attachments)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse("belege@flowtr.test"));
        message.Subject = subject;
        message.Date = new DateTimeOffset(2026, 10, 11, 9, 30, 0, TimeSpan.FromHours(2));
        message.MessageId = messageId ?? $"{Guid.NewGuid()}@example.ch";
        if (authenticationResults is not null)
        {
            message.Headers.Insert(0, "Authentication-Results", authenticationResults);
        }

        var body = new BodyBuilder { TextBody = text, HtmlBody = html };
        foreach (var (name, content) in attachments)
        {
            body.Attachments.Add(name, content);
        }

        message.Body = body.ToMessageBody();
        return message;
    }

    /// <summary>
    /// A PNG of the given size: big enough for a photo of a receipt, or small like an icon.
    /// </summary>
    public static byte[] Image(int size)
    {
        var image = new byte[size];
        ReceiptTestData.Png.CopyTo(image, 0);
        image[^1] = (byte)size;
        return image;
    }

    public static Task<Result<FetchResult>> Fetch(this TestApp app) =>
        app.Command<FetchMail, Result<FetchResult>>(new FetchMail());

    public static async Task<FetchResult> FetchOk(this TestApp app)
    {
        var result = await app.Fetch();
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value!;
    }
}
