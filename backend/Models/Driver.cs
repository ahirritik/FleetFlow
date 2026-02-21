using System.ComponentModel.DataAnnotations;

namespace FleetFlow.API.Models;

public class Driver
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    public DateTime LicenseExpiry { get; set; }

    [Required, MaxLength(30)]
    public string LicenseCategory { get; set; } = string.Empty; // Truck, Van, Bike

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "OnDuty"; // OnDuty, OffDuty, Suspended, OnTrip

    public double SafetyScore { get; set; } = 100.0;

    public int TripCount { get; set; } = 0;

    public int CompletedTrips { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
