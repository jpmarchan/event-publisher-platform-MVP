using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventService.Application.Events.CreateEvent;

public static class CreateEventRequestHasher
{
    public static string Hash(CreateEventCommand command)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            command.Name,
            Date = command.Date.ToUniversalTime().ToString("O"),
            command.Venue,
            Zones = command.Zones.Select(z => new { z.Name, z.Price, z.Capacity })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
