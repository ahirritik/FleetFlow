using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetFlow.API.Models;

public class Expense
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    [ForeignKey("VehicleId")]
    public Vehicle? Vehicle { get; set; }

    public int? TripId { get; set; }
    [ForeignKey("TripId")]
    public Trip? Trip { get; set; }

    [Required, MaxLength(30)]
    public string Category { get; set; } = "Fuel"; // Fuel, Toll, Other

    public double? Liters { get; set; }

    public double Cost { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string Notes { get; set; } = string.Empty;
}
