using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Statistics.Queries;
using Flowtr.Shared.Statistics;

namespace Flowtr.Server.Features.Statistics;

public static class StatisticsEndpoints
{
    /// <summary>
    /// <c>statistics</c> in the API group, for all roles.
    /// </summary>
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/statistics", async (int? months, IQueryHandler<GetStatistics, StatisticsView> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStatistics(months ?? StatisticsView.DefaultMonths), cancellationToken)).ToHttp());

        return api;
    }
}
