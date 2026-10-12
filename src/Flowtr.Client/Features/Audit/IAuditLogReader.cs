using Flowtr.Shared;
using Flowtr.Shared.Audit;

namespace Flowtr.Client.Features.Audit;

/// <summary>
/// Reads the audit log (admins only). In the browser it calls the API, on the server (prerendering) the handler.
/// </summary>
public interface IAuditLogReader
{
    Task<Result<AuditPage>> Read(AuditFilter filter, CancellationToken cancellationToken = default);
}
