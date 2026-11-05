using FluentValidation;

namespace Bookly.Api.Features.Bookings.Create;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.OccupantName).Length(1, 100);
        RuleFor(x => x.StartsAtUtc).LessThan(x => x.EndsAtUtc).WithMessage("The start time must be before the end time.");
        RuleFor(x => x.EndsAtUtc).LessThan(x => x.StartsAtUtc.Date.AddDays(1)).WithMessage("The end time must be on the same day as the start time.");
    }
}
