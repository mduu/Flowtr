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
            receipt.HasIndex(r => r.Number).IsUnique();
            receipt.HasIndex(r => new { r.Year, r.Kind, r.SequenceNumber }).IsUnique();
            receipt.HasMany(r => r.Files).WithOne().HasForeignKey(f => f.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReceiptFile>(file =>
        {
            file.Property(f => f.BlobName).HasMaxLength(300);
            file.Property(f => f.FileName).HasMaxLength(255);
            file.Property(f => f.ContentType).HasMaxLength(100);
            file.Property(f => f.Sha256).HasMaxLength(64);
            file.HasIndex(f => f.BlobName).IsUnique();
            file.HasIndex(f => new { f.ReceiptId, f.Position }).IsUnique();
            file.HasIndex(f => f.Sha256);
        });

        builder.Entity<ReceiptSequence>(sequence =>
        {
            sequence.HasKey(s => new { s.Year, s.Kind });
            sequence.Property(s => s.Kind).HasConversion<string>().HasMaxLength(10);
            sequence.Property(s => s.LastNumber).IsConcurrencyToken();
        });
    }
}
