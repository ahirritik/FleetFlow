using System.ComponentModel.DataAnnotations;

namespace FleetFlow.API.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string LicensePlate { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Type { get; set; } = "Truck"; // Truck, Van, Bike

    public double MaxCapacity { get; set; } // kg

    public double Odometer { get; set; } // km

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Available"; // Available, OnTrip, InShop, Retired

    [MaxLength(50)]
    public string Region { get; set; } = string.Empty;

    public double AcquisitionCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<MaintenanceLog> MaintenanceLogs { get; set; } = new List<MaintenanceLog>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
