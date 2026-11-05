using AwesomeAssertions;
using Bookly.Api.Data;
using Bookly.Api.Features.Rooms;
using Bookly.Api.Features.Rooms.GetAll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Bookly.IntegrationTests.Rooms;

[Collection(nameof(ApiCollection))]
public sealed class GetAllRoomsTests(BooklyApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        // Clear any rooms from the db before each test run
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
        await dbContext.Rooms.ExecuteDeleteAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetAllRooms_WithNoSeededRooms_ReturnsOkResponseWithEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var res = await _client.GetAsync("/api/rooms", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await res.Content.ReadFromJsonAsync<GetAllRoomsResponse>(cancellationToken);
        content.Should().NotBeNull();
        content.Rooms.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAllRooms_WithSeededRooms_ReturnsOkResponseWithExpectedRooms()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();

        dbContext.Rooms.AddRange(
            Room.Create("Testing Room A"),
            Room.Create("Testing Room B")
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var res = await _client.GetAsync("/api/rooms", cancellationToken);

        // Assert
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await res.Content.ReadFromJsonAsync<GetAllRoomsResponse>(cancellationToken);
        content.Should().NotBeNull();
        content.Rooms.Should().Satisfy(
            r => r.DisplayName == "Testing Room A",
            r => r.DisplayName == "Testing Room B"
        );
    }
}
