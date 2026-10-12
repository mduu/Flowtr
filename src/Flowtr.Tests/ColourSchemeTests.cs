using Flowtr.Client.Layout;
using Flowtr.Server.Features.Appearance;
using Microsoft.AspNetCore.Http;

namespace Flowtr.Tests;

/// <summary>
/// Dark mode: the cookie keeps the choice, without a cookie the system decides.
/// </summary>
public class ColourSchemeTests
{
    [Theory]
    [InlineData(null, ColourScheme.Auto)]
    [InlineData("hell", ColourScheme.Light)]
    [InlineData("dunkel", ColourScheme.Dark)]
    [InlineData("pink", ColourScheme.Auto)]
    public void Cookie_gives_the_scheme(string? cookie, ColourScheme expected)
    {
        // Arrange
        var context = new DefaultHttpContext();
        if (cookie is not null)
        {
            context.Request.Headers.Cookie = $"{ColourSchemeCookie.Name}={cookie}";
        }

        // Act
        var scheme = ColourSchemeCookie.Read(context);

        // Assert
        scheme.ShouldBe(expected);
    }

    [Theory]
    [InlineData("dunkel", "flowtr.darstellung=dunkel")]
    [InlineData("", "flowtr.darstellung=;")]
    public void Remember_sets_or_deletes_the_cookie(string choice, string expected)
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        ColourSchemeCookie.Remember(context, choice);

        // Assert
        context.Response.Headers.SetCookie.ToString().ShouldStartWith(expected);
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("Account/Login?ReturnUrl=%2F", "/Account/Login?ReturnUrl=%2F")]
    [InlineData("benutzer", "/benutzer")]
    [InlineData("//evil.example", "/evil.example")]
    [InlineData("/\\evil.example", "/")]
    public void Return_url_stays_on_this_site(string? returnUrl, string expected)
    {
        // Act
        var path = AppearanceEndpoints.LocalPath(returnUrl);

        // Assert
        path.ShouldBe(expected);
    }
}
