using AwesomeAssertions;
using Bookly.Api.Data;
using Bookly.Api.Features.Bookings.Create;
using Bookly.Api.Features.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace Bookly.IntegrationTests.Bookings;

[Collection(nameof(ApiCollection))]
public sealed class CreateBookingTests(BooklyApiFactory factory) : IAsyncLifetime
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
    public async Task CreateBooking_WithValidRequest_CreatesBookingAndReturnsOkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc),
            new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc)
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var bookingRes = await res.Content.ReadFromJsonAsync<CreateBookingResponse>(cancellationToken);
        bookingRes.Should().NotBeNull();
        bookingRes.Id.Should().NotBeEmpty();
        bookingRes.RoomId.Should().Be(request.RoomId);
        bookingRes.OccupantName.Should().Be(request.OccupantName);
        bookingRes.StartsAtUtc.Should().Be(request.StartsAtUtc);
        bookingRes.EndsAtUtc.Should().Be(request.EndsAtUtc);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var booking = await dbContext.Bookings.FindAsync([bookingRes.Id], cancellationToken);
            booking.Should().NotBeNull();
            booking.RoomId.Should().Be(request.RoomId);
            booking.OccupantName.Should().Be(request.OccupantName);
            booking.StartsAtUtc.Should().Be(request.StartsAtUtc);
            booking.EndsAtUtc.Should().Be(request.EndsAtUtc);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData((string?)null)]
    public async Task CreateBooking_WithMissingOccupantName_ReturnsBadRequestResponse(string? occupantName)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            occupantName!,
            new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc),
            new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc)
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.OccupantName));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateBooking_WithOccupantNameTooLong_ReturnsBadRequestResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            new string('A', 101), // Occupant name longer than 100 characters
            new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc),
            new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc)
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.OccupantName));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(0);
        }
    }

    [Theory]
    [MemberData(nameof(CreateBooking_WithStartNotAfterEnd_Data))]
    public async Task CreateBooking_WithStartNotAfterEnd_ReturnsBadRequestResponse(DateTime startsAt, DateTime endsAt)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            startsAt,
            endsAt
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.StartsAtUtc))
            .WhoseValue.Should().BeEquivalentTo("The start time must be before the end time.");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(0);
        }
    }

    public static TheoryData<DateTime, DateTime> CreateBooking_WithStartNotAfterEnd_Data => new() {
        // Start time after end time
        { new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc) },
        // Start time same as end time
        { new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc) },
    };

    [Fact]
    public async Task CreateBooking_WithEndDateOnDifferentCalendarDay_ReturnsBadRequestResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc),
            new DateTime(2026, 11, 06, 10, 00, 00, DateTimeKind.Utc)
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.EndsAtUtc))
            .WhoseValue.Should().BeEquivalentTo("The end time must be on the same day as the start time.");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(0);
        }
    }

    [Fact]
    public async Task CreateBooking_WithAdjacentBookings_CreatesBookingAndReturnsOkResponse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");
        var existingBooking = room.Book("John Doe", new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 11, 00, 00, DateTimeKind.Utc));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);
            dbContext.Bookings.Add(existingBooking);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            existingBooking.EndsAtUtc,
            existingBooking.EndsAtUtc.AddMinutes(30)
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var bookingRes = await res.Content.ReadFromJsonAsync<CreateBookingResponse>(cancellationToken);
        bookingRes.Should().NotBeNull();
        bookingRes.Id.Should().NotBeEmpty();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var booking = await dbContext.Bookings.FindAsync([bookingRes.Id], cancellationToken);
            booking.Should().NotBeNull();
        }
    }

    [Theory]
    [MemberData(nameof(CreateBooking_WithOverlappingBookings_Data))]
    public async Task CreateBooking_WithOverlappingBookings_ReturnsBadRequestResponse(DateTime startsAt, DateTime endsAt)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");
        var existingBooking = room.Book("John Doe", new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 11, 00, 00, DateTimeKind.Utc));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);
            dbContext.Bookings.Add(existingBooking);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            startsAt,
            endsAt
        );

        // Act
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.RoomId)).WhoseValue.Should().BeEquivalentTo("This room is already booked during the requested period.");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(1);
        }
    }

    public static TheoryData<DateTime, DateTime> CreateBooking_WithOverlappingBookings_Data => new() {
        // Overlap at the start
        { new DateTime(2026, 11, 05, 09, 30, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 10, 30, 00, DateTimeKind.Utc) },
        // Overlap at the end
        { new DateTime(2026, 11, 05, 10, 30, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 11, 30, 00, DateTimeKind.Utc) },
        // Fully overlapping
        { new DateTime(2026, 11, 05, 10, 15, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 10, 45, 00, DateTimeKind.Utc) },
        // Fully encompassing
        { new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 12, 00, 00, DateTimeKind.Utc) },
    };

    [Fact]
    public async Task CreateBooking_WithConcurrentConflictingRequests_EnsuresOnlyOneBookingIsCreated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var startsAt = new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc);
        var endsAt = new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc);

        var request1 = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            startsAt,
            endsAt
        );

        var request2 = new CreateBookingRequest(
            room.Id,
            "John Doe",
            startsAt,
            endsAt
        );

        // Act
        var res1Task = _client.PostAsJsonAsync("/api/bookings", request1, cancellationToken);
        var res2Task = _client.PostAsJsonAsync("/api/bookings", request2, cancellationToken);
        await Task.WhenAll(res1Task, res2Task);

        // Assert
        var res1 = res1Task.Result;
        var res2 = res2Task.Result;

        (res1.IsSuccessStatusCode ^ res2.IsSuccessStatusCode).Should().BeTrue("Exactly one of the requests should succeed");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(1);
        }
    }

    [Fact]
    public async Task CreateBooking_WithConcurrentNonConflictingRequests_BothBookingsCreated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var startsAt = new DateTime(2026, 11, 05, 09, 00, 00, DateTimeKind.Utc);
        var endsAt = new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc);

        var request1 = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            startsAt,
            endsAt
        );

        var request2 = new CreateBookingRequest(
            room.Id,
            "John Doe",
            endsAt,
            endsAt.AddHours(1)
        );

        // Act
        var res1Task = _client.PostAsJsonAsync("/api/bookings", request1, cancellationToken);
        var res2Task = _client.PostAsJsonAsync("/api/bookings", request2, cancellationToken);
        await Task.WhenAll(res1Task, res2Task);

        // Assert
        var res1 = res1Task.Result;
        var res2 = res2Task.Result;

        res1.IsSuccessStatusCode.Should().BeTrue();
        res2.IsSuccessStatusCode.Should().BeTrue();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var numBookingsInDb = await dbContext.Bookings.CountAsync(cancellationToken);
            numBookingsInDb.Should().Be(2);
        }
    }

    [Fact]
    public async Task CreateBooking_WithExistingBookingCancelled_AllowsNewBookingToBeCreatedInSameSlot()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var room = Room.Create("Testing Room A");
        var existingBooking = room.Book("John Doe", new DateTime(2026, 11, 05, 10, 00, 00, DateTimeKind.Utc), new DateTime(2026, 11, 05, 11, 00, 00, DateTimeKind.Utc));

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            dbContext.Rooms.Add(room);
            dbContext.Bookings.Add(existingBooking);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var request = new CreateBookingRequest(
            room.Id,
            "Cory Andrews",
            existingBooking.StartsAtUtc,
            existingBooking.EndsAtUtc
        );

        // Act - Part 1
        var res = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var problemDetails = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        problemDetails.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey(nameof(CreateBookingRequest.RoomId)).WhoseValue.Should().BeEquivalentTo("This room is already booked during the requested period.");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var bookingsInDb = await dbContext.Bookings.ToListAsync(cancellationToken);
            bookingsInDb.Should().HaveCount(1);
            bookingsInDb.Should().Contain(b => b.Id == existingBooking.Id);
        }

        // Act - Part 2 - Cancel the existing booking and try again
        var cancelRes = await _client.DeleteAsync($"/api/bookings/{existingBooking.Id}", cancellationToken);

        // Assert
        cancelRes.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Act - Part 3 - Try creating the booking again
        var createRes = await _client.PostAsJsonAsync("/api/bookings", request, cancellationToken);
        
        // Assert
        createRes.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

            var bookingsInDb = await dbContext.Bookings.ToListAsync(cancellationToken);
            bookingsInDb.Should().HaveCount(1);
            bookingsInDb.Should().NotContain(b => b.Id == existingBooking.Id);
        }
    }
}
