namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// Supplier or customer, for example Microsoft or Stripe. Flowtr adds new names when a user enters them.
/// </summary>
public sealed class Counterparty
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = "";
}
