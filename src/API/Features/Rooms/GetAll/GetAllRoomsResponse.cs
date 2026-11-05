namespace Bookly.Api.Features.Rooms.GetAll;

public sealed record GetAllRoomsResponse(GetAllRoomsResponse.RoomDto[] Rooms)
{
    public sealed record RoomDto(Guid Id, string DisplayName);
}
