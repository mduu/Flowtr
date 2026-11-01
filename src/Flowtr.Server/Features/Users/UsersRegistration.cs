using Flowtr.Client.Features.Users;
using Flowtr.Server.Components.Account;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.CrossCutting.Hosting;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowtr.Server.Features.Users;

public static class UsersRegistration
{
    /// <summary>
    /// Adds ASP.NET Core Identity with roles, invitations (ADR-004) and the configured admins.
    /// </summary>
    public static IServiceCollection AddUsers(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<GermanIdentityErrorDescriber>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddTokenProvider<InvitationTokenProvider>(InvitationTokenProvider.ProviderName)
            .AddClaimsPrincipalFactory<FlowtrClaimsPrincipalFactory>();

        // Role changes reach the cookie of a signed-in user within one minute.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddScoped<Invitations>();
        services.AddScoped<ConfiguredAdmins>();
        services.AddScoped<IUserAdministration, ServerUserAdministration>();
        services.AddHttpContextAccessor();
        services.AddHostedService<InvitationCleanup>();

        return services;
    }

    /// <summary>
    /// Sets up the configured admins when the server and the database are ready (the links need the address of the server).
    /// </summary>
    public static void SetUpConfiguredAdminsOnStart(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() => _ = SetUpConfiguredAdmins(app));
    }

    private static async Task SetUpConfiguredAdmins(WebApplication app)
    {
        try
        {
            await app.Services.GetRequiredService<DatabaseReadiness>().WaitUntilReady(app.Lifetime.ApplicationStopping);
            await using var scope = app.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetUpConfiguredAdmins, Result>>();
            await handler.Handle(new SetUpConfiguredAdmins(BaseUris.From(app)), app.Lifetime.ApplicationStopping);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            app.Logger.LogError(exception, "Configured admins not set up");
        }
    }
}
