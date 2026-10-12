using Flowtr.Client.Features.Audit;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Audit.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Audit;

/// <summary>
/// <see cref="IAuditLogReader"/> for prerendering on the server. It calls the handler directly.
/// </summary>
public sealed class ServerAuditLogReader(IQueryHandler<GetAuditEntries, AuditPage> handler, IHttpContextAccessor httpContextAccessor)
    : IAuditLogReader
{
    public Task<Result<AuditPage>> Read(AuditFilter filter, CancellationToken cancellationToken = default) =>
        httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) == true
            ? handler.Handle(new GetAuditEntries(filter), cancellationToken)
            : Task.FromResult(Result.Failure<AuditPage>("Dafür hast du keine Berechtigung."));
}
