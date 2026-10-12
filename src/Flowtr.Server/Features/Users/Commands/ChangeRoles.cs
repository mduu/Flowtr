using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Features.Users.Commands;

/// <summary>
/// Sets the roles of a user. Flowtr always keeps at least one admin.
/// </summary>
public sealed record ChangeRoles(string UserId, IReadOnlyList<string> Roles);

public sealed class ChangeRolesHandler(UserManager<ApplicationUser> users, ILogger<ChangeRolesHandler> logger)
    : ICommandHandler<ChangeRoles, Result>
{
    public async Task<Result> Handle(ChangeRoles command, CancellationToken cancellationToken)
    {
        if (command.Roles.FirstOrDefault(role => !Roles.All.Contains(role)) is { } unknown)
        {
            return Result.Failure($"Die Rolle «{unknown}» gibt es nicht.");
        }

        if (await users.FindByIdAsync(command.UserId) is not { } user)
        {
            return Result.Failure("Diesen Benutzer gibt es nicht mehr.");
        }

        var current = await users.GetRolesAsync(user);
        var removed = current.Except(command.Roles).ToList();
        var added = command.Roles.Except(current).Distinct().ToList();

        if (removed.Contains(Roles.Admin) && await IsLastAdmin(user))
        {
            return Result.Failure("Flowtr braucht mindestens einen Admin. Gib zuerst einem anderen Benutzer die Rolle Admin.");
        }

        var result = await users.RemoveFromRolesAsync(user, removed);
        if (result.Succeeded)
        {
            result = await users.AddToRolesAsync(user, added);
        }

        if (!result.Succeeded)
        {
            return Result.Failure(result.ErrorText());
        }

        logger.LogInformation("Roles of user {UserId} changed: added {Added}, removed {Removed}", user.Id, added, removed);

        return Result.Success();
    }

    private async Task<bool> IsLastAdmin(ApplicationUser user)
    {
        var admins = await users.GetUsersInRoleAsync(Roles.Admin);
        return admins.All(admin => admin.Id == user.Id);
    }
}
