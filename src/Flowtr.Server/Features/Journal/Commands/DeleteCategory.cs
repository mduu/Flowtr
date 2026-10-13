using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Deletes an unused category. A used category can only be deactivated.
/// </summary>
public sealed record DeleteCategory(Guid Id);

public sealed class DeleteCategoryHandler(ApplicationDbContext db) : ICommandHandler<DeleteCategory, Result>
{
    public async Task<Result> Handle(DeleteCategory command, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FindAsync([command.Id], cancellationToken);
        if (category is null)
        {
            return Result.Failure("Diese Kategorie gibt es nicht mehr.");
        }

        if (await db.Bookings.AnyAsync(b => b.CategoryId == category.Id, cancellationToken))
        {
            return Result.Failure("Diese Kategorie hat Buchungen. Deaktiviere sie stattdessen.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
