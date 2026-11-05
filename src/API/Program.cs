using Bookly.Api.Data;
using Bookly.Api.Features.Bookings;
using Bookly.Api.Features.Rooms;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddSqlServerDbContext<BooklyDbContext>(connectionName: "Database", configureDbContextOptions: options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSeeding((dbContext, _) =>
        {
            var rooms = dbContext.Set<Room>();

            if (rooms.FirstOrDefault(r => r.DisplayName == "Conference Room") is null)rooms.Add(Room.Create("Conference Room"));
            if (rooms.FirstOrDefault(r => r.DisplayName == "Meeting Room A") is null) rooms.Add(Room.Create("Meeting Room A"));
            if (rooms.FirstOrDefault(r => r.DisplayName == "Meeting Room B") is null) rooms.Add(Room.Create("Meeting Room B"));

            dbContext.SaveChanges();
        });

        options.UseAsyncSeeding(async (dbContext, _, ct) =>
        {
            var rooms = dbContext.Set<Room>();

            if (await rooms.FirstOrDefaultAsync(r => r.DisplayName == "Conference Room", ct) is null) rooms.Add(Room.Create("Conference Room"));
            if (await rooms.FirstOrDefaultAsync(r => r.DisplayName == "Meeting Room A", ct) is null) rooms.Add(Room.Create("Meeting Room A"));
            if (await rooms.FirstOrDefaultAsync(r => r.DisplayName == "Meeting Room B", ct) is null) rooms.Add(Room.Create("Meeting Room B"));

            await dbContext.SaveChangesAsync(ct);
        });
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

BookingsEndpoints.MapEndpoints(app);
RoomEndpoints.MapEndpoints(app);

await app.RunAsync();
