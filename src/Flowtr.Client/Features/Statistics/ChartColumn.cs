namespace Flowtr.Client.Features.Statistics;

/// <summary>
/// One month in a <see cref="MonthChart"/>: bars from <see cref="ChartBar.From"/> to <see cref="ChartBar.To"/> and an optional marker, in CHF.
/// </summary>
public sealed record ChartColumn(string Label, IReadOnlyList<ChartBar> Bars, decimal? Marker = null);

/// <param name="Class">CSS class of the bar, for example <c>income</c>.</param>
public sealed record ChartBar(decimal From, decimal To, string Class);
