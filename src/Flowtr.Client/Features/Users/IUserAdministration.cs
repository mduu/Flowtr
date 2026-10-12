using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Client.Features.Users;

/// <summary>
/// User list for admins. In the browser it calls the API, on the server (prerendering) it calls the handlers.
/// </summary>
public interface IUserAdministration
{
    Task<Result<IReadOnlyList<UserSummary>>> GetUsers(CancellationToken cancellationToken = default);

    Task<Result<Invitation>> Invite(InviteUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<Invitation>> RenewInvitation(string userId, CancellationToken cancellationToken = default);

    Task<Result> RevokeInvitation(string userId, CancellationToken cancellationToken = default);

    Task<Result> ChangeRoles(string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);
}
