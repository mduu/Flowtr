using System.Text;
using Flowtr.Server.Features.Receipts.Commands;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Tests.Receipts;

internal static class ReceiptTestData
{
    public static byte[] Pdf(string text = "Rechnung") => Encoding.ASCII.GetBytes($"%PDF-1.7\n% {text}\n%%EOF");

    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13];

    public static async Task<Result<UploadedReceipt>> Upload(this TestApp app, byte[] content, string fileName = "rechnung.pdf") =>
        await app.Command<UploadReceipt, Result<UploadedReceipt>>(new UploadReceipt(new MemoryStream(content), fileName));

    public static async Task<Guid> UploadPdf(this TestApp app, string text = "Rechnung")
    {
        var result = await app.Upload(Pdf(text));
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value!.UploadId;
    }
}
