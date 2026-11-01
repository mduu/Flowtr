using Flowtr.Client.Features.Business;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Features.Business.Commands;
using Flowtr.Server.Features.Business.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Business;
using Flowtr.Shared.Users;

namespace Flowtr.Server.Features.Business;

/// <summary>
/// <see cref="IBusinessApi"/> for prerendering: the handlers, with the same roles as the API.
/// </summary>
public sealed class ServerBusinessApi(IServiceProvider services, IHttpContextAccessor httpContextAccessor) : IBusinessApi
{
    private const string NotAllowed = "Dafür hast du keine Berechtigung.";

    public Task<Result<BusinessView>> Get(CancellationToken cancellationToken = default) =>
        Roles.All.Any(role => httpContextAccessor.HttpContext?.User.IsInRole(role) == true)
            ? services.GetRequiredService<IQueryHandler<GetBusiness, BusinessView>>().Handle(new GetBusiness(), cancellationToken)
            : Task.FromResult(Result.Failure<BusinessView>(NotAllowed));

    public Task<Result> Set(BusinessInput input, CancellationToken cancellationToken = default) =>
        httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) == true
            ? services.GetRequiredService<ICommandHandler<SetBusiness, Result>>().Handle(new SetBusiness(input), cancellationToken)
            : Task.FromResult(Result.Failure(NotAllowed));
}
