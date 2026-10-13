using Flowtr.Server.Features.Journal.Commands;
using Flowtr.Server.Features.Journal.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Journal;

/// <summary>
/// Admins manage categories. A used category can only be deactivated.
/// </summary>
public class CategoryTests
{
    [Fact]
    public async Task Start_list_has_income_first_in_its_order()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var categories = (await app.Query<GetCategories, IReadOnlyList<CategoryView>>(new GetCategories())).Value!;

        // Assert
        categories.Select(c => c.Name).Take(3).ShouldBe(["Umsatz und Honorare", "Übrige Erträge", "Material und Waren"]);
        categories.Last().Name.ShouldBe("Übriger Aufwand");
        categories.ShouldAllBe(c => c.IsActive && c.Usage == 0);
    }

    [Fact]
    public async Task New_category_goes_to_the_end_and_names_are_unique_per_kind()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var created = await app.Command<CreateCategory, Result<Guid>>(new CreateCategory(new CategoryInput(" Domains ", BookingKind.Expense)));
        var duplicate = await app.Command<CreateCategory, Result<Guid>>(new CreateCategory(new CategoryInput("domains", BookingKind.Expense)));
        var otherKind = await app.Command<CreateCategory, Result<Guid>>(new CreateCategory(new CategoryInput("Domains", BookingKind.Income)));

        // Assert
        created.IsSuccess.ShouldBeTrue();
        duplicate.Error.ShouldBe("Die Kategorie «domains» gibt es schon.");
        otherKind.IsSuccess.ShouldBeTrue();
        var expenses = (await app.Query<GetCategories, IReadOnlyList<CategoryView>>(new GetCategories())).Value!.Where(c => c.Kind == BookingKind.Expense);
        expenses.Last().Name.ShouldBe("Domains");
    }

    [Fact]
    public async Task Used_category_cannot_be_deleted()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(category: Hardware));

        // Act
        var result = await app.Command<DeleteCategory, Result>(new DeleteCategory(Hardware));

        // Assert
        result.Error.ShouldBe("Diese Kategorie hat Buchungen. Deaktiviere sie stattdessen.");
    }

    [Fact]
    public async Task Unused_category_can_be_deleted()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var result = await app.Command<DeleteCategory, Result>(new DeleteCategory(Hardware));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await app.Query<GetCategories, IReadOnlyList<CategoryView>>(new GetCategories())).Value!.ShouldNotContain(c => c.Id == Hardware);
    }
}
