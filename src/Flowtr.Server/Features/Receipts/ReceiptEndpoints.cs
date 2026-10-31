using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Extraction.Queries;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Receipts.Commands;
using Flowtr.Server.Features.Receipts.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts;

public static class ReceiptEndpoints
{
    /// <summary>
    /// Maps <c>receipts</c> in the API group: upload, preview of an upload, file of a filed receipt.
    /// A mail as receipt shows as a page (ADR-009); <c>?original=true</c> gives the <c>.eml</c>.
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

        receipts.MapGet("/uploads/{id:guid}", async (Guid id, bool? original, IQueryHandler<GetUploadFile, ReceiptFile> handler, CancellationToken cancellationToken) =>
            await File(await handler.Handle(new GetUploadFile(id), cancellationToken), original == true, cancellationToken));

        receipts.MapGet("/{id:guid}", async (Guid id, bool? original, IQueryHandler<GetReceiptFile, ReceiptFile> handler, CancellationToken cancellationToken) =>
            await File(await handler.Handle(new GetReceiptFile(id), cancellationToken), original == true, cancellationToken));

        return api;
    }

    /// <summary>
    /// Inline, thus the browser shows PDF and images in a tab. Private: the receipt must not stay in a shared cache.
    /// </summary>
    private static async Task<IResult> File(Result<ReceiptFile> result, bool original, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            return TypedResults.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        var file = result.Value!;
        if (file.ContentType != MailContentTypes.Mail)
        {
            return new ReceiptResult(TypedResults.Stream(file.Content, file.ContentType, enableRangeProcessing: true), file.FileName, Inline: true);
        }

        if (original)
        {
            return new ReceiptResult(TypedResults.Stream(file.Content, file.ContentType), file.FileName, Inline: false);
        }

        await using (file.Content)
        {
            using var mail = new MemoryStream();
            await file.Content.CopyToAsync(mail, cancellationToken);
            var page = MailView.Render(mail.ToArray(), "?original=true");
            return new ReceiptResult(TypedResults.Content(page, "text/html; charset=utf-8"), Path.ChangeExtension(file.FileName, ".html"), Inline: true, MailView.ContentSecurityPolicy);
        }
    }

    private sealed record ReceiptResult(IResult Inner, string FileName, bool Inline, string? ContentSecurityPolicy = null) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            var headers = httpContext.Response.Headers;
            headers.ContentDisposition = $"{(Inline ? "inline" : "attachment")}; filename*=UTF-8''{Uri.EscapeDataString(FileName)}";
            headers.CacheControl = "private, no-store";
            headers.XContentTypeOptions = "nosniff";
            if (ContentSecurityPolicy is not null)
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
            }

            return Inner.ExecuteAsync(httpContext);
        }
    }

    private sealed class RequestSizeLimitAttributeMetadata(long size) : Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize => size;
    }
}
