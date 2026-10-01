using EventService.Api.Auth;
using EventService.Api.Observability;
using EventService.Application.Events.CreateEvent;
using EventService.Application.Events.GetEventById;
using EventService.Application.Events.GetEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Api.Events;

public static class EventsEndpoints
{
    public const string CreateEventRateLimiterPolicy = "create-event";
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotentReplayedHeader = "Idempotent-Replayed";

    public static void MapEventsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapPost("/", async (
                [FromBody] CreateEventRequest request,
                [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
                HttpContext httpContext,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

                var command = new CreateEventCommand(
                    request.Name,
                    request.Date,
                    request.Venue,
                    request.Zones.Select(z => new CreateZoneInput(z.Name, z.Price, z.Capacity)).ToList(),
                    correlationId,
                    idempotencyKey);

                var result = await sender.Send(command, cancellationToken);

                if (result.Replayed)
                    httpContext.Response.Headers[IdempotentReplayedHeader] = "true";

                return Results.Created($"/events/{result.Event.Id}", result.Event);
            })
            .RequireAuthorization(AuthorizationPolicies.EventsWrite)
            .RequireRateLimiting(CreateEventRateLimiterPolicy)
            .WithName("CreateEvent");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetEventsQuery(), cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization(AuthorizationPolicies.EventsRead)
            .WithName("GetEvents");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetEventByIdQuery(id), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization(AuthorizationPolicies.EventsRead)
            .WithName("GetEventById");
    }
}
