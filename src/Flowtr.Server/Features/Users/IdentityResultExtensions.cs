using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users;

internal static class IdentityResultExtensions
{
    public static string ErrorText(this IdentityResult result) => string.Join(" ", result.Errors.Select(error => error.Description));
}
