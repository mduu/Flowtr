using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Business;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Business.Queries;

public sealed record GetBusiness;

public sealed class GetBusinessHandler(ApplicationDbContext db) : IQueryHandler<GetBusiness, BusinessView>
{
    public async Task<Result<BusinessView>> Handle(GetBusiness query, CancellationToken cancellationToken)
    {
        var name = await db.BusinessSettings.AsNoTracking().Select(b => b.Name).FirstOrDefaultAsync(cancellationToken);
        return Result.Success(new BusinessView(name));
    }
}
