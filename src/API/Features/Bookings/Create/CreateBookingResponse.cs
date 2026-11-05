namespace Bookly.Api.Features.Bookings.Create;

public sealed record CreateBookingResponse(
    Guid Id,
    Guid RoomId,
    string OccupantName,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc
);
