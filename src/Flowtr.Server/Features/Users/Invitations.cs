using System.Text;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Flowtr.Server.Features.Users;

/// <summary>
/// Makes and checks invitation links (ADR-004).
/// </summary>
public sealed class Invitations(UserManager<ApplicationUser> users)
{
    /// <summary>
    /// Flowtr deletes an invitation that nobody accepts after this time.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public const string NotFound = "Diese Einladung gibt es nicht mehr. Vielleicht hat ein Admin sie widerrufen. Bitte frage nach einer neuen Einladung.";

    public const string AlreadyAccepted = "Diese Einladung ist schon angenommen. Melde dich an.";

    public const string LinkNotValid = "Dieser Einladungslink ist abgelaufen oder nicht mehr gültig. Bitte frage einen Admin nach einem neuen Link.";

    /// <summary>
    /// Makes a new link for <paramref name="user"/>. <paramref name="baseUri"/> is the address of Flowtr and ends with <c>/</c>.
    /// </summary>
    public async Task<Invitation> CreateLink(ApplicationUser user, Uri baseUri)
    {
        var token = await users.GenerateUserTokenAsync(user, InvitationTokenProvider.ProviderName, InvitationTokenProvider.Purpose);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = new Uri(baseUri, $"Account/AcceptInvitation?userId={Uri.EscapeDataString(user.Id)}&code={code}");

        return new Invitation(user.Id, user.Email!, link.AbsoluteUri);
    }

    /// <summary>
    /// Finds the invited user of a link. Fails with a message for the user if the link is not valid.
    /// </summary>
    public async Task<Result<ApplicationUser>> Find(string? userId, string? code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code) || await users.FindByIdAsync(userId) is not { } user)
        {
            return Result.Failure<ApplicationUser>(NotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure<ApplicationUser>(AlreadyAccepted);
        }

        return await IsValid(user, code)
            ? Result.Success(user)
            : Result.Failure<ApplicationUser>(LinkNotValid);
    }

    private async Task<bool> IsValid(ApplicationUser user, string code)
    {
        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return false;
        }

        return await users.VerifyUserTokenAsync(user, InvitationTokenProvider.ProviderName, InvitationTokenProvider.Purpose, token);
    }
}
