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
public class VehiclesController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public VehiclesController(FleetFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? status, [FromQuery] string? region)
    {
        var q = _db.Vehicles.AsQueryable();
        if (!string.IsNullOrEmpty(type)) q = q.Where(v => v.Type == type);
        if (!string.IsNullOrEmpty(status)) q = q.Where(v => v.Status == status);
        if (!string.IsNullOrEmpty(region)) q = q.Where(v => v.Region == region);

        var vehicles = await q.OrderBy(v => v.Name).Select(v => new VehicleDto(
            v.Id, v.Name, v.Model, v.LicensePlate, v.Type, v.MaxCapacity,
            v.Odometer, v.Status, v.Region, v.AcquisitionCost, v.CreatedAt
        )).ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        return Ok(new VehicleDto(v.Id, v.Name, v.Model, v.LicensePlate, v.Type, v.MaxCapacity,
            v.Odometer, v.Status, v.Region, v.AcquisitionCost, v.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] VehicleCreateDto dto)
    {
        if (await _db.Vehicles.AnyAsync(v => v.LicensePlate == dto.LicensePlate))
            return BadRequest(new { message = "License plate already exists." });

        var vehicle = new Vehicle
        {
            Name = dto.Name, Model = dto.Model, LicensePlate = dto.LicensePlate,
            Type = dto.Type, MaxCapacity = dto.MaxCapacity, Odometer = dto.Odometer,
            Region = dto.Region, AcquisitionCost = dto.AcquisitionCost, Status = "Available"
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = vehicle.Id },
            new VehicleDto(vehicle.Id, vehicle.Name, vehicle.Model, vehicle.LicensePlate, vehicle.Type,
                vehicle.MaxCapacity, vehicle.Odometer, vehicle.Status, vehicle.Region, vehicle.AcquisitionCost, vehicle.CreatedAt));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleUpdateDto dto)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return NotFound();

        if (v.LicensePlate != dto.LicensePlate && await _db.Vehicles.AnyAsync(x => x.LicensePlate == dto.LicensePlate))
            return BadRequest(new { message = "License plate already exists." });

        v.Name = dto.Name; v.Model = dto.Model; v.LicensePlate = dto.LicensePlate;
        v.Type = dto.Type; v.MaxCapacity = dto.MaxCapacity; v.Region = dto.Region;
        v.AcquisitionCost = dto.AcquisitionCost;
        await _db.SaveChangesAsync();
        return Ok(new VehicleDto(v.Id, v.Name, v.Model, v.LicensePlate, v.Type, v.MaxCapacity,
            v.Odometer, v.Status, v.Region, v.AcquisitionCost, v.CreatedAt));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateRequest req)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        if (v.Status == "OnTrip") return BadRequest(new { message = "Cannot change status while vehicle is on a trip." });

        var valid = new[] { "Available", "InShop", "Retired" };
        if (!valid.Contains(req.Status)) return BadRequest(new { message = $"Status must be one of: {string.Join(", ", valid)}" });

        v.Status = req.Status;
        await _db.SaveChangesAsync();
        return Ok(new VehicleDto(v.Id, v.Name, v.Model, v.LicensePlate, v.Type, v.MaxCapacity,
            v.Odometer, v.Status, v.Region, v.AcquisitionCost, v.CreatedAt));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        if (v.Status == "OnTrip") return BadRequest(new { message = "Cannot delete a vehicle that is on a trip." });

        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record StatusUpdateRequest(string Status);
