using System.Net.Http.Json;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Client.Features.Users;

/// <summary>
/// Calls <c>/api/users</c>.
/// </summary>
public sealed class HttpUserAdministration(HttpClient http) : IUserAdministration
{
    public Task<Result<IReadOnlyList<UserSummary>>> GetUsers(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/users", cancellationToken).Read<IReadOnlyList<UserSummary>>(cancellationToken);

    public Task<Result<Invitation>> Invite(InviteUserRequest request, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("api/users/invitations", request, cancellationToken).Read<Invitation>(cancellationToken);

    public Task<Result<Invitation>> RenewInvitation(string userId, CancellationToken cancellationToken = default) =>
        http.PostAsync($"api/users/{Uri.EscapeDataString(userId)}/invitation", null, cancellationToken).Read<Invitation>(cancellationToken);

    public Task<Result> RevokeInvitation(string userId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"api/users/{Uri.EscapeDataString(userId)}/invitation", cancellationToken).Read(cancellationToken);

    public Task<Result> ChangeRoles(string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(userId)}/roles", new ChangeRolesRequest(roles), cancellationToken).Read(cancellationToken);
}
