using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Deletes the users of invitations that nobody accepted within <see cref="Invitations.Lifetime"/>.
/// </summary>
public sealed record DeleteExpiredInvitations;

public sealed class DeleteExpiredInvitationsHandler(
    UserManager<ApplicationUser> users,
    TimeProvider time,
    ILogger<DeleteExpiredInvitationsHandler> logger)
    : ICommandHandler<DeleteExpiredInvitations, Result<int>>
{
    public async Task<Result<int>> Handle(DeleteExpiredInvitations command, CancellationToken cancellationToken)
    {
        var limit = time.GetUtcNow() - Invitations.Lifetime;
        var expired = await users.Users
            .Where(user => !user.EmailConfirmed && user.InvitedAt <= limit)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var user in expired)
        {
            var result = await users.DeleteAsync(user);
            if (result.Succeeded)
            {
                deleted++;
                logger.LogInformation("Deleted expired invitation of user {UserId}", user.Id);
            }
            else
            {
                logger.LogWarning("Expired invitation of user {UserId} not deleted: {Errors}", user.Id, result.ErrorText());
            }
        }

        return Result.Success(deleted);
    }
}
