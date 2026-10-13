using System.Globalization;

namespace Flowtr.Shared;

/// <summary>
/// Swiss formats: <c>1'234.50</c> and <c>dd.MM.yyyy</c>. Fixed separators, thus the same on server and in the browser.
/// </summary>
public static class SwissFormat
{
    private static readonly NumberFormatInfo numbers = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = "'",
        NegativeSign = "-",
    };

    /// <summary>
    /// For example <c>1'234.50</c>.
    /// </summary>
    public static string Amount(decimal value) => value.ToString("#,##0.00", numbers);

    /// <summary>
    /// For example <c>CHF 1'234.50</c>.
    /// </summary>
    public static string Chf(decimal value) => $"CHF {Amount(value)}";

    public static string Date(DateOnly value) => value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}
