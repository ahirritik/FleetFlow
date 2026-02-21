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
public class DriversController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public DriversController(FleetFlowDbContext db) => _db = db;

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
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? category)
    {
        var q = _db.Drivers.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(d => d.Status == status);
        if (!string.IsNullOrEmpty(category)) q = q.Where(d => d.LicenseCategory == category);

        var drivers = await q.OrderBy(d => d.FullName).Select(d => new DriverDto(
            d.Id, d.FullName, d.LicenseNumber, d.LicenseExpiry, d.LicenseCategory,
            d.Phone, d.Status, d.SafetyScore, d.TripCount, d.CompletedTrips, d.CreatedAt
        )).ToListAsync();

        return Ok(drivers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _db.Drivers.FindAsync(id);
        if (d == null) return NotFound();
        return Ok(new DriverDto(d.Id, d.FullName, d.LicenseNumber, d.LicenseExpiry, d.LicenseCategory,
            d.Phone, d.Status, d.SafetyScore, d.TripCount, d.CompletedTrips, d.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,SafetyOfficer")]
    public async Task<IActionResult> Create([FromBody] DriverCreateDto dto)
    {
        if (await _db.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber))
            return BadRequest(new { message = "License number already exists." });

        var driver = new Driver
        {
            FullName = dto.FullName, LicenseNumber = dto.LicenseNumber,
            LicenseExpiry = dto.LicenseExpiry, LicenseCategory = dto.LicenseCategory,
            Phone = dto.Phone, Status = "OnDuty"
        };

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();
        await Audit("Created", "Driver", driver.Id, $"Created driver profile for {driver.FullName}");
        return CreatedAtAction(nameof(Get), new { id = driver.Id },
            new DriverDto(driver.Id, driver.FullName, driver.LicenseNumber, driver.LicenseExpiry,
                driver.LicenseCategory, driver.Phone, driver.Status, driver.SafetyScore, driver.TripCount, driver.CompletedTrips, driver.CreatedAt));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,SafetyOfficer")]
    public async Task<IActionResult> Update(int id, [FromBody] DriverUpdateDto dto)
    {
        var d = await _db.Drivers.FindAsync(id);
        if (d == null) return NotFound();

        if (d.LicenseNumber != dto.LicenseNumber && await _db.Drivers.AnyAsync(x => x.LicenseNumber == dto.LicenseNumber))
            return BadRequest(new { message = "License number already exists." });

        d.FullName = dto.FullName; d.LicenseNumber = dto.LicenseNumber;
        d.LicenseExpiry = dto.LicenseExpiry; d.LicenseCategory = dto.LicenseCategory;
        d.Phone = dto.Phone;
        await _db.SaveChangesAsync();
        await Audit("Updated", "Driver", d.Id, $"Updated driver profile for {d.FullName}");
        return Ok(new DriverDto(d.Id, d.FullName, d.LicenseNumber, d.LicenseExpiry, d.LicenseCategory,
            d.Phone, d.Status, d.SafetyScore, d.TripCount, d.CompletedTrips, d.CreatedAt));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Manager,SafetyOfficer")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateRequest req)
    {
        var d = await _db.Drivers.FindAsync(id);
        if (d == null) return NotFound();
        if (d.Status == "OnTrip") return BadRequest(new { message = "Cannot change status while driver is on a trip." });

        var valid = new[] { "OnDuty", "OffDuty", "Suspended" };
        if (!valid.Contains(req.Status)) return BadRequest(new { message = $"Status must be one of: {string.Join(", ", valid)}" });

        d.Status = req.Status;
        await _db.SaveChangesAsync();
        await Audit("StatusChanged", "Driver", d.Id, $"Driver status changed to '{req.Status}' for {d.FullName}");
        return Ok(new DriverDto(d.Id, d.FullName, d.LicenseNumber, d.LicenseExpiry, d.LicenseCategory,
            d.Phone, d.Status, d.SafetyScore, d.TripCount, d.CompletedTrips, d.CreatedAt));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,SafetyOfficer")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Drivers.FindAsync(id);
        if (d == null) return NotFound();
        if (d.Status == "OnTrip") return BadRequest(new { message = "Cannot delete a driver that is on a trip." });

        _db.Drivers.Remove(d);
        await _db.SaveChangesAsync();
        await Audit("Deleted", "Driver", id, $"Deleted driver profile for {d.FullName}");
        return NoContent();
    }
}
