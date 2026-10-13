using Flowtr.Server.CrossCutting.Audit;
using Flowtr.Server.Features.Journal.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FlowtrRoles = Flowtr.Shared.Users.Roles;

namespace Flowtr.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    /// Fixed IDs, thus the migrations can refer to the roles.
    /// </summary>
    public static class RoleIds
    {
        public const string Admin = "3f1b5d2e-8a4c-4e6f-9b7d-1c2a3e4f5a60";

        public const string Contributor = "7c9e2a4b-1d3f-4b5a-8e6c-2f4a6b8d0c11";
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<ClosedYear> ClosedYears => Set<ClosedYear>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditEntry>(entry =>
        {
            // The trigger stops changes and deletes (see migration AddAuditLog). EF Core must know it for its SQL.
            entry.ToTable("AuditEntries", table => table.HasTrigger("AuditEntries_ReadOnly"));
            entry.Property(e => e.UserId).HasMaxLength(450);
            entry.Property(e => e.UserName).HasMaxLength(256);
            entry.Property(e => e.Action).HasConversion<string>().HasMaxLength(20);
            entry.Property(e => e.EntityType).HasMaxLength(50);
            entry.Property(e => e.EntityId).HasMaxLength(450);
            entry.HasIndex(e => e.At);
            entry.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        builder.ConfigureJournal();

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = RoleIds.Admin, Name = FlowtrRoles.Admin, NormalizedName = FlowtrRoles.Admin.ToUpperInvariant(), ConcurrencyStamp = RoleIds.Admin },
            new IdentityRole { Id = RoleIds.Contributor, Name = FlowtrRoles.Contributor, NormalizedName = FlowtrRoles.Contributor.ToUpperInvariant(), ConcurrencyStamp = RoleIds.Contributor });
    }
}
