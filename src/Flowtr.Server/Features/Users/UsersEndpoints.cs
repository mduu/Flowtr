using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Users.Commands;
using Flowtr.Server.Features.Users.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Users;

public static class UsersEndpoints
{
    /// <summary>
    /// Maps <c>users</c> in the API group: user list, invitations and roles. Only for admins.
    /// </summary>
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder api)
    {
        var users = api.MapGroup("/users").RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        users.MapGet("/", async (IQueryHandler<GetUsers, IReadOnlyList<UserSummary>> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetUsers(), cancellationToken)).ToHttp());

        users.MapPost("/invitations", async (
            InviteUserRequest request,
            HttpRequest http,
            IConfiguration configuration,
            ICommandHandler<InviteUser, Result<Invitation>> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new InviteUser(request.Email, request.Role, BaseUris.From(http, configuration)), cancellationToken)).ToHttp());

        users.MapPost("/{id}/invitation", async (
            string id,
            HttpRequest http,
            IConfiguration configuration,
            ICommandHandler<RenewInvitation, Result<Invitation>> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new RenewInvitation(id, BaseUris.From(http, configuration)), cancellationToken)).ToHttp());

        users.MapDelete("/{id}/invitation", async (string id, ICommandHandler<RevokeInvitation, Result> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new RevokeInvitation(id), cancellationToken)).ToHttp());

        users.MapPut("/{id}/roles", async (
            string id,
            ChangeRolesRequest request,
            ICommandHandler<ChangeRoles, Result> handler,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ChangeRoles(id, request.Roles), cancellationToken)).ToHttp());

        return api;
    }
}
