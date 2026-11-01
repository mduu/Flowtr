using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;

namespace Flowtr.Server.CrossCutting.Hosting;

public static class HostingRegistration
{
    /// <summary>
    /// The blob container with the keys of Data Protection.
    /// </summary>
    public const string KeyContainer = "dataprotection";

    /// <summary>
    /// The database starts in the background. Until it is ready, <see cref="StartupGate"/> holds requests back.
    /// </summary>
    public static IServiceCollection AddDatabaseStartup(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseReadiness>();
        services.AddSingleton<DatabaseWakeInterceptor>();
        services.AddSingleton<IDatabasePreparation, DatabasePreparation>();
        services.AddHostedService<DatabaseStartup>();

        return services;
    }

    /// <summary>
    /// The keys for login cookies, invitation links and forms. A container loses its file system at each restart:
    /// the keys go to blob storage, in Azure protected with a key in Key Vault (<c>DataProtection:KeyVaultKey</c>).
    /// </summary>
    public static IServiceCollection AddKeyStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var protection = services.AddDataProtection()
            .SetApplicationName("Flowtr")
            .PersistKeysToAzureBlobStorage(provider =>
            {
                var container = provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(KeyContainer);
                container.CreateIfNotExists();
                return container.GetBlobClient("keys.xml");
            });

        if (configuration["DataProtection:KeyVaultKey"] is { Length: > 0 } key)
        {
            protection.ProtectKeysWithAzureKeyVault(new Uri(key), new DefaultAzureCredential());
        }

        return services;
    }

    /// <summary>
    /// Security headers, then the gate that holds requests back until the database is ready, then the authentication.
    /// </summary>
    /// <remarks>
    /// The authentication comes after the gate: it checks the login cookie against the database.
    /// </remarks>
    public static IApplicationBuilder UseSecurityHeadersAndStartupGate(this IApplicationBuilder app) => app
        .UseMiddleware<SecurityHeaders>()
        .UseMiddleware<StartupGate>()
        .UseAuthentication()
        .UseAuthorization();
}
