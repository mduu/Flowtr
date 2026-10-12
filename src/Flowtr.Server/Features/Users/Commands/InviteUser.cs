using System.ComponentModel.DataAnnotations;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Makes a user without password and gives the link with which the user completes the registration.
/// </summary>
public sealed record InviteUser(string Email, string Role, Uri BaseUri);

public sealed class InviteUserHandler(
    UserManager<ApplicationUser> users,
    Invitations invitations,
    TimeProvider time,
    ILogger<InviteUserHandler> logger)
    : ICommandHandler<InviteUser, Result<Invitation>>
{
    public async Task<Result<Invitation>> Handle(InviteUser command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return Result.Failure<Invitation>("Bitte eine gültige E-Mail-Adresse angeben.");
        }

        if (!Roles.All.Contains(command.Role))
        {
            return Result.Failure<Invitation>($"Die Rolle «{command.Role}» gibt es nicht.");
        }

        if (await users.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<Invitation>("Für diese E-Mail-Adresse gibt es schon ein Konto oder eine Einladung.");
        }

        var user = new ApplicationUser { UserName = email, Email = email, InvitedAt = time.GetUtcNow() };

        var created = await users.CreateAsync(user);
        if (!created.Succeeded)
        {
            return Result.Failure<Invitation>(created.ErrorText());
        }

        var roleAdded = await users.AddToRoleAsync(user, command.Role);
        if (!roleAdded.Succeeded)
        {
            await users.DeleteAsync(user);
            return Result.Failure<Invitation>(roleAdded.ErrorText());
        }

        logger.LogInformation("Invited user {UserId} with role {Role}", user.Id, command.Role);

        return Result.Success(await invitations.CreateLink(user, command.BaseUri));
    }
}
