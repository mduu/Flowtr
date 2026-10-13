using Flowtr.Shared;

namespace Flowtr.Server.CrossCutting.Api;

/// <summary>
/// Maps handler results to HTTP: success as 200 (with value) or 204, failure as 400 with the German message in <c>detail</c>.
/// </summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : Problem(result);

    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : Problem(result);

    private static IResult Problem(Result result) =>
        TypedResults.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
}
