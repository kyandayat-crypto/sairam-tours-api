
using Microsoft.EntityFrameworkCore;
using SaiRamTours.Api.Models;

namespace SaiRamTours.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();
}
