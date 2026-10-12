using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Deletes the user of an open invitation. Then the link does not work any more.
/// </summary>
public sealed record RevokeInvitation(string UserId);

public sealed class RevokeInvitationHandler(UserManager<ApplicationUser> users, ILogger<RevokeInvitationHandler> logger)
    : ICommandHandler<RevokeInvitation, Result>
{
    public async Task<Result> Handle(RevokeInvitation command, CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(command.UserId) is not { } user)
        {
            return Result.Failure("Diese Einladung gibt es nicht mehr.");
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure("Diese Einladung ist schon angenommen.");
        }

        var deleted = await users.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            return Result.Failure(deleted.ErrorText());
        }

        logger.LogInformation("Revoked invitation of user {UserId}", user.Id);

        return Result.Success();
    }
}
