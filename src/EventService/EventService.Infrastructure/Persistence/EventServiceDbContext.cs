using EventService.Application.Idempotency;
using EventService.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Event = EventService.Domain.Event;

namespace EventService.Infrastructure.Persistence;

public sealed class EventServiceDbContext(DbContextOptions<EventServiceDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventServiceDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
