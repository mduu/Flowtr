using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Flowtr.Shared;
using Flowtr.Shared.Audit;

namespace Flowtr.Client.Features.Audit;

public sealed class HttpAuditLogReader(HttpClient http) : IAuditLogReader
{
    public async Task<Result<AuditPage>> Read(AuditFilter filter, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync(Url(filter), cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return Result.Failure<AuditPage>("Dafür hast du keine Berechtigung. Bitte melde dich neu an.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<AuditPage>($"Der Server antwortete mit Status {(int)response.StatusCode}.");
        }

        var page = await response.Content.ReadFromJsonAsync<AuditPage>(cancellationToken);
        return page is null ? Result.Failure<AuditPage>("Der Server hat keine Daten geliefert.") : Result.Success(page);
    }

    private static string Url(AuditFilter filter)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["from"] = filter.From?.ToString("O", CultureInfo.InvariantCulture),
            ["to"] = filter.To?.ToString("O", CultureInfo.InvariantCulture),
            ["user"] = filter.User,
            ["area"] = filter.Area,
            ["skip"] = filter.Skip.ToString(CultureInfo.InvariantCulture),
            ["take"] = filter.Take.ToString(CultureInfo.InvariantCulture),
        };

        return "api/audit?" + string.Join("&", parameters
            .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
            .Select(parameter => $"{parameter.Key}={Uri.EscapeDataString(parameter.Value!)}"));
    }
}
