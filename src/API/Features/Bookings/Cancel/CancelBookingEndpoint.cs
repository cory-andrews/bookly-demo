using Bookly.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Bookly.Api.Features.Bookings.Cancel;

public static class CancelBookingEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapDelete("/api/bookings/{bookingId:guid}", HandleAsync)
            .WithTags("Bookings");
    }

    private static async Task<Ok> HandleAsync(BooklyDbContext dbContext, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings.FindAsync([bookingId], cancellationToken);
        if (booking is null)
        {
            return TypedResults.Ok();
        }

        dbContext.Bookings.Remove(booking);

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
