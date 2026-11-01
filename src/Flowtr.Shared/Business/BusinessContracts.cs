namespace Flowtr.Shared.Business;

/// <summary>
/// The business that keeps its books with Flowtr, for example «Marc Dürst, Softwareentwicklung». It heads the export.
/// </summary>
public sealed record BusinessView(string? Name);

public sealed record BusinessInput(string? Name)
{
    public const int MaxLength = 200;
}
