using Bookly.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Features.Bookings.GetByDate;

public static class GetBookingsByDateEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/bookings", HandleAsync)
            .WithSummary("Get bookings by date")
            .WithDescription("Retrieves all bookings for a specific date")
            .WithTags("Bookings");
    }

    private static async Task<Ok<GetBookingsByDateResponse>> HandleAsync(BooklyDbContext dbContext, DateOnly date, CancellationToken cancellationToken)
    {
        var bookings = await dbContext.Bookings
            .Where(b => b.StartsAtUtc.Date == date.ToDateTime(TimeOnly.MinValue))
            .Select(b => new GetBookingsByDateResponse.BookingDto(
                b.Id,
                b.RoomId,
                b.OccupantName,
                b.StartsAtUtc,
                b.EndsAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new GetBookingsByDateResponse([.. bookings]));
    }
}
