
using Microsoft.EntityFrameworkCore;
using SaiRamTours.Api.Data;
using SaiRamTours.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("MySqlConnection")
    ?? Environment.GetEnvironmentVariable("MYSQLCONNSTR_MySqlConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "MySqlConnection is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://172.20.10.2:5173",
                "https://sairam-tours-frontend.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("ReactFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        success = true,
        message = "Sai Ram Tours & Travels API is connected!"
    });
});

app.MapPost("/api/bookings", async (
    Booking booking,
    AppDbContext db) =>
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

    if (booking.Passengers < 1 || booking.Passengers > 50)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = "Passengers must be between 1 and 50."
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

    booking.Id = 0;
    booking.Status = "Pending";
    booking.CreatedAt = DateTime.UtcNow;

    db.Bookings.Add(booking);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/bookings/{booking.Id}",
        new
        {
            success = true,
            message = "Booking saved successfully!",
            bookingId = booking.Id
        });
});

app.MapGet("/api/bookings", async (AppDbContext db) =>
{
    var bookings = await db.Bookings
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return Results.Ok(bookings);
});

app.Run();
