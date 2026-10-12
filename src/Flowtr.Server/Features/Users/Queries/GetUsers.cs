using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Users.Queries;

/// <summary>
/// All users with their roles, including open invitations.
/// </summary>
public sealed record GetUsers;

public sealed class GetUsersHandler(ApplicationDbContext db) : IQueryHandler<GetUsers, IReadOnlyList<UserSummary>>
{
    public async Task<Result<IReadOnlyList<UserSummary>>> Handle(GetUsers query, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email, user.EmailConfirmed, user.InvitedAt })
            .ToListAsync(cancellationToken);

        var roles = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        IReadOnlyList<UserSummary> summaries = users
            .Select(user => new UserSummary(
                user.Id,
                user.Email ?? "",
                roles.Where(role => role.UserId == user.Id).Select(role => role.Name!).Order().ToList(),
                InvitationOpen: !user.EmailConfirmed,
                user.InvitedAt))
            .ToList();

        return Result.Success(summaries);
    }
}
