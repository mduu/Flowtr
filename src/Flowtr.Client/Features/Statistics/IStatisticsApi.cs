using Flowtr.Shared;
using Flowtr.Shared.Statistics;

namespace Flowtr.Client.Features.Statistics;

/// <summary>
/// Totals per month. In the browser it calls the API, on the server (prerendering) the handler.
/// </summary>
public interface IStatisticsApi
{
    Task<Result<StatisticsView>> Get(int months, CancellationToken cancellationToken = default);
}
