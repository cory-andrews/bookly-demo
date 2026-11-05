using Bookly.Api.Features.Bookings;
using Bookly.Api.Features.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Bookly.Api.Data;

public sealed class BooklyDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Room> Rooms => Set<Room>();
}
