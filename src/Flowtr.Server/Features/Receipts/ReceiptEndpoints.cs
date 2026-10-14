using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Receipts.Commands;
using Flowtr.Server.Features.Receipts.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts;

public static class ReceiptEndpoints
{
    /// <summary>
    /// Maps <c>receipts</c> in the API group: upload, preview of an upload, file of a filed receipt.
    /// </summary>
    /// <remarks>
    /// The upload takes no antiforgery token: the WebAssembly app sends it with fetch. The identity cookie is
    /// <c>SameSite=Lax</c>, thus a form of another site cannot send it with a POST.
    /// </remarks>
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder api)
    {
        var receipts = api.MapGroup("/receipts");

        receipts.MapPost("/uploads", async (IFormFile file, ICommandHandler<UploadReceipt, Result<UploadedReceipt>> handler, CancellationToken cancellationToken) =>
            {
                await using var content = file.OpenReadStream();
                return (await handler.Handle(new UploadReceipt(content, file.FileName), cancellationToken)).ToHttp();
            })
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttributeMetadata(ReceiptFiles.MaxSize + 1024 * 1024));

        receipts.MapPost("/uploads/{id:guid}/analysis", async (Guid id, IQueryHandler<AnalyzeUpload, BookingProposal> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new AnalyzeUpload(id), cancellationToken)).ToHttp());

        receipts.MapGet("/uploads/{id:guid}", async (Guid id, IQueryHandler<GetUploadFile, ReceiptFile> handler, CancellationToken cancellationToken) =>
            File(await handler.Handle(new GetUploadFile(id), cancellationToken)));

        receipts.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetReceiptFile, ReceiptFile> handler, CancellationToken cancellationToken) =>
            File(await handler.Handle(new GetReceiptFile(id), cancellationToken)));

        return api;
    }

    /// <summary>
    /// Inline, thus the browser shows PDF and images in a tab. Private: the receipt must not stay in a shared cache.
    /// </summary>
    private static IResult File(Result<ReceiptFile> result)
    {
        if (!result.IsSuccess)
        {
            return TypedResults.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        var file = result.Value!;
        return new InlineFile(TypedResults.Stream(file.Content, file.ContentType, enableRangeProcessing: true), file.FileName);
    }

    private sealed class InlineFile(IResult inner, string fileName) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
            httpContext.Response.Headers.CacheControl = "private, no-store";
            return inner.ExecuteAsync(httpContext);
        }
    }

    private sealed class RequestSizeLimitAttributeMetadata(long size) : Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize => size;
    }
}
