using Microsoft.EntityFrameworkCore;

namespace Store.Infrastructure.Persistence.Common;

/// <summary>
/// Mapea Outbox e Inbox. Cada servicio los tiene en su propio schema:
/// no comparten tablas de mensajería.
/// </summary>
internal static class MessagingModelBuilderExtensions
{
    public static ModelBuilder AddOutboxAndInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();

            b.HasIndex(x => x.OccurredAt)
                .HasDatabaseName("ix_outbox_messages_pending")
                .HasFilter("published_at IS NULL");
        });

        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("processed_events");
            b.HasKey(x => x.EventId);
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        });

        return modelBuilder;
    }
}
