namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// How the user paid or got paid, for example credit card or bank account.
/// </summary>
public sealed class PaymentMethod
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = "";
}
