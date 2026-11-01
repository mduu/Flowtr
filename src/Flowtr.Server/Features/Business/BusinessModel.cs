using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Business;

/// <summary>
/// The settings of the business. One row: an installation is one business (single tenant).
/// </summary>
public sealed class BusinessSettings
{
    /// <summary>
    /// The key of the one row. Fixed: the audit log needs the key before the save.
    /// </summary>
    public static readonly Guid TheId = Guid.Parse("0199d6a0-0003-7000-8000-000000000001");

    public Guid Id { get; set; } = TheId;

    public string? Name { get; set; }
}

public static class BusinessModel
{
    public static void ConfigureBusiness(this ModelBuilder builder)
    {
        builder.Entity<BusinessSettings>(business => business.Property(b => b.Name).HasMaxLength(Shared.Business.BusinessInput.MaxLength));
    }
}
