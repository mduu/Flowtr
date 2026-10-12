using System.Reflection;

namespace Flowtr.Server.CrossCutting.Cqs;

/// <summary>
/// Registers all command and query handlers of an assembly.
/// </summary>
public static class HandlerRegistration
{
    private static readonly Type[] handlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    /// <summary>
    /// Registers each non-abstract class that implements a handler interface as a scoped service.
    /// </summary>
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var registrations =
            from type in assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from @interface in type.GetInterfaces()
            where @interface.IsGenericType && handlerInterfaces.Contains(@interface.GetGenericTypeDefinition())
            select (@interface, type);

        foreach (var (@interface, type) in registrations)
        {
            services.AddScoped(@interface, type);
        }

        return services;
    }
}
