using Flowtr.Client.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.AspNetCore.Components.Forms;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// <see cref="IReceiptApi"/> for prerendering. Upload and analysis only happen in the browser, after a user action.
/// </summary>
public sealed class ServerReceiptApi : IReceiptApi
{
    private const string BrowserOnly = "Das geht nur im Browser.";

    public Task<Result<UploadedReceipt>> Upload(IBrowserFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<UploadedReceipt>(BrowserOnly));

    public Task<Result<BookingProposal>> Analyze(Guid uploadId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<BookingProposal>(BrowserOnly));
}
