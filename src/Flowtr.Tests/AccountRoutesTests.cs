using Microsoft.AspNetCore.Components;

namespace Flowtr.Tests;

/// <summary>
/// Flowtr only supports passkey and password login (story #001, ADR-002).
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
    public void Passkey_management_page_exists()
    {
        // Assert
        routes.ShouldContain("/Account/Manage/Passkeys");
    }
}
