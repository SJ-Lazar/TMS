using Microsoft.EntityFrameworkCore;
using TMS.Domain.Entities;
using TMS.Domain.Enums;
using TMS.Infrastructure.Persistence.Encryption;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections;
using System.Collections.Generic;

namespace TMS.Infrastructure.Persistence;

public sealed class TmsDbContext : DbContext
{
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<SupportMember> SupportMembers => Set<SupportMember>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public TmsDbContext(DbContextOptions<TmsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportMember>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
            builder.Property(m => m.IsActive).IsRequired();
        });

        modelBuilder.Entity<SupportTicket>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(2000).IsRequired();
            builder.Property(t => t.RequesterName).HasMaxLength(200).IsRequired();
            builder.Property(t => t.CreatedAtUtc).IsRequired();
            builder.Property(t => t.Status).HasConversion<int>().IsRequired();
            builder.Property(t => t.AssignedSupportMemberId);
            builder.Property(t => t.AttachmentFileName).HasMaxLength(255);
            builder.Property(t => t.AttachmentContentType).HasMaxLength(100);
            builder.Property(t => t.IdempotencyKey).HasMaxLength(100);
            builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("IdempotencyKey IS NOT NULL");
            builder.Property(t => t.RowVersion).IsRowVersion();

            var attachmentConverter = new ValueConverter<byte[]?, byte[]?>(
                v => v == null ? null : DatabaseEncryption.Encrypt(v),
                v => v == null ? null : DatabaseEncryption.Decrypt(v));

            var attachmentComparer = new ValueComparer<byte[]?>(
                (l, r) => StructuralComparisons.StructuralEqualityComparer.Equals(l, r),
                v => v == null ? 0 : StructuralComparisons.StructuralEqualityComparer.GetHashCode(v),
                v => v == null ? null : v.ToArray());

            builder.Property(t => t.AttachmentBytes)
                .HasConversion(attachmentConverter)
                .Metadata.SetValueComparer(attachmentComparer);
            builder.HasOne<SupportMember>()
                .WithMany()
                .HasForeignKey(t => t.AssignedSupportMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasMany(t => t.Tags)
                .WithMany(t => t.Tickets)
                .UsingEntity<Dictionary<string, object>>("TicketTags",
                    j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<SupportTicket>().WithMany().HasForeignKey("TicketId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("TicketId", "TagId"));
            builder.Navigation(t => t.Tags).AutoInclude(false);
        });

        modelBuilder.Entity<Tag>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
            builder.HasIndex(t => t.Name).IsUnique();
            builder.Navigation(t => t.Tickets).AutoInclude(false);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
            builder.Property(a => a.Details).IsRequired();
            builder.Property(a => a.CreatedAtUtc).IsRequired();
        });
    }
}
