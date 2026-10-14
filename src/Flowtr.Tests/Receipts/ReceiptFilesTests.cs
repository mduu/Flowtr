using System.Text;
using Flowtr.Server.Features.Receipts;

namespace Flowtr.Tests.Receipts;

public class ReceiptFilesTests
{
    [Theory]
    [InlineData("%PDF-1.7", "application/pdf")]
    [InlineData("\xFF\xD8\xFF\xE0", "image/jpeg")]
    [InlineData("\x89PNG\r\n\x1A\n", "image/png")]
    [InlineData("RIFF\0\0\0\0WEBPVP8 ", "image/webp")]
    [InlineData("\0\0\0\u0018ftypheic", "image/heic")]
    [InlineData("Hallo Welt", null)]
    [InlineData("<html>", null)]
    public void Type_comes_from_the_content(string head, string? contentType)
    {
        // Act
        var detected = ReceiptFiles.Detect(Encoding.Latin1.GetBytes(head));

        // Assert
        detected?.ContentType.ShouldBe(contentType);
        (detected is null).ShouldBe(contentType is null);
    }

    [Theory]
    [InlineData("Büro-Stuhl (neu)", "buero-stuhl-neu")]
    [InlineData("Café crème", "cafe-creme")]
    [InlineData("!!!", "beleg")]
    [InlineData("Ein sehr langer Text für eine Beschreibung mit vielen Wörtern", "ein-sehr-langer-text-fuer-eine-beschreib")]
    public void Slug_is_short_ascii(string text, string slug)
    {
        // Assert
        ReceiptFiles.Slug(text).ShouldBe(slug);
    }

    [Fact]
    public void Blob_name_follows_adr_001()
    {
        // Assert
        ReceiptFiles.BlobName(2026, income: false, "A-2026-007", "Azure Abo", ".pdf").ShouldBe("2026/ausgaben/A-2026-007_azure-abo.pdf");
        ReceiptFiles.BlobName(2026, income: true, "E-2026-001", "Stripe", ".png").ShouldBe("2026/einnahmen/E-2026-001_stripe.png");
    }
}
