using AwesomeAssertions;
using Bookly.Api.Data;
using Bookly.Api.Features.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookly.IntegrationTests.Bookings;

[Collection(nameof(ApiCollection))]
public sealed class CancelBookingTests(BooklyApiFactory factory) : IAsyncLifetime
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
    public async Task CancelBooking_WithValidBookingId_DeletesBookingAndReturnsOkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        Guid bookingId;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var room = Room.Create("Testing Room A");
            dbContext.Rooms.Add(room);

            var startDate = new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc);
            var endDate = startDate.AddHours(1);
            var booking = room.Book("Cory Andrews", startDate, endDate);
            dbContext.Bookings.Add(booking);

            await dbContext.SaveChangesAsync(cancellationToken);

            bookingId = booking.Id;
        }

        // Act
        var res = await _client.DeleteAsync($"/api/bookings/{bookingId}", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
            var booking = await dbContext.Bookings.FindAsync([bookingId], cancellationToken);
            booking.Should().BeNull();
        }
    }

    [Fact]
    public async Task CancelBooking_WithUnknownBookingId_ReturnsNotFoundResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var bookingId = Guid.NewGuid();

        // Act
        var res = await _client.DeleteAsync($"/api/bookings/{bookingId}", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
