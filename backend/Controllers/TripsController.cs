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
public class TripsController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public TripsController(FleetFlowDbContext db) => _db = db;

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
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var q = _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);

        var trips = await q.OrderByDescending(t => t.CreatedAt).Select(t => new TripDto(
            t.Id, t.VehicleId, t.Vehicle!.Name, t.Vehicle.LicensePlate,
            t.DriverId, t.Driver!.FullName, t.Origin, t.Destination,
            t.CargoWeight, t.CargoDescription, t.Status, t.StartOdometer, t.EndOdometer,
            t.CreatedAt, t.DispatchedAt, t.CompletedAt
        )).ToListAsync();

        return Ok(trips);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (t == null) return NotFound();
        return Ok(new TripDto(t.Id, t.VehicleId, t.Vehicle!.Name, t.Vehicle.LicensePlate,
            t.DriverId, t.Driver!.FullName, t.Origin, t.Destination,
            t.CargoWeight, t.CargoDescription, t.Status, t.StartOdometer, t.EndOdometer,
            t.CreatedAt, t.DispatchedAt, t.CompletedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Create([FromBody] TripCreateDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { message = "Vehicle not found." });

        var driver = await _db.Drivers.FindAsync(dto.DriverId);
        if (driver == null) return BadRequest(new { message = "Driver not found." });

        // Validation: vehicle must be available
        if (vehicle.Status != "Available")
            return BadRequest(new { message = $"Vehicle '{vehicle.Name}' is not available (current status: {vehicle.Status})." });

        // Validation: driver must be on duty
        if (driver.Status != "OnDuty")
            return BadRequest(new { message = $"Driver '{driver.FullName}' is not on duty (current status: {driver.Status})." });

        // Validation: driver license must not be expired
        if (driver.LicenseExpiry < DateTime.UtcNow)
            return BadRequest(new { message = $"Driver '{driver.FullName}' has an expired license (expired: {driver.LicenseExpiry:yyyy-MM-dd})." });

        // Validation: license category must match vehicle type
        if (driver.LicenseCategory != vehicle.Type)
            return BadRequest(new { message = $"Driver license category '{driver.LicenseCategory}' does not match vehicle type '{vehicle.Type}'." });

        // Validation: cargo weight must not exceed max capacity
        if (dto.CargoWeight > vehicle.MaxCapacity)
            return BadRequest(new { message = $"Cargo weight ({dto.CargoWeight}kg) exceeds vehicle max capacity ({vehicle.MaxCapacity}kg)." });

        var trip = new Trip
        {
            VehicleId = dto.VehicleId, DriverId = dto.DriverId,
            Origin = dto.Origin, Destination = dto.Destination,
            CargoWeight = dto.CargoWeight, CargoDescription = dto.CargoDescription,
            Status = "Draft", StartOdometer = vehicle.Odometer
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        await Audit("Created", "Trip", trip.Id, $"Created trip from {trip.Origin} to {trip.Destination} for vehicle {vehicle.Name}");

        return CreatedAtAction(nameof(Get), new { id = trip.Id },
            new TripDto(trip.Id, trip.VehicleId, vehicle.Name, vehicle.LicensePlate,
                trip.DriverId, driver.FullName, trip.Origin, trip.Destination,
                trip.CargoWeight, trip.CargoDescription, trip.Status, trip.StartOdometer, trip.EndOdometer,
                trip.CreatedAt, trip.DispatchedAt, trip.CompletedAt));
    }

    [HttpPost("{id}/dispatch")]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Dispatch(int id)
    {
        var trip = await _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();
        if (trip.Status != "Draft") return BadRequest(new { message = "Only Draft trips can be dispatched." });

        trip.Status = "Dispatched";
        trip.DispatchedAt = DateTime.UtcNow;
        trip.Vehicle!.Status = "OnTrip";
        trip.Driver!.Status = "OnTrip";
        trip.Driver.TripCount++;

        await _db.SaveChangesAsync();
        await Audit("Dispatched", "Trip", trip.Id, $"Dispatched trip #{trip.Id} with driver {trip.Driver.FullName}");
        return Ok(new TripDto(trip.Id, trip.VehicleId, trip.Vehicle.Name, trip.Vehicle.LicensePlate,
            trip.DriverId, trip.Driver.FullName, trip.Origin, trip.Destination,
            trip.CargoWeight, trip.CargoDescription, trip.Status, trip.StartOdometer, trip.EndOdometer,
            trip.CreatedAt, trip.DispatchedAt, trip.CompletedAt));
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Complete(int id, [FromBody] TripCompleteDto dto)
    {
        var trip = await _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();
        if (trip.Status != "Dispatched") return BadRequest(new { message = "Only Dispatched trips can be completed." });

        trip.Status = "Completed";
        trip.CompletedAt = DateTime.UtcNow;
        trip.EndOdometer = dto.EndOdometer;
        trip.Vehicle!.Odometer = dto.EndOdometer;
        trip.Vehicle.Status = "Available";
        trip.Driver!.Status = "OnDuty";
        trip.Driver.CompletedTrips++;

        // Auto-calculate safety score
        trip.Driver.SafetyScore = trip.Driver.TripCount > 0
            ? Math.Round((double)trip.Driver.CompletedTrips / trip.Driver.TripCount * 100, 1)
            : 100;

        await _db.SaveChangesAsync();
        await Audit("Completed", "Trip", trip.Id, $"Completed trip #{trip.Id}. End odometer: {dto.EndOdometer}");
        return Ok(new TripDto(trip.Id, trip.VehicleId, trip.Vehicle.Name, trip.Vehicle.LicensePlate,
            trip.DriverId, trip.Driver.FullName, trip.Origin, trip.Destination,
            trip.CargoWeight, trip.CargoDescription, trip.Status, trip.StartOdometer, trip.EndOdometer,
            trip.CreatedAt, trip.DispatchedAt, trip.CompletedAt));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Cancel(int id)
    {
        var trip = await _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();
        if (trip.Status == "Completed" || trip.Status == "Cancelled")
            return BadRequest(new { message = "Cannot cancel a completed or already cancelled trip." });

        if (trip.Status == "Dispatched")
        {
            trip.Vehicle!.Status = "Available";
            trip.Driver!.Status = "OnDuty";

            // Dispatched cancel counts against safety score
            trip.Driver.SafetyScore = trip.Driver.TripCount > 0
                ? Math.Round((double)trip.Driver.CompletedTrips / trip.Driver.TripCount * 100, 1)
                : 100;
        }

        trip.Status = "Cancelled";
        await _db.SaveChangesAsync();
        await Audit("Cancelled", "Trip", trip.Id, $"Cancelled trip #{trip.Id}");
        return Ok(new TripDto(trip.Id, trip.VehicleId, trip.Vehicle!.Name, trip.Vehicle.LicensePlate,
            trip.DriverId, trip.Driver!.FullName, trip.Origin, trip.Destination,
            trip.CargoWeight, trip.CargoDescription, trip.Status, trip.StartOdometer, trip.EndOdometer,
            trip.CreatedAt, trip.DispatchedAt, trip.CompletedAt));
    }
}
