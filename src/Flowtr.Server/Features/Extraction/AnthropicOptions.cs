namespace Flowtr.Server.Features.Extraction;

/// <summary>
/// Configuration section <c>Anthropic</c>. The API key only in user secrets or Key Vault, never in the repository.
/// </summary>
public sealed class AnthropicOptions
{
    public const string Section = "Anthropic";

    public string? ApiKey { get; set; }

    /// <summary>
    /// Only for an API key without a workspace: the workspace to use (header <c>anthropic-workspace-id</c>).
    /// </summary>
    public string? WorkspaceId { get; set; }

    public string Model { get; set; } = "claude-sonnet-5-5";

    public Uri BaseAddress { get; set; } = new("https://api.anthropic.com/");

    public int MaxTokens { get; set; } = 1024;
}
