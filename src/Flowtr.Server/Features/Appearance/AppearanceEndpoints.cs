using Microsoft.AspNetCore.Mvc;

namespace Flowtr.Server.Features.Appearance;

public static class AppearanceEndpoints
{
    /// <summary>
    /// Maps <c>POST /darstellung</c>: keeps the colour scheme and goes back to the page of the user.
    /// </summary>
    public static IEndpointRouteBuilder MapAppearanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/darstellung", (HttpContext context, [FromForm] string? choice, [FromForm] string? returnUrl) =>
        {
            ColourSchemeCookie.Remember(context, choice);
            return TypedResults.LocalRedirect(LocalPath(returnUrl));
        });

        return endpoints;
    }

    /// <summary>
    /// Makes a path on this site from a base-relative URL. Gives <c>/</c> for a URL that can go to a different site.
    /// </summary>
    public static string LocalPath(string? returnUrl)
    {
        var path = "/" + (returnUrl ?? "").TrimStart('/');

        return path.Contains('\\') || path.Any(char.IsControl) ? "/" : path;
    }
}
