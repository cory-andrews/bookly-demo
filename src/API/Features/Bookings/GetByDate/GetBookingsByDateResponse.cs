namespace Bookly.Api.Features.Bookings.GetByDate;

public sealed record GetBookingsByDateResponse(GetBookingsByDateResponse.BookingDto[] Bookings)
{
    public sealed record BookingDto(
        Guid Id,
        Guid RoomId,
        string OccupantName,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc);
}
