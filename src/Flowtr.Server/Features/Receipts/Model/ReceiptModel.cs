using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Receipts.Model;

public static class ReceiptModel
{
    public static void ConfigureReceipts(this ModelBuilder builder)
    {
        builder.Entity<Receipt>(receipt =>
        {
            receipt.Property(r => r.Number).HasMaxLength(20);
            receipt.Property(r => r.Kind).HasConversion<string>().HasMaxLength(10);
            receipt.Property(r => r.BlobName).HasMaxLength(300);
            receipt.Property(r => r.FileName).HasMaxLength(255);
            receipt.Property(r => r.ContentType).HasMaxLength(100);
            receipt.Property(r => r.Sha256).HasMaxLength(64);
            receipt.HasIndex(r => r.Number).IsUnique();
            receipt.HasIndex(r => new { r.Year, r.Kind, r.SequenceNumber }).IsUnique();
        });

        builder.Entity<ReceiptSequence>(sequence =>
        {
            sequence.HasKey(s => new { s.Year, s.Kind });
            sequence.Property(s => s.Kind).HasConversion<string>().HasMaxLength(10);
            sequence.Property(s => s.LastNumber).IsConcurrencyToken();
        });
    }
}
