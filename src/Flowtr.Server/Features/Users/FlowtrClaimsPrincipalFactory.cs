using System.Security.Claims;
using Flowtr.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Users;

/// <summary>
/// Gives the Admin role to configured admins before Identity makes the claims.
/// Identity calls it at each sign-in and each refresh of the cookie, thus the role is in the cookie at once.
/// </summary>
public sealed class FlowtrClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options,
    ConfiguredAdmins admins)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        await admins.Grant(user);
        return await base.GenerateClaimsAsync(user);
    }
}
