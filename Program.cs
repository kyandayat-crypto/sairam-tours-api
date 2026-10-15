
using Microsoft.EntityFrameworkCore;
using SaiRamTours.Api.Data;
using SaiRamTours.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------- DATABASE CONFIGURATION --------------------

var connectionString =
    builder.Configuration.GetConnectionString("MySqlConnection")
    ?? Environment.GetEnvironmentVariable("MYSQLCONNSTR_MySqlConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "MySqlConnection is not configured. Check appsettings.json or environment variables."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// -------------------- CORS CONFIGURATION --------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://172.20.10.2:5173",
                "https://sairam-tours-frontend.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// -------------------- MIDDLEWARE --------------------

app.UseCors("ReactFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Render and other reverse-proxy hosts normally terminate HTTPS
// at the proxy. Configure forwarded headers separately if needed.

// -------------------- ROOT / HEALTH CHECK --------------------

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        success = true,
        message = "Sai Ram Tours & Travels API is running!"
    });
});

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        success = true,
        message = "Sai Ram Tours & Travels API is connected!"
    });
});

// -------------------- CREATE BOOKING --------------------

app.MapPost("/api/bookings", async (
    Booking booking,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(booking.CustomerName) ||
        string.IsNullOrWhiteSpace(booking.Phone) ||
        string.IsNullOrWhiteSpace(booking.FromCity) ||
        string.IsNullOrWhiteSpace(booking.ToCity))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Please fill all required fields."
        });
    }

    if (booking.Phone.Length != 10 ||
        !booking.Phone.All(char.IsDigit))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Please enter a valid 10-digit mobile number."
        });
    }

    if (booking.Passengers < 1 || booking.Passengers > 50)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Passengers must be between 1 and 50."
        });
    }

    if (booking.JourneyDate == default)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Please select a journey date."
        });
    }

    if (booking.JourneyDate.Date < DateTime.Today)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Journey date cannot be in the past."
        });
    }

    if (string.IsNullOrWhiteSpace(booking.VehicleType))
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Please select a vehicle."
        });
    }

    booking.Id = 0;
    booking.CustomerName = booking.CustomerName.Trim();
    booking.Phone = booking.Phone.Trim();
    booking.FromCity = booking.FromCity.Trim();
    booking.ToCity = booking.ToCity.Trim();
    booking.VehicleType = booking.VehicleType.Trim();
    booking.Status = "Pending";
    booking.CreatedAt = DateTime.UtcNow;

    try
    {
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/bookings/{booking.Id}",
            new
            {
                success = true,
                message = "Booking saved successfully!",
                bookingId = booking.Id,
                status = booking.Status
            }
        );
    }
    catch (DbUpdateException)
    {
        return Results.Problem(
            title: "Booking could not be saved",
            detail: "Please check the database connection and booking table.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

// -------------------- GET ALL BOOKINGS (ADMIN) --------------------

app.MapGet("/api/bookings", async (
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var bookings = await db.Bookings
        .AsNoTracking()
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync(cancellationToken);

    return Results.Ok(bookings);
});

// -------------------- GET BOOKING BY ID --------------------

app.MapGet("/api/bookings/{id:int}", async (
    int id,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var booking = await db.Bookings
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    if (booking is null)
    {
        return Results.NotFound(new
        {
            success = false,
            message = "Booking not found."
        });
    }

    return Results.Ok(booking);
});

// -------------------- START API --------------------

app.Run();
