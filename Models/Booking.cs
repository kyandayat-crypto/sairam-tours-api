
namespace SaiRamTours.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string FromCity { get; set; } = string.Empty;

    public string ToCity { get; set; } = string.Empty;

    public DateTime JourneyDate { get; set; }

    public int Passengers { get; set; }

    public string VehicleType { get; set; } = "Car";

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
