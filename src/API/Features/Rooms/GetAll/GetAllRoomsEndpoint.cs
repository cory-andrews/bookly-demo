using Bookly.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Features.Rooms.GetAll;

public static class GetAllRoomsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/rooms", HandleAsync)
            .WithTags("Rooms");
    }

    private static async Task<Ok<GetAllRoomsResponse>> HandleAsync(BooklyDbContext dbContext, CancellationToken cancellationToken)
    {
        var rooms = await dbContext.Rooms
            .Select(r => new GetAllRoomsResponse.RoomDto(r.Id, r.DisplayName))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new GetAllRoomsResponse([.. rooms]));
    }
}
