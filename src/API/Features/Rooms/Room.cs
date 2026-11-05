using Bookly.Api.Features.Bookings;
using System.ComponentModel.DataAnnotations;

namespace Bookly.Api.Features.Rooms;

public sealed class Room
{
    private Room() { }

    public Guid Id { get; private set; }

    [MaxLength(100)]
    public string DisplayName { get; private set; } = string.Empty;

    [ConcurrencyCheck]
    public Guid ConcurrencyToken { get; private set; }

    public static Room Create(string dDisplayName)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            DisplayName = dDisplayName
        };
    }

    public Booking Book(string occupantName, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        ConcurrencyToken = Guid.NewGuid();

        return Booking.Create(this, occupantName, startsAtUtc, endsAtUtc);
    }
}
