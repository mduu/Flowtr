using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Renames a category or (de)activates it. An inactive category is not offered for new bookings.
/// </summary>
public sealed record UpdateCategory(Guid Id, CategoryUpdate Update);

public sealed class UpdateCategoryHandler(ApplicationDbContext db) : ICommandHandler<UpdateCategory, Result>
{
    public async Task<Result> Handle(UpdateCategory command, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FindAsync([command.Id], cancellationToken);
        if (category is null)
        {
            return Result.Failure("Diese Kategorie gibt es nicht mehr.");
        }

        var name = command.Update.Name.Trim();
        if (await CategoryNames.Check(db, name, category.Kind, category.Id, cancellationToken) is { } error)
        {
            return Result.Failure(error);
        }

        category.Name = name;
        category.IsActive = command.Update.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
