using Flowtr.Server.CrossCutting.Api;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Business.Commands;
using Flowtr.Server.Features.Business.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Business;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Business;

public static class BusinessEndpoints
{
    /// <summary>
    /// <c>business</c> in the API group: read for all roles, change only for admins.
    /// </summary>
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/business", async (IQueryHandler<GetBusiness, BusinessView> handler, CancellationToken cancellationToken) =>
            (await handler.Handle(new GetBusiness(), cancellationToken)).ToHttp());

        api.MapPut("/business", async (BusinessInput input, ICommandHandler<SetBusiness, Result> handler, CancellationToken cancellationToken) =>
                (await handler.Handle(new SetBusiness(input), cancellationToken)).ToHttp())
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));

        return api;
    }
}
