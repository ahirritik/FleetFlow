using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetFlow.API.Models;

public class Trip
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    [ForeignKey("VehicleId")]
    public Vehicle? Vehicle { get; set; }

    public int DriverId { get; set; }
    [ForeignKey("DriverId")]
    public Driver? Driver { get; set; }

    [Required, MaxLength(200)]
    public string Origin { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Destination { get; set; } = string.Empty;

    public double CargoWeight { get; set; } // kg

    [MaxLength(200)]
    public string CargoDescription { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Dispatched, Completed, Cancelled

    public double StartOdometer { get; set; }
    public double EndOdometer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
