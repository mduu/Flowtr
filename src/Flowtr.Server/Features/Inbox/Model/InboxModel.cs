using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Inbox.Model;

public static class InboxModel
{
    public static void ConfigureInbox(this ModelBuilder builder)
    {
        builder.Entity<ImportedMail>(mail =>
        {
            mail.Property(m => m.MessageKey).HasMaxLength(64);
            mail.Property(m => m.MessageId).HasMaxLength(300);
            mail.Property(m => m.Sender).HasMaxLength(256);
            mail.Property(m => m.OriginalSender).HasMaxLength(300);
            mail.Property(m => m.Subject).HasMaxLength(500);
            mail.Property(m => m.Outcome).HasConversion<string>().HasMaxLength(20);
            mail.Property(m => m.Reason).HasMaxLength(500);
            mail.HasIndex(m => m.MessageKey).IsUnique();
        });

        builder.Entity<InboxItem>(item =>
        {
            item.HasOne(i => i.Mail).WithMany().HasForeignKey(i => i.MailId).OnDelete(DeleteBehavior.Restrict);
            item.Property(i => i.FileName).HasMaxLength(255);
            item.Property(i => i.ContentType).HasMaxLength(100);
            item.Property(i => i.Sha256).HasMaxLength(64);
            item.Property(i => i.Analysis).HasConversion<string>().HasMaxLength(20);
            item.Property(i => i.AnalysisError).HasMaxLength(500);
            item.HasIndex(i => i.UploadId).IsUnique();
            item.HasIndex(i => i.Sha256);
        });

        builder.Entity<AllowedSender>(sender =>
        {
            sender.Property(s => s.Address).HasMaxLength(256);
            sender.HasIndex(s => s.Address).IsUnique();
        });
    }
}
