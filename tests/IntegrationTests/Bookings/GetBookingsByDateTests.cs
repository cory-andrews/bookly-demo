using AwesomeAssertions;
using Bookly.Api.Data;
using Bookly.Api.Features.Bookings.GetByDate;
using Bookly.Api.Features.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Bookly.IntegrationTests.Bookings;

[Collection(nameof(ApiCollection))]
public sealed class GetBookingsByDateTests(BooklyApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        // Clear db state before each test
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await dbContext.Bookings.ExecuteDeleteAsync();
        await dbContext.Rooms.ExecuteDeleteAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetBookingsByDate_WithSeededBookingsAroundGivenDate_ReturnsOkResponseWithExpectedBookings()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var roomA = Room.Create("Room A");
        var roomB = Room.Create("Room B");

        // Booking 1: Room A 2026-11-04 09:00 - 10:00 (previous day)
        var booking1 = roomA.Book("Cory Andrews", new DateTime(2026, 11, 04, 09, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 04, 10, 00, 00, DateTimeKind.Utc));
        // Booking 2: Room A 2026-11-05 09:00 - 10:00 (current day)
        var booking2 = roomA.Book("Cory Andrews", new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc));
        // Booking 3: Room B 2026-11-05 10:00 - 10:30 (current day)
        var booking3 = roomB.Book("John Doe", new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 10, 30, 00, DateTimeKind.Utc));
        // Booking 4: Room B 2026-11-06 10:00 - 10:30 (next day)
        var booking4 = roomB.Book("John Doe", new DateTime(2026, 11, 06, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 06, 10, 30, 00, DateTimeKind.Utc));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.AddRange(roomA, roomB);
              
            dbContext.Bookings.AddRange(booking1, booking2, booking3, booking4);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Act
        var res = await _client.GetAsync($"/api/bookings?date=2026-11-05", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await res.Content.ReadFromJsonAsync<GetBookingsByDateResponse>(cancellationToken);
        content.Should().NotBeNull();
        content.Bookings.Should().Satisfy(
            b => b.Id == booking2.Id && b.RoomId == roomA.Id && b.OccupantName == "Cory Andrews" && b.StartsAtUtc == booking2.StartsAtUtc && b.EndsAtUtc == booking2.EndsAtUtc,
            b => b.Id == booking3.Id && b.RoomId == roomB.Id && b.OccupantName == "John Doe" && b.StartsAtUtc == booking3.StartsAtUtc && b.EndsAtUtc == booking3.EndsAtUtc
        );
    }

    [Fact]
    public async Task GetBookingsByDate_WithNoBookingsForDate_ReturnsOkResponseWithEmptyBookingsList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var res = await _client.GetAsync($"/api/bookings?date=2026-11-05", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await res.Content.ReadFromJsonAsync<GetBookingsByDateResponse>(cancellationToken);
        content.Should().NotBeNull();
        content.Bookings.Should().NotBeNull().And.BeEmpty();
    }
}

