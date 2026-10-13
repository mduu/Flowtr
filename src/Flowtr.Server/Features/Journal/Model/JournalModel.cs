using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal.Model;

public static class JournalModel
{
    /// <summary>
    /// Tables, keys and seed data of the journal.
    /// </summary>
    public static void ConfigureJournal(this ModelBuilder builder)
    {
        builder.Entity<Booking>(booking =>
        {
            booking.Property(b => b.Kind).HasConversion<string>().HasMaxLength(10);
            booking.Property(b => b.AmountChf).HasPrecision(18, 2);
            booking.Property(b => b.OriginalAmount).HasPrecision(18, 2);
            booking.Property(b => b.OriginalCurrency).HasMaxLength(3);
            booking.Property(b => b.Description).HasMaxLength(500);
            booking.Property(b => b.ReceiptNumber).HasMaxLength(50);
            booking.Property(b => b.Note).HasMaxLength(2000);
            booking.Property(b => b.VatRate).HasPrecision(5, 2);
            booking.Property(b => b.VatAmount).HasPrecision(18, 2);
            booking.HasIndex(b => b.Date);
            booking.HasOne(b => b.Category).WithMany().OnDelete(DeleteBehavior.Restrict);
            booking.HasOne(b => b.Counterparty).WithMany().OnDelete(DeleteBehavior.Restrict);
            booking.HasOne(b => b.PaymentMethod).WithMany().OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(100);
            category.Property(c => c.Kind).HasConversion<string>().HasMaxLength(10);
            category.HasIndex(c => new { c.Kind, c.Name }).IsUnique();
            category.HasData(JournalSeed.Categories);
        });

        builder.Entity<Counterparty>(counterparty =>
        {
            counterparty.Property(c => c.Name).HasMaxLength(200);
            counterparty.HasIndex(c => c.Name).IsUnique();
        });

        builder.Entity<PaymentMethod>(method =>
        {
            method.Property(m => m.Name).HasMaxLength(100);
            method.HasIndex(m => m.Name).IsUnique();
            method.HasData(JournalSeed.PaymentMethods);
        });

        builder.Entity<ClosedYear>(year =>
        {
            year.HasKey(y => y.Year);
            year.Property(y => y.Year).ValueGeneratedNever();
        });
    }
}
