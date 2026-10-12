using Flowtr.Client.Features.Users;
using Flowtr.Server.Components.Account;
using Flowtr.Server.CrossCutting.Cqs;
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
    /// Sets up the configured admins when the server is ready (the links need its address).
    /// </summary>
    public static void SetUpConfiguredAdminsOnStart(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SetUpConfiguredAdmins, Result>>();
                handler.Handle(new SetUpConfiguredAdmins(BaseUris.From(app)), CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                app.Logger.LogError(exception, "Configured admins not set up");
            }
        });
    }
}
