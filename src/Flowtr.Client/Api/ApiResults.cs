using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Flowtr.Shared;

namespace Flowtr.Client.Api;

/// <summary>
/// Reads API responses as <see cref="Result"/>. The API sends errors as problem details with a German message in <c>detail</c>.
/// </summary>
public static class ApiResults
{
    public static async Task<Result<T>> Read<T>(this Task<HttpResponseMessage> request, CancellationToken cancellationToken)
    {
        var response = await request;
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<T>(await ErrorText(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return value is null ? Result.Failure<T>("Der Server hat keine Daten geliefert.") : Result.Success(value);
    }

    public static async Task<Result> Read(this Task<HttpResponseMessage> request, CancellationToken cancellationToken)
    {
        var response = await request;
        return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await ErrorText(response, cancellationToken));
    }

    /// <summary>
    /// Makes a query string, without empty values.
    /// </summary>
    public static string Query(string path, IReadOnlyDictionary<string, string?> parameters) =>
        path + "?" + string.Join("&", parameters
            .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
            .Select(parameter => $"{parameter.Key}={Uri.EscapeDataString(parameter.Value!)}"));

    private static async Task<string> ErrorText(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "Dafür hast du keine Berechtigung. Bitte melde dich neu an.";
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(cancellationToken);
            if (!string.IsNullOrEmpty(problem?.Detail))
            {
                return problem.Detail;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
        }

        return $"Der Server antwortete mit Status {(int)response.StatusCode}.";
    }

    private sealed record Problem(string? Detail);
}
