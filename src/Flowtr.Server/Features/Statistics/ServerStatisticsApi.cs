using Flowtr.Client.Features.Statistics;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Statistics.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Statistics;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Statistics;

/// <summary>
/// <see cref="IStatisticsApi"/> for prerendering: the handler, with the same roles as the API.
/// </summary>
public sealed class ServerStatisticsApi(IServiceProvider services, IHttpContextAccessor httpContextAccessor) : IStatisticsApi
{
    public Task<Result<StatisticsView>> Get(int months, CancellationToken cancellationToken = default) =>
        Roles.All.Any(role => httpContextAccessor.HttpContext?.User.IsInRole(role) == true)
            ? services.GetRequiredService<IQueryHandler<GetStatistics, StatisticsView>>().Handle(new GetStatistics(months), cancellationToken)
            : Task.FromResult(Result.Failure<StatisticsView>("Dafür hast du keine Berechtigung."));
}
