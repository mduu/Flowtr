using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Gives the Admin role to the configured admins (<see cref="ConfiguredAdmins"/>).
/// For an address without an accepted account, it writes an invitation link to the log.
/// </summary>
public sealed record SetUpConfiguredAdmins(Uri BaseUri);

public sealed class SetUpConfiguredAdminsHandler(
    UserManager<ApplicationUser> users,
    ConfiguredAdmins admins,
    ICommandHandler<InviteUser, Result<Invitation>> invite,
    Invitations invitations,
    ILogger<SetUpConfiguredAdminsHandler> logger)
    : ICommandHandler<SetUpConfiguredAdmins, Result>
{
    public async Task<Result> Handle(SetUpConfiguredAdmins command, CancellationToken cancellationToken)
    {
        foreach (var address in admins.Addresses)
        {
            var user = await users.FindByEmailAsync(address);

            if (user is { EmailConfirmed: true })
            {
                await admins.Grant(user);
                continue;
            }

            // An open invitation gets one more link. The links in the log of earlier starts stay valid.
            var invitation = user is null
                ? await invite.Handle(new InviteUser(address, Roles.Admin, command.BaseUri), cancellationToken)
                : Result.Success(await invitations.CreateLink(user, command.BaseUri));

            if (invitation.IsSuccess)
            {
                logger.LogWarning("Configured admin {Email} has no account yet. Invitation link: {Link}", address, invitation.Value!.Link);
            }
            else
            {
                logger.LogError("No invitation for configured admin {Email}: {Error}", address, invitation.Error);
            }
        }

        return Result.Success();
    }
}
