using EventService.Application.Abstractions;
using EventService.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence;

public sealed class EventRepository(EventServiceDbContext dbContext) : IEventRepository
{
    public void Add(Event @event) => dbContext.Events.Add(@event);

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Events
            .Include(e => e.Zones)
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Events
            .Include(e => e.Zones)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
