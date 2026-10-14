using System.Net.Http.Headers;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.AspNetCore.Components.Forms;

namespace Flowtr.Client.Features.Receipts;

public sealed class HttpReceiptApi(HttpClient http) : IReceiptApi
{
    /// <summary>
    /// The server checks the size again. This limit only stops the browser from reading a too large file.
    /// </summary>
    private const long MaxSize = 20 * 1024 * 1024;

    public async Task<Result<UploadedReceipt>> Upload(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file.Size > MaxSize)
        {
            return Result.Failure<UploadedReceipt>("Die Datei ist zu gross (höchstens 20 MB).");
        }

        using var content = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream(MaxSize, cancellationToken));
        stream.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(stream, "file", file.Name);

        return await http.PostAsync("api/receipts/uploads", content, cancellationToken).Read<UploadedReceipt>(cancellationToken);
    }

    public Task<Result<BookingProposal>> Analyze(Guid uploadId, CancellationToken cancellationToken = default) =>
        http.PostAsync($"api/receipts/uploads/{uploadId}/analysis", null, cancellationToken).Read<BookingProposal>(cancellationToken);
}
