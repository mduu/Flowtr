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
            ToHttp(await handler.Handle(new GetUsers(), cancellationToken)));

        users.MapPost("/invitations", async (
            InviteUserRequest request,
            HttpRequest http,
            IConfiguration configuration,
            ICommandHandler<InviteUser, Result<Invitation>> handler,
            CancellationToken cancellationToken) =>
            ToHttp(await handler.Handle(new InviteUser(request.Email, request.Role, BaseUris.From(http, configuration)), cancellationToken)));

        users.MapPost("/{id}/invitation", async (
            string id,
            HttpRequest http,
            IConfiguration configuration,
            ICommandHandler<RenewInvitation, Result<Invitation>> handler,
            CancellationToken cancellationToken) =>
            ToHttp(await handler.Handle(new RenewInvitation(id, BaseUris.From(http, configuration)), cancellationToken)));

        users.MapDelete("/{id}/invitation", async (string id, ICommandHandler<RevokeInvitation, Result> handler, CancellationToken cancellationToken) =>
            ToHttp(await handler.Handle(new RevokeInvitation(id), cancellationToken)));

        users.MapPut("/{id}/roles", async (
            string id,
            ChangeRolesRequest request,
            ICommandHandler<ChangeRoles, Result> handler,
            CancellationToken cancellationToken) =>
            ToHttp(await handler.Handle(new ChangeRoles(id, request.Roles), cancellationToken)));

        return api;
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : TypedResults.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
}
