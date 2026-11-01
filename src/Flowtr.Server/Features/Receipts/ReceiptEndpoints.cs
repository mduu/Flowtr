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
    /// Maps <c>receipts</c> in the API group: upload, analysis of uploads, preview of an upload, file of a filed receipt.
    /// A mail (ADR-009) and a table (story #014) show as a page; <c>?original=true</c> gives the original file.
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

        receipts.MapPost("/uploads/analysis", async (AnalysisInput input, IQueryHandler<AnalyzeUploads, BookingProposal> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new AnalyzeUploads(input.UploadIds), cancellationToken)).ToHttp());

        receipts.MapGet("/uploads/{id:guid}", async (Guid id, bool? original, IQueryHandler<GetUploadFile, FileContent> handler, CancellationToken cancellationToken) =>
            await File(await handler.Handle(new GetUploadFile(id), cancellationToken), original == true, cancellationToken));

        receipts.MapGet("/files/{id:guid}", async (Guid id, bool? original, IQueryHandler<GetReceiptFile, FileContent> handler, CancellationToken cancellationToken) =>
            await File(await handler.Handle(new GetReceiptFile(id), cancellationToken), original == true, cancellationToken));

        return api;
    }

    /// <summary>
    /// Inline, thus the browser shows PDF and images in a tab. Private: the receipt must not stay in a shared cache.
    /// </summary>
    private static async Task<IResult> File(Result<FileContent> result, bool original, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            return TypedResults.Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
        }

        var file = result.Value!;
        var hasPage = file.ContentType is MailContentTypes.Mail or ReceiptContentTypes.Table;
        if (!hasPage)
        {
            return new ReceiptResult(TypedResults.Stream(file.Content, file.ContentType, enableRangeProcessing: true), file.FileName, Inline: true);
        }

        if (original)
        {
            return new ReceiptResult(TypedResults.Stream(file.Content, file.ContentType), file.FileName, Inline: false);
        }

        await using (file.Content)
        {
            using var content = new MemoryStream();
            await file.Content.CopyToAsync(content, cancellationToken);
            return file.ContentType == MailContentTypes.Mail
                ? new ReceiptResult(TypedResults.Content(MailView.Render(content.ToArray(), "?original=true"), "text/html; charset=utf-8"),
                    Path.ChangeExtension(file.FileName, ".html"), Inline: true, MailView.ContentSecurityPolicy)
                : new ReceiptResult(TypedResults.Content(TableView.Render(content.ToArray(), file.FileName, "?original=true"), "text/html; charset=utf-8"),
                    Path.ChangeExtension(file.FileName, ".html"), Inline: true, TableView.ContentSecurityPolicy);
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
