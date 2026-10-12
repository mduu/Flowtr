using Flowtr.Server.Data;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users;

/// <summary>
/// The e-mail addresses in the configuration section <c>Admins</c> always get the Admin role.
/// With this, the first admin of a new installation gets access, and an admin who lost access gets it back.
/// </summary>
public sealed class ConfiguredAdmins(UserManager<ApplicationUser> users, IConfiguration configuration, ILogger<ConfiguredAdmins> logger)
{
    public const string Section = "Admins";

    public IReadOnlyList<string> Addresses => configuration.GetSection(Section).Get<string[]>() ?? [];

    /// <summary>
    /// Gives the Admin role to a listed user with a confirmed address. It never removes the role: admins manage roles in the user list.
    /// </summary>
    /// <remarks>Only confirmed addresses: else an invitation for the address would give admin rights to who has the link.</remarks>
    public async Task Grant(ApplicationUser user)
    {
        if (!user.EmailConfirmed || !IsListed(user.Email) || await users.IsInRoleAsync(user, Roles.Admin))
        {
            return;
        }

        var result = await users.AddToRoleAsync(user, Roles.Admin);
        if (result.Succeeded)
        {
            logger.LogInformation("Configured admin {UserId} got the Admin role", user.Id);
        }
        else
        {
            logger.LogWarning("Configured admin {UserId} did not get the Admin role: {Errors}", user.Id, result.ErrorText());
        }
    }

    private bool IsListed(string? email) =>
        email is not null && Addresses.Contains(email, StringComparer.OrdinalIgnoreCase);
}
