using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowtr.Server.CrossCutting.Audit;

public static class AuditRegistration
{
    /// <summary>
    /// Adds the services for the audit log. Add <see cref="AuditInterceptor"/> to the DbContext options.
    /// </summary>
    public static IServiceCollection AddAudit(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IAuditActor, AuditActor>();
        services.AddSingleton<AuditInterceptor>();

        return services;
    }
}
