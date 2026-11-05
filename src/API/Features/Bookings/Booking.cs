using Bookly.Api.Features.Rooms;
using System.ComponentModel.DataAnnotations;

namespace Bookly.Api.Features.Bookings;

public sealed class Booking
{
    private Booking() { }

    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public Room Room { get; private set; } = null!;

    [MaxLength(100)]
    public string OccupantName { get; private set; } = string.Empty;

    public DateTime StartsAtUtc { get; private set; }

    public DateTime EndsAtUtc { get; private set; }

    public static Booking Create(
        Room room,
        string occupantName,
        DateTime startsAtUtc,
        DateTime endsAtUtc
    )
    {
        return new()
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Room = room,
            OccupantName = occupantName,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc
        };
    }
}
