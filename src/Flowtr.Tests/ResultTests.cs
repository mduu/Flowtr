using Flowtr.Shared;

namespace Flowtr.Tests;

public class ResultTests
{
    [Fact]
    public void Failure_with_value_type_has_no_value_and_keeps_error()
    {
        // Act
        var result = Result.Failure<int>("Beleg fehlt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Beleg fehlt");
        result.Value.ShouldBe(0);
    }
}
