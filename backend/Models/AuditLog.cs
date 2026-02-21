using System.ComponentModel.DataAnnotations;

namespace FleetFlow.API.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [MaxLength(100)] public string UserName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, StatusChanged, Dispatched, Completed, Cancelled
    [Required, MaxLength(50)] public string EntityType { get; set; } = string.Empty; // Vehicle, Driver, Trip, MaintenanceLog, Expense, User
    public int EntityId { get; set; }
    [MaxLength(500)] public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
