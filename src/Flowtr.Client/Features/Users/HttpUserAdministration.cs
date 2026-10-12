using System.Net;
using System.Net.Http.Json;
using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Client.Features.Users;

/// <summary>
/// Calls <c>/api/users</c>. The API sends errors as problem details with a German message in <c>detail</c>.
/// </summary>
public sealed class HttpUserAdministration(HttpClient http) : IUserAdministration
{
    public async Task<Result<IReadOnlyList<UserSummary>>> GetUsers(CancellationToken cancellationToken = default) =>
        await Read<IReadOnlyList<UserSummary>>(await http.GetAsync("api/users", cancellationToken), cancellationToken);

    public async Task<Result<Invitation>> Invite(InviteUserRequest request, CancellationToken cancellationToken = default) =>
        await Read<Invitation>(await http.PostAsJsonAsync("api/users/invitations", request, cancellationToken), cancellationToken);

    public async Task<Result<Invitation>> RenewInvitation(string userId, CancellationToken cancellationToken = default) =>
        await Read<Invitation>(await http.PostAsync($"api/users/{Uri.EscapeDataString(userId)}/invitation", null, cancellationToken), cancellationToken);

    public async Task<Result> RevokeInvitation(string userId, CancellationToken cancellationToken = default) =>
        await Read(await http.DeleteAsync($"api/users/{Uri.EscapeDataString(userId)}/invitation", cancellationToken), cancellationToken);

    public async Task<Result> ChangeRoles(string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default) =>
        await Read(await http.PutAsJsonAsync($"api/users/{Uri.EscapeDataString(userId)}/roles", new ChangeRolesRequest(roles), cancellationToken), cancellationToken);

    private static async Task<Result<T>> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<T>(await ErrorText(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return value is null ? Result.Failure<T>("Der Server hat keine Daten geliefert.") : Result.Success(value);
    }

    private static async Task<Result> Read(HttpResponseMessage response, CancellationToken cancellationToken) =>
        response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ErrorText(response, cancellationToken));

    private static async Task<string> ErrorText(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "Dafür hast du keine Berechtigung. Bitte melde dich neu an.";
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(cancellationToken);
            if (!string.IsNullOrEmpty(problem?.Detail))
            {
                return problem.Detail;
            }
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or NotSupportedException)
        {
        }

        return $"Der Server antwortete mit Status {(int)response.StatusCode}.";
    }

    private sealed record Problem(string? Detail);
}
