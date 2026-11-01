using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// The culture of Flowtr for numbers and dates, on the server and in the browser: configuration <c>Culture</c>, default
/// <c>de-CH</c>. The language of the browser does not change it: a decimal comma (for example <c>de-DE</c>) must not break
/// the input of amounts. The browser gets it through <c>&lt;html lang&gt;</c> (<c>js/start.js</c>).
/// </summary>
public sealed class AppCulture
{
    public const string Section = "Culture";

    public const string Default = "de-CH";

    public AppCulture(IConfiguration configuration)
    {
        var name = configuration[Section] is { Length: > 0 } configured ? configured : Default;
        try
        {
            Culture = CultureInfo.GetCultureInfo(name, predefinedOnly: true);
        }
        catch (CultureNotFoundException exception)
        {
            throw new InvalidOperationException($"Unknown culture '{name}' in the configuration '{Section}'.", exception);
        }
    }

    public CultureInfo Culture { get; }
}

public static class AppCultureRegistration
{
    public static IServiceCollection AddAppCulture(this IServiceCollection services, IConfiguration configuration)
    {
        // Checked at the start: a wrong value stops Flowtr at once, not at the first request.
        var appCulture = new AppCulture(configuration);
        services.AddSingleton(appCulture);
        CultureInfo.DefaultThreadCurrentCulture = appCulture.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = appCulture.Culture;
        return services;
    }

    /// <summary>
    /// Each request uses the culture of Flowtr, whatever the browser asks for.
    /// </summary>
    public static IApplicationBuilder UseAppCulture(this IApplicationBuilder app)
    {
        var culture = app.ApplicationServices.GetRequiredService<AppCulture>().Culture;
        return app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(culture),
            SupportedCultures = [culture],
            SupportedUICultures = [culture],
            RequestCultureProviders = [],
        });
    }
}
