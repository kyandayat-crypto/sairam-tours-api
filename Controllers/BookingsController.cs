using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaiRamTours.Api.Data;
using SaiRamTours.Api.Models;

namespace SaiRamTours.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookingsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Bookings
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
    {
        var bookings = await _context.Bookings
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings);
    }

    // GET: api/Bookings/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Booking>> GetBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);

        if (booking == null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        return Ok(booking);
    }

    // POST: api/Bookings
    [HttpPost]
    public async Task<ActionResult<Booking>> CreateBooking(
        [FromBody] Booking booking)
    {
        if (string.IsNullOrWhiteSpace(booking.CustomerName) ||
            string.IsNullOrWhiteSpace(booking.Phone) ||
            string.IsNullOrWhiteSpace(booking.FromCity) ||
            string.IsNullOrWhiteSpace(booking.ToCity))
        {
            return BadRequest(new
            {
                message = "Please enter customer name, phone, from city and to city."
            });
        }

        if (booking.JourneyDate.Date < DateTime.UtcNow.Date ||
            booking.Passengers < 1)
        {
            return BadRequest(new
            {
                message = "Please enter a valid journey date and passenger count."
            });
        }

        booking.Id = 0;
        booking.Status = "Pending";
        booking.CreatedAt = DateTime.UtcNow;

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBooking),
            new { id = booking.Id },
            booking);
    }

    // PUT: api/Bookings/1/status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateBookingStatus(
        int id,
        [FromBody] UpdateBookingStatusRequest request)
    {
        var booking = await _context.Bookings.FindAsync(id);

        if (booking == null)
        {
            return NotFound(new { message = "Booking not found." });
        }

        var allowedStatuses = new[]
        {
            "Pending",
            "Confirmed",
            "Cancelled",
            "Completed"
        };

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !allowedStatuses.Contains(
                request.Status,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid booking status." });
        }

        booking.Status = allowedStatuses.First(
            s => s.Equals(
                request.Status,
                StringComparison.OrdinalIgnoreCase));

        await _context.SaveChangesAsync();

        return Ok(booking);
    }
}

public class UpdateBookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}