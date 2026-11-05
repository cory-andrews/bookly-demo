using Bookly.Api.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Features.Bookings.Create;

public static class CreateBookingEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/bookings", HandleAsync)
            .WithSummary("Create booking")
            .WithDescription("Creates a new booking for a room")
            .WithTags("Bookings");
    }

    private static async Task<Results<Ok<CreateBookingResponse>, ValidationProblem>> HandleAsync(
        CreateBookingRequest request,
        BooklyDbContext dbContext,
        IValidator<CreateBookingRequest> requestValidator,
        CancellationToken cancellationToken
    )
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        while (true)
        {
            var room = await dbContext.Rooms.FindAsync([request.RoomId], cancellationToken);

            if (room is null)
            {
                return TypedResults.ValidationProblem([KeyValuePair.Create<string, string[]>("RoomId", ["Could not find the specified room."])]);
            }

            // Check for any overlaps before creating the booking
            if (await IsOverlappingAsync(dbContext, request.RoomId, request.StartsAtUtc, request.EndsAtUtc, cancellationToken))
            {
                return TypedResults.ValidationProblem([KeyValuePair.Create<string, string[]>("RoomId", ["This room is already booked during the requested period."])]);
            }

            var booking = room.Book(request.OccupantName, request.StartsAtUtc, request.EndsAtUtc);

            dbContext.Bookings.Add(booking);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The same room was booked by another request, that may or may not overlap with the current booking request
                // Clear the tracker and retry the operation
                dbContext.ChangeTracker.Clear();
                continue;
            }

            return TypedResults.Ok(new CreateBookingResponse(
                booking.Id,
                booking.RoomId,
                booking.OccupantName,
                booking.StartsAtUtc,
                booking.EndsAtUtc
            ));
        }

        static Task<bool> IsOverlappingAsync(BooklyDbContext dbContext, Guid roomId, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken cancellationToken)
        {
            return dbContext.Bookings.AnyAsync(b =>
                b.RoomId == roomId &&
                b.StartsAtUtc < endsAtUtc &&
                b.EndsAtUtc > startsAtUtc,
                cancellationToken
            );
        }
    }
}
