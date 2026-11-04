using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Flowtr.Shared.Statistics;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Statistics.Queries;

/// <summary>
/// The totals per month for the last <paramref name="Months"/> months, the current month included.
/// </summary>
public sealed record GetStatistics(int Months);

public sealed class GetStatisticsHandler(ApplicationDbContext db, TimeProvider time) : IQueryHandler<GetStatistics, StatisticsView>
{
    public async Task<Result<StatisticsView>> Handle(GetStatistics query, CancellationToken cancellationToken)
    {
        if (!StatisticsView.MonthChoices.Contains(query.Months))
        {
            return Result.Failure<StatisticsView>($"Zeitraum nicht möglich: {string.Join(", ", StatisticsView.MonthChoices)} Monate.");
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var end = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var start = end.AddMonths(-query.Months);

        var sums = await db.Bookings.AsNoTracking()
            .Where(b => b.Date >= start && b.Date < end)
            .GroupBy(b => new { b.Date.Year, b.Date.Month, b.Kind })
            .Select(group => new { group.Key.Year, group.Key.Month, group.Key.Kind, Amount = group.Sum(b => b.AmountChf) })
            .ToListAsync(cancellationToken);

        decimal Sum(DateOnly month, BookingKind kind) =>
            sums.Where(sum => sum.Year == month.Year && sum.Month == month.Month && sum.Kind == kind).Sum(sum => sum.Amount);

        var totals = new List<MonthTotals>(query.Months);
        decimal cumulativeIncome = 0, cumulativeExpense = 0;
        for (var month = start; month < end; month = month.AddMonths(1))
        {
            var income = Sum(month, BookingKind.Income);
            var expense = Sum(month, BookingKind.Expense);
            cumulativeIncome += income;
            cumulativeExpense += expense;
            totals.Add(new MonthTotals(month, income, expense, income - expense, cumulativeIncome, cumulativeExpense, cumulativeIncome - cumulativeExpense));
        }

        return Result.Success(new StatisticsView(query.Months, totals));
    }
}
