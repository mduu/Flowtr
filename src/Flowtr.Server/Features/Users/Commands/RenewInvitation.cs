using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Makes a new link for an open invitation. The old links become invalid, and the 30 days start again.
/// </summary>
public sealed record RenewInvitation(string UserId, Uri BaseUri);

public sealed class RenewInvitationHandler(
    UserManager<ApplicationUser> users,
    Invitations invitations,
    TimeProvider time,
    ILogger<RenewInvitationHandler> logger)
    : ICommandHandler<RenewInvitation, Result<Invitation>>
{
    public async Task<Result<Invitation>> Handle(RenewInvitation command, CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(command.UserId) is not { } user)
        {
            return Result.Failure<Invitation>("Diese Einladung gibt es nicht mehr.");
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure<Invitation>("Diese Einladung ist schon angenommen.");
        }

        user.InvitedAt = time.GetUtcNow();
        var updated = await users.UpdateSecurityStampAsync(user);
        if (!updated.Succeeded)
        {
            return Result.Failure<Invitation>(updated.ErrorText());
        }

        logger.LogInformation("Renewed invitation of user {UserId}", user.Id);

        return Result.Success(await invitations.CreateLink(user, command.BaseUri));
    }
}
