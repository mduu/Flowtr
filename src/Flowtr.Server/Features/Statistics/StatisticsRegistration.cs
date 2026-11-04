using Flowtr.Client.Features.Statistics;

namespace Flowtr.Server.Features.Statistics;

public static class StatisticsRegistration
{
    public static IServiceCollection AddStatistics(this IServiceCollection services) =>
        services.AddScoped<IStatisticsApi, ServerStatisticsApi>();
}
