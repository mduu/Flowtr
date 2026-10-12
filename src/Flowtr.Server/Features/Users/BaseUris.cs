namespace Flowtr.Server.Features.Users;

/// <summary>
/// The address of Flowtr for links. The configuration value <c>PublicUrl</c> has priority.
/// </summary>
internal static class BaseUris
{
    public static Uri From(HttpRequest request, IConfiguration configuration) =>
        Configured(configuration) ?? WithSlash($"{request.Scheme}://{request.Host}{request.PathBase}");

    public static Uri From(WebApplication app) =>
        Configured(app.Configuration)
        ?? WithSlash(app.Urls.FirstOrDefault(url => url.StartsWith("https:", StringComparison.OrdinalIgnoreCase)) ?? app.Urls.First());

    private static Uri? Configured(IConfiguration configuration) =>
        configuration["PublicUrl"] is { Length: > 0 } url ? WithSlash(url) : null;

    private static Uri WithSlash(string url) => new(url.EndsWith('/') ? url : url + "/");
}
