using Flowtr.Server.Features.Inbox.Mail;
using MimeKit;
using static Flowtr.Tests.Inbox.MailTestData;

namespace Flowtr.Tests.Inbox;

/// <summary>
/// ADR-009: a mail as receipt shows in the browser without scripts and without content from other servers.
/// </summary>
public class MailViewTests
{
    [Fact]
    public void Page_has_no_script_and_no_external_image_but_the_mail_images()
    {
        // Arrange
        var body = new BodyBuilder { HtmlBody = StripeHtml + "<a href=\"https://stripe.com/rechnung\">PDF</a><a href=\"javascript:alert(1)\">x</a>" };
        body.LinkedResources.Add("logo.png", Image(20_000)).ContentId = "logo@stripe";
        var mail = Mail(subject: "<b>Rechnung</b>");
        mail.Body = body.ToMessageBody();

        // Act
        var page = MailView.Render(Bytes(mail), "?original=true");

        // Assert
        page.ShouldNotContain("<script");
        page.ShouldNotContain("alert");
        page.ShouldNotContain("onclick");
        page.ShouldNotContain("track.example.com");
        page.ShouldContain("<img src=\"data:image/png;base64,");
        page.ShouldContain("href=\"https://stripe.com/rechnung\"");
        page.ShouldContain("<td>CHF 33.00</td>");
        page.ShouldContain("<title>&lt;b&gt;Rechnung&lt;/b&gt;</title>");
        page.ShouldContain("href=\"?original=true\"");
    }

    [Fact]
    public void Text_mail_shows_as_text()
    {
        // Arrange
        var mail = Mail(text: "Betrag: CHF 12.00\n<script>alert(1)</script>");

        // Act
        var page = MailView.Render(Bytes(mail), "?original=true");

        // Assert
        page.ShouldContain("Betrag: CHF 12.00\n&lt;script&gt;");
    }

    [Fact]
    public void Policy_stops_scripts_and_external_content()
    {
        MailView.ContentSecurityPolicy.ShouldStartWith("sandbox ");
        MailView.ContentSecurityPolicy.ShouldNotContain("allow-scripts");
        MailView.ContentSecurityPolicy.ShouldNotContain("allow-same-origin");
        MailView.ContentSecurityPolicy.ShouldContain("default-src 'none'");
    }

    private static byte[] Bytes(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return stream.ToArray();
    }
}
