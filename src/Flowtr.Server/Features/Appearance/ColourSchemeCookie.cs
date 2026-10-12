using Flowtr.Client.Layout;

namespace Flowtr.Server.Features.Appearance;

/// <summary>
/// Keeps the colour scheme of the user in a cookie. Without the cookie, the scheme is <see cref="ColourScheme.Auto"/>.
/// </summary>
/// <remarks>The cookie holds only <c>hell</c> or <c>dunkel</c>.</remarks>
public static class ColourSchemeCookie
{
    public const string Name = "flowtr.darstellung";

    public const string Light = "hell";

    public const string Dark = "dunkel";

    public static ColourScheme Read(HttpContext context) => context.Request.Cookies[Name] switch
    {
        Light => ColourScheme.Light,
        Dark => ColourScheme.Dark,
        _ => ColourScheme.Auto,
    };

    /// <summary>
    /// Keeps <paramref name="choice"/> for one year. All other values delete the cookie, thus the scheme becomes <see cref="ColourScheme.Auto"/>.
    /// </summary>
    public static void Remember(HttpContext context, string? choice)
    {
        if (choice is Light or Dark)
        {
            context.Response.Cookies.Append(Name, choice, new CookieOptions
            {
                MaxAge = TimeSpan.FromDays(365),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                IsEssential = true,
            });
        }
        else
        {
            context.Response.Cookies.Delete(Name);
        }
    }
}
