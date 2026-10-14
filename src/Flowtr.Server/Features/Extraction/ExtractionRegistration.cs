using Microsoft.Extensions.Http.Resilience;

namespace Flowtr.Server.Features.Extraction;

public static class ExtractionRegistration
{
    public static IServiceCollection AddExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.Section));

        // Reading a receipt can take half a minute: longer timeouts than the defaults, at most one retry.
        // RemoveAllResilienceHandlers is experimental (EXTEXP0001), but it is the only way to replace the default handler.
#pragma warning disable EXTEXP0001
        services.AddHttpClient<IReceiptReader, AnthropicReceiptReader>()
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(130);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
                options.Retry.MaxRetryAttempts = 1;
            });
#pragma warning restore EXTEXP0001

        services.AddHttpClient<IExchangeRates, FrankfurterExchangeRates>();

        return services;
    }
}
