using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetFlow.API.Data;
using FleetFlow.API.DTOs;
using FleetFlow.API.Models;

namespace FleetFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public MaintenanceController(FleetFlowDbContext db) => _db = db;

    private async Task Audit(string action, string entityType, int entityId, string details)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdString, out var id) ? id : 0;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
        _db.AuditLogs.Add(new AuditLog 
        { 
            UserId = userId, 
            UserName = userName, 
            Action = action, 
            EntityType = entityType, 
            EntityId = entityId, 
            Details = details,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? vehicleId)
    {
        var q = _db.MaintenanceLogs.Include(m => m.Vehicle).AsQueryable();
        if (vehicleId.HasValue) q = q.Where(m => m.VehicleId == vehicleId.Value);

        var logs = await q.OrderByDescending(m => m.Date).Select(m => new MaintenanceLogDto(
            m.Id, m.VehicleId, m.Vehicle!.Name, m.ServiceType, m.Description, m.Cost, m.Date, m.IsCompleted
        )).ToListAsync();

        return Ok(logs);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] MaintenanceCreateDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { message = "Vehicle not found." });
        if (vehicle.Status == "OnTrip") return BadRequest(new { message = "Cannot schedule maintenance for a vehicle on trip." });

        var log = new MaintenanceLog
        {
            VehicleId = dto.VehicleId, ServiceType = dto.ServiceType,
            Description = dto.Description, Cost = dto.Cost, Date = dto.Date
        };

        // Auto-set vehicle status to InShop
        vehicle.Status = "InShop";

        _db.MaintenanceLogs.Add(log);
        await _db.SaveChangesAsync();
        await Audit("Created", "MaintenanceLog", log.Id, $"Scheduled {log.ServiceType} for vehicle {vehicle.Name}");
        return Ok(new MaintenanceLogDto(log.Id, log.VehicleId, vehicle.Name, log.ServiceType, log.Description, log.Cost, log.Date, log.IsCompleted));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] MaintenanceCreateDto dto)
    {
        var log = await _db.MaintenanceLogs.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
        if (log == null) return NotFound();
        if (log.IsCompleted) return BadRequest(new { message = "Cannot edit a completed maintenance log." });

        log.ServiceType = dto.ServiceType;
        log.Description = dto.Description;
        log.Cost = dto.Cost;
        log.Date = dto.Date;

        await _db.SaveChangesAsync();
        await Audit("Updated", "MaintenanceLog", id, $"Updated {log.ServiceType} for vehicle {log.Vehicle!.Name}");
        return Ok(new MaintenanceLogDto(log.Id, log.VehicleId, log.Vehicle.Name, log.ServiceType, log.Description, log.Cost, log.Date, log.IsCompleted));
    }

    [HttpPatch("{id}/complete")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Complete(int id)
    {
        var log = await _db.MaintenanceLogs.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
        if (log == null) return NotFound();
        if (log.IsCompleted) return BadRequest(new { message = "Maintenance already completed." });

        log.IsCompleted = true;

        // Check if there are any other active maintenance for this vehicle
        var hasOtherActive = await _db.MaintenanceLogs
            .AnyAsync(m => m.VehicleId == log.VehicleId && m.Id != id && !m.IsCompleted);

        if (!hasOtherActive)
            log.Vehicle!.Status = "Available";

        await _db.SaveChangesAsync();
        await Audit("Completed", "MaintenanceLog", log.Id, $"Completed {log.ServiceType} for vehicle {log.Vehicle!.Name}");
        return Ok(new MaintenanceLogDto(log.Id, log.VehicleId, log.Vehicle!.Name, log.ServiceType, log.Description, log.Cost, log.Date, log.IsCompleted));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _db.MaintenanceLogs.FindAsync(id);
        if (log == null) return NotFound();
        _db.MaintenanceLogs.Remove(log);
        await _db.SaveChangesAsync();
        await Audit("Deleted", "MaintenanceLog", id, $"Deleted maintenance log #{id}");
        return NoContent();
    }
}
