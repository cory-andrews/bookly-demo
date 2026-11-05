using Bookly.Api.Features.Bookings.Cancel;
using Bookly.Api.Features.Bookings.Create;
using Bookly.Api.Features.Bookings.GetByDate;

namespace Bookly.Api.Features.Bookings;

public static class BookingsEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder builder)
    {
        CreateBookingEndpoint.MapEndpoint(builder);
        CancelBookingEndpoint.MapEndpoint(builder);
        GetBookingsByDateEndpoint.MapEndpoint(builder);
    }
}
