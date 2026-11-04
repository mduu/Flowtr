namespace Flowtr.Shared.Statistics;

/// <summary>
/// Income, expense and balance per month for the last <see cref="Months"/> months, the current month last.
/// </summary>
public sealed record StatisticsView(int Months, IReadOnlyList<MonthTotals> Totals)
{
    public const int DefaultMonths = 12;

    public static readonly IReadOnlyList<int> MonthChoices = [6, 12, 24, 36];
}

/// <summary>
/// The totals of one month and the cumulative totals from the start of the period until the end of this month.
/// </summary>
public sealed record MonthTotals(
    DateOnly Month,
    decimal Income,
    decimal Expense,
    decimal Balance,
    decimal CumulativeIncome,
    decimal CumulativeExpense,
    decimal CumulativeBalance);
