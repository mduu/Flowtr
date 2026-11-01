using Flowtr.Server.Features.Business.Commands;
using Flowtr.Server.Features.Business.Queries;
using Flowtr.Shared;
using Flowtr.Shared.Business;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Tests.Business;

/// <summary>
/// Story #013: the name of the business heads the export. Admins set it under «Verwaltung → Geschäft».
/// </summary>
public class BusinessTests
{
    [Fact]
    public async Task Name_is_set_changed_and_removed()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var empty = await Name(app);
        await Set(app, "  Marc Dürst, Softwareentwicklung ");
        var set = await Name(app);
        await Set(app, "Marc Dürst");
        var changed = await Name(app);
        await Set(app, " ");
        var removed = await Name(app);

        // Assert
        (empty, set, changed, removed).ShouldBe((null, "Marc Dürst, Softwareentwicklung", "Marc Dürst", null));
        (await app.Db.BusinessSettings.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Name_has_at_most_200_characters()
    {
        using var app = new TestApp();

        var result = await app.Command<SetBusiness, Result>(new SetBusiness(new BusinessInput(new string('x', 201))));

        result.Error.ShouldBe("Der Name hat höchstens 200 Zeichen.");
    }

    [Fact]
    public async Task Change_is_in_the_audit_log()
    {
        using var app = new TestApp();

        await Set(app, "Marc Dürst");

        (await app.Db.AuditEntries.Select(e => e.EntityType).ToListAsync()).ShouldContain("Business");
    }

    private static async Task Set(TestApp app, string name)
    {
        var result = await app.Command<SetBusiness, Result>(new SetBusiness(new BusinessInput(name)));
        result.IsSuccess.ShouldBeTrue(result.Error);
    }

    private static async Task<string?> Name(TestApp app) => (await app.Query<GetBusiness, BusinessView>(new GetBusiness())).Value!.Name;
}
