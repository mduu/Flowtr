using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Commands;

/// <summary>
/// Adds a category at the end of its list.
/// </summary>
public sealed record CreateCategory(CategoryInput Input);

public sealed class CreateCategoryHandler(ApplicationDbContext db) : ICommandHandler<CreateCategory, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategory command, CancellationToken cancellationToken)
    {
        var name = command.Input.Name.Trim();
        if (await CategoryNames.Check(db, name, command.Input.Kind, exceptId: null, cancellationToken) is { } error)
        {
            return Result.Failure<Guid>(error);
        }

        var last = await db.Categories.Where(c => c.Kind == command.Input.Kind).MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0;
        var category = new Category { Name = name, Kind = command.Input.Kind, SortOrder = last + 10 };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(category.Id);
    }
}

internal static class CategoryNames
{
    public static async Task<string?> Check(ApplicationDbContext db, string name, BookingKind kind, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (name.Length is 0 or > 100)
        {
            return "Bitte einen Namen angeben (höchstens 100 Zeichen).";
        }

        var taken = await db.Categories.AnyAsync(
            c => c.Kind == kind && c.Name.ToLower() == name.ToLower() && c.Id != exceptId,
            cancellationToken);

        return taken ? $"Die Kategorie «{name}» gibt es schon." : null;
    }
}
