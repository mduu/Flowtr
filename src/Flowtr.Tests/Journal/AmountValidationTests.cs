using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Flowtr.Tests.Journal;

/// <summary>
/// The amount check of the booking dialog must work in each culture of the browser, also with a decimal comma.
/// </summary>
public class AmountValidationTests
{
    [Theory]
    [InlineData("de-CH")]
    [InlineData("de-DE")]
    [InlineData("fr-CH")]
    public void Amount_range_does_not_depend_on_the_culture(string culture)
    {
        // Arrange: the same attribute as in BookingDialog.
        var range = new RangeAttribute(0.01, 999_999_999.0);
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

        try
        {
            // Act and Assert
            range.IsValid(20.50m).ShouldBeTrue();
            range.IsValid(0m).ShouldBeFalse();
            range.IsValid(0.01m).ShouldBeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
