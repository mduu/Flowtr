using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Audit.Queries;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Mvc;

namespace Flowtr.Server.Features.Audit;

public static class AuditEndpoints
{
    /// <summary>
    /// Maps <c>audit</c> in the API group: read the audit log. Only for admins. There is no endpoint to change it.
    /// </summary>
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/audit", async (
                [AsParameters] AuditQuery query,
                IQueryHandler<GetAuditEntries, AuditPage> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetAuditEntries(query.ToFilter()), cancellationToken)).ToHttp())
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        return api;
    }

    private sealed record AuditQuery(
        [FromQuery] DateTimeOffset? From,
        [FromQuery] DateTimeOffset? To,
        [FromQuery] string? User,
        [FromQuery] string? Area,
        [FromQuery] int? Skip,
        [FromQuery] int? Take)
    {
        public AuditFilter ToFilter() => new(From, To, User, Area, Skip ?? 0, Take ?? 50);
    }
}
