using Microsoft.AspNetCore.Components;

namespace Flowtr.Tests;

/// <summary>
/// Account pages: passkey and password login only (ADR-002), static SSR (ADR-003).
/// </summary>
public class AccountRoutesTests
{
    private static readonly string[] routes = typeof(Program).Assembly.GetTypes()
        .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), inherit: false))
        .Cast<RouteAttribute>()
        .Select(route => route.Template)
        .ToArray();

    [Theory]
    [InlineData("2fa")]
    [InlineData("TwoFactor")]
    [InlineData("Authenticator")]
    [InlineData("RecoveryCode")]
    [InlineData("ExternalLogin")]
    public void No_account_page_for_removed_login_method(string fragment)
    {
        // Assert
        routes.ShouldNotContain(route => route.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Account_pages_stay_static_server_rendered()
    {
        // Arrange
        var accountPages = typeof(Program).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Any(route => route.Template.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Assert (Identity needs HttpContext for cookies, ADR-003)
        accountPages.ShouldNotBeEmpty();
        accountPages.ShouldAllBe(page => page.IsDefined(typeof(ExcludeFromInteractiveRoutingAttribute), true));
    }

    [Fact]
    public void Passkey_management_page_exists()
    {
        // Assert
        routes.ShouldContain("/Account/Manage/Passkeys");
    }
}
