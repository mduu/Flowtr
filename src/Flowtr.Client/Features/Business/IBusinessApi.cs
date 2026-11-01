using Flowtr.Shared;
using Flowtr.Shared.Business;

namespace Flowtr.Client.Features.Business;

/// <summary>
/// The name of the business. In the browser it calls the API, on the server (prerendering) the handlers.
/// </summary>
public interface IBusinessApi
{
    Task<Result<BusinessView>> Get(CancellationToken cancellationToken = default);

    Task<Result> Set(BusinessInput input, CancellationToken cancellationToken = default);
}
