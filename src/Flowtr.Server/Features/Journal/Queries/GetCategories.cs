using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Queries;

/// <summary>
/// All categories, income first, each in its order, with the number of bookings.
/// </summary>
public sealed record GetCategories;

public sealed class GetCategoriesHandler(ApplicationDbContext db) : IQueryHandler<GetCategories, IReadOnlyList<CategoryView>>
{
    public async Task<Result<IReadOnlyList<CategoryView>>> Handle(GetCategories query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryView> categories = await db.Categories
            .OrderBy(c => c.Kind == BookingKind.Income ? 0 : 1)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryView(c.Id, c.Name, c.Kind, c.IsActive, db.Bookings.Count(b => b.CategoryId == c.Id)))
            .ToListAsync(cancellationToken);

        return Result.Success(categories);
    }
}
