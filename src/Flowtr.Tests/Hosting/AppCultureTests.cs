using Flowtr.Server.CrossCutting.Hosting;
using Microsoft.Extensions.Configuration;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// The culture of Flowtr is de-CH by default and can be configured. A wrong value stops the start.
/// </summary>
public class AppCultureTests
{
    [Theory]
    [InlineData(null, "de-CH")]
    [InlineData("", "de-CH")]
    [InlineData("fr-CH", "fr-CH")]
    public void Culture_is_de_CH_or_configured(string? configured, string expected)
    {
        new AppCulture(Configuration(configured)).Culture.Name.ShouldBe(expected);
    }

    [Fact]
    public void Unknown_culture_stops_the_start()
    {
        Should.Throw<InvalidOperationException>(() => new AppCulture(Configuration("xx-Unknown")))
            .Message.ShouldContain("xx-Unknown");
    }

    private static IConfiguration Configuration(string? culture) =>
        new ConfigurationBuilder().AddInMemoryCollection([new(AppCulture.Section, culture)]).Build();
}
