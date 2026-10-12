using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Completes the registration: sets the password and confirms the address (the link came by e-mail).
/// A password is necessary, because a passkey alone has no recovery (ADR-002).
/// </summary>
public sealed record AcceptInvitation(string? UserId, string? Code, string Password);

public sealed class AcceptInvitationHandler(
    UserManager<ApplicationUser> users,
    Invitations invitations,
    ILogger<AcceptInvitationHandler> logger)
    : ICommandHandler<AcceptInvitation, Result>
{
    public async Task<Result> Handle(AcceptInvitation command, CancellationToken cancellationToken)
    {
        var found = await invitations.Find(command.UserId, command.Code);
        if (!found.IsSuccess)
        {
            return Result.Failure(found.Error!);
        }

        var user = found.Value!;

        // Changes the security stamp, thus the link becomes invalid.
        var passwordAdded = await users.AddPasswordAsync(user, command.Password);
        if (!passwordAdded.Succeeded)
        {
            return Result.Failure(passwordAdded.ErrorText());
        }

        user.EmailConfirmed = true;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return Result.Failure(updated.ErrorText());
        }

        logger.LogInformation("User {UserId} accepted the invitation", user.Id);

        return Result.Success();
    }
}
