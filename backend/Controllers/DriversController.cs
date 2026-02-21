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
        return NoContent();
    }
}
