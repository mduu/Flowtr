using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.AspNetCore.Components.Forms;

namespace Flowtr.Client.Features.Receipts;

/// <summary>
/// Upload of receipts and their addresses for preview and opening.
/// </summary>
public interface IReceiptApi
{
    Task<Result<UploadedReceipt>> Upload(IBrowserFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the uploads of one receipt together with AI and gives one booking proposal (story #009, #012).
    /// </summary>
    Task<Result<BookingProposal>> Analyze(IReadOnlyList<Guid> uploadIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Relative addresses of receipt files. The browser opens them with the identity cookie.
/// </summary>
public static class ReceiptUrls
{
    public static string Upload(Guid uploadId) => $"api/receipts/uploads/{uploadId}";

    public static string File(Guid fileId) => $"api/receipts/files/{fileId}";

    /// <summary>
    /// Browsers can show these images. HEIC only Safari: Flowtr shows an icon instead.
    /// </summary>
    public static bool IsPreviewImage(string? contentType) => contentType is "image/jpeg" or "image/png" or "image/webp";
}
