using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Export.Queries;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Export;

public static class ExportRegistration
{
    public static IServiceCollection AddExport(this IServiceCollection services)
    {
        services.AddSingleton<IPdfDocuments, QuestPdfDocuments>();
        return services;
    }

    /// <summary>
    /// <c>GET /api/export?from=2026-01-01&amp;to=2026-12-31&amp;receipts=true</c>: the ZIP as download, for all roles that see the journal.
    /// </summary>
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/export", async (DateOnly from, DateOnly to, bool? receipts, IQueryHandler<ExportJournal, ExportFile> handler, HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var user = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? context.User.Identity?.Name ?? "";
            var result = await handler.Handle(new ExportJournal(from, to, receipts ?? true, user), cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
            }

            context.Response.Headers.CacheControl = "private, no-store";
            return Results.File(result.Value!.Content, "application/zip", result.Value.FileName);
        });

        return api;
    }
}
