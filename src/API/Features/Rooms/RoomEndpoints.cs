using Bookly.Api.Features.Rooms.GetAll;

namespace Bookly.Api.Features.Rooms;

public static class RoomEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        GetAllRoomsEndpoint.MapEndpoint(builder);
    }
}
