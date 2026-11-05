namespace Bookly.Api.Features.Bookings.Create;

public sealed record CreateBookingRequest(
    Guid RoomId,
    string OccupantName,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc
);
