using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetFlow.API.Models;

public class MaintenanceLog
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    [ForeignKey("VehicleId")]
    public Vehicle? Vehicle { get; set; }

    [Required, MaxLength(100)]
    public string ServiceType { get; set; } = string.Empty; // Oil Change, Tire Rotation, Brake Repair, etc.

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public double Cost { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; } = false;
}
