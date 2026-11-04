using System.Globalization;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Statistics;

namespace Flowtr.Client.Features.Statistics;

public sealed class HttpStatisticsApi(HttpClient http) : IStatisticsApi
{
    public Task<Result<StatisticsView>> Get(int months, CancellationToken cancellationToken = default) =>
        http.GetAsync($"api/statistics?months={months.ToString(CultureInfo.InvariantCulture)}", cancellationToken).Read<StatisticsView>(cancellationToken);
}
