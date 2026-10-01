using EventService.Application.Idempotency;
using FluentValidation;

namespace EventService.Application.Events.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Zones).NotEmpty().WithMessage("Debe incluir al menos una zona.");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(IdempotencyRecord.MaxKeyLength)
            .When(x => x.IdempotencyKey is not null)
            .OverridePropertyName("Idempotency-Key");

        RuleForEach(x => x.Zones).ChildRules(zone =>
        {
            zone.RuleFor(z => z.Name).NotEmpty().MaximumLength(100);
            zone.RuleFor(z => z.Price).GreaterThanOrEqualTo(0);
            zone.RuleFor(z => z.Capacity).GreaterThan(0);
        });
    }
}
