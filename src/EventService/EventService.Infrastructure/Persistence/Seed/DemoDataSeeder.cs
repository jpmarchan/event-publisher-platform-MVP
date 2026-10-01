using EventService.Domain;
using Microsoft.EntityFrameworkCore;
using Event = EventService.Domain.Event;

namespace EventService.Infrastructure.Persistence.Seed;

public static class DemoDataSeeder
{
    public const string ConfigKey = "Seed:DemoData";

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var events = context.Set<Event>();
        if (await events.AnyAsync(cancellationToken))
            return;

        events.AddRange(CreateDemoEvents());
        await context.SaveChangesAsync(cancellationToken);
    }

    public static void Seed(DbContext context)
    {
        var events = context.Set<Event>();
        if (events.Any())
            return;

        events.AddRange(CreateDemoEvents());
        context.SaveChanges();
    }

    private static IEnumerable<Event> CreateDemoEvents()
    {
        // Fechas relativas: el dominio exige fecha futura.
        var today = DateTimeOffset.UtcNow.Date;

        yield return Event.CreatePublished(
            "Festival de Rock Lima",
            new DateTimeOffset(today.AddDays(60).AddHours(20), TimeSpan.Zero),
            "Estadio Nacional",
            [
                Zone.Create("General", 150m, 20000),
                Zone.Create("Preferencial", 320m, 5000),
                Zone.Create("VIP", 650m, 800)
            ]);

        yield return Event.CreatePublished(
            "Conferencia .NET Latam",
            new DateTimeOffset(today.AddDays(30).AddHours(9), TimeSpan.Zero),
            "Centro de Convenciones",
            [
                Zone.Create("Asistente", 80m, 1200),
                Zone.Create("Workshop", 200m, 150)
            ]);

        yield return Event.CreatePublished(
            "Stand-up Comedy Night",
            new DateTimeOffset(today.AddDays(14).AddHours(21), TimeSpan.Zero),
            "Teatro Municipal",
            [
                Zone.Create("Platea", 90m, 400),
                Zone.Create("Mezanine", 60m, 250)
            ]);
    }
}
