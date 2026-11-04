using System.Security.Claims;
using Flowtr.Client.Features.Statistics;
using Flowtr.Server.Features.Statistics.Queries;
using Flowtr.Shared.Statistics;
using Flowtr.Shared.Users;
using Microsoft.AspNetCore.Http;
using Flowtr.Tests.Journal;
using static Flowtr.Tests.Journal.JournalTestData;

namespace Flowtr.Tests.Statistics;

/// <summary>
/// Story #018: income, expense and balance per month and cumulative, the current month last.
/// </summary>
public class StatisticsTests
{
    [Fact]
    public async Task Months_end_with_the_current_month_and_include_months_without_bookings()
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var view = await app.Statistics(6);

        // Assert
        view.Totals.Select(total => total.Month).ShouldBe(
            [new(2026, 5, 1), new(2026, 6, 1), new(2026, 7, 1), new(2026, 8, 1), new(2026, 9, 1), new(2026, 10, 1)]);
        view.Totals.ShouldAllBe(total => total.Income == 0 && total.Expense == 0 && total.CumulativeBalance == 0);
    }

    [Fact]
    public async Task Totals_per_month_and_cumulative()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Income(date: "2026-09-30", amount: 99.00m));
        await app.Create(Expense(date: "2026-09-01", amount: 7.84m));
        await app.Create(Expense(date: "2026-09-15", amount: 12.16m));
        await app.Create(Expense(date: "2026-10-01", amount: 120.00m));
        await app.Create(Income(date: "2026-10-12", amount: 9.00m));

        // Act
        var view = await app.Statistics(6);

        // Assert
        view.Totals[^2].ShouldBe(new MonthTotals(new DateOnly(2026, 9, 1), 99.00m, 20.00m, 79.00m, 99.00m, 20.00m, 79.00m));
        view.Totals[^1].ShouldBe(new MonthTotals(new DateOnly(2026, 10, 1), 9.00m, 120.00m, -111.00m, 108.00m, 140.00m, -32.00m));
    }

    [Fact]
    public async Task Bookings_outside_the_period_do_not_count()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Income(date: "2026-04-30", amount: 50.00m));
        await app.Create(Income(date: "2026-05-01", amount: 10.00m));
        await app.Create(Income(date: "2026-11-01", amount: 70.00m));

        // Act
        var view = await app.Statistics(6);

        // Assert
        view.Totals[0].Income.ShouldBe(10.00m);
        view.Totals[^1].CumulativeIncome.ShouldBe(10.00m);
    }

    [Fact]
    public async Task Period_goes_back_into_the_last_years()
    {
        // Arrange
        using var app = new TestApp();
        await app.Create(Expense(date: "2024-11-05", amount: 30.00m));

        // Act
        var view = await app.Statistics(24);

        // Assert
        view.Totals.Count.ShouldBe(24);
        view.Totals[0].ShouldBe(new MonthTotals(new DateOnly(2024, 11, 1), 0, 30.00m, -30.00m, 0, 30.00m, -30.00m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(1000)]
    public async Task Other_periods_are_refused(int months)
    {
        // Arrange
        using var app = new TestApp();

        // Act
        var result = await app.Query<GetStatistics, StatisticsView>(new GetStatistics(months));

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(Roles.Contributor, true)]
    [InlineData(Roles.Admin, true)]
    public async Task Only_users_with_a_role_get_the_statistics(string? role, bool allowed)
    {
        // Arrange
        using var app = new TestApp();
        app.Get<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(role is null ? [] : [new Claim(ClaimTypes.Role, role)], "Test")),
        };

        // Act
        var result = await app.Get<IStatisticsApi>().Get(StatisticsView.DefaultMonths);

        // Assert
        result.IsSuccess.ShouldBe(allowed);
    }
}

internal static class StatisticsTestExtensions
{
    public static async Task<StatisticsView> Statistics(this TestApp app, int months)
    {
        var result = await app.Query<GetStatistics, StatisticsView>(new GetStatistics(months));
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Value!;
    }
}
