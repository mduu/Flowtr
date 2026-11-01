using System.Net.Http.Json;
using Flowtr.Client.Api;
using Flowtr.Shared;
using Flowtr.Shared.Business;

namespace Flowtr.Client.Features.Business;

public sealed class HttpBusinessApi(HttpClient http) : IBusinessApi
{
    public Task<Result<BusinessView>> Get(CancellationToken cancellationToken = default) =>
        http.GetAsync("api/business", cancellationToken).Read<BusinessView>(cancellationToken);

    public Task<Result> Set(BusinessInput input, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync("api/business", input, cancellationToken).Read(cancellationToken);
}
