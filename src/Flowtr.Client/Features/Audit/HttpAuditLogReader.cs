using System.Globalization;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Audit;

namespace Flowtr.Client.Features.Audit;

public sealed class HttpAuditLogReader(HttpClient http) : IAuditLogReader
{
    public Task<Result<AuditPage>> Read(AuditFilter filter, CancellationToken cancellationToken = default) =>
        http.GetAsync(ApiResults.Query("api/audit", new Dictionary<string, string?>
            {
                ["from"] = filter.From?.ToString("O", CultureInfo.InvariantCulture),
                ["to"] = filter.To?.ToString("O", CultureInfo.InvariantCulture),
                ["user"] = filter.User,
                ["area"] = filter.Area,
                ["skip"] = filter.Skip.ToString(CultureInfo.InvariantCulture),
                ["take"] = filter.Take.ToString(CultureInfo.InvariantCulture),
            }), cancellationToken)
            .Read<AuditPage>(cancellationToken);
}
