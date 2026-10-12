using Flowtr.Client.Features.Users;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Server.Features.Users.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Users;

/// <summary>
/// <see cref="IUserAdministration"/> for prerendering on the server. It calls the handlers directly.
/// </summary>
public sealed class ServerUserAdministration(IServiceProvider services, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    : IUserAdministration
{
    private const string NotAllowed = "Dafür hast du keine Berechtigung.";

    public Task<Result<IReadOnlyList<UserSummary>>> GetUsers(CancellationToken cancellationToken = default) =>
        IsAdmin
            ? Query<GetUsers, IReadOnlyList<UserSummary>>(new GetUsers(), cancellationToken)
            : Task.FromResult(Result.Failure<IReadOnlyList<UserSummary>>(NotAllowed));

    public Task<Result<Invitation>> Invite(InviteUserRequest request, CancellationToken cancellationToken = default) =>
        IsAdmin
            ? Command<InviteUser, Result<Invitation>>(new InviteUser(request.Email, request.Role, BaseUri), cancellationToken)
            : Task.FromResult(Result.Failure<Invitation>(NotAllowed));

    public Task<Result<Invitation>> RenewInvitation(string userId, CancellationToken cancellationToken = default) =>
        IsAdmin
            ? Command<RenewInvitation, Result<Invitation>>(new RenewInvitation(userId, BaseUri), cancellationToken)
            : Task.FromResult(Result.Failure<Invitation>(NotAllowed));

    public Task<Result> RevokeInvitation(string userId, CancellationToken cancellationToken = default) =>
        IsAdmin
            ? Command<RevokeInvitation, Result>(new RevokeInvitation(userId), cancellationToken)
            : Task.FromResult(Result.Failure(NotAllowed));

    public Task<Result> ChangeRoles(string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default) =>
        IsAdmin
            ? Command<ChangeRoles, Result>(new ChangeRoles(userId, roles), cancellationToken)
            : Task.FromResult(Result.Failure(NotAllowed));

    private HttpContext HttpContext => httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP request.");

    private bool IsAdmin => HttpContext.User.IsInRole(Roles.Admin);

    private Uri BaseUri => BaseUris.From(HttpContext.Request, configuration);

    private Task<Result<TResult>> Query<TQuery, TResult>(TQuery query, CancellationToken cancellationToken) =>
        services.GetRequiredService<IQueryHandler<TQuery, TResult>>().Handle(query, cancellationToken);

    private Task<TResult> Command<TCommand, TResult>(TCommand command, CancellationToken cancellationToken) where TResult : Result =>
        services.GetRequiredService<ICommandHandler<TCommand, TResult>>().Handle(command, cancellationToken);
}
