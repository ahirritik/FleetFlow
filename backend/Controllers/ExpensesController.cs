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
public class ExpensesController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public ExpensesController(FleetFlowDbContext db) => _db = db;

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
    public async Task<IActionResult> GetAll([FromQuery] int? vehicleId, [FromQuery] int? tripId)
    {
        var q = _db.Expenses.Include(e => e.Vehicle).AsQueryable();
        if (vehicleId.HasValue) q = q.Where(e => e.VehicleId == vehicleId.Value);
        if (tripId.HasValue) q = q.Where(e => e.TripId == tripId.Value);

        var expenses = await q.OrderByDescending(e => e.Date).Select(e => new ExpenseDto(
            e.Id, e.VehicleId, e.Vehicle!.Name, e.TripId, e.Category, e.Liters, e.Cost, e.Date, e.Notes
        )).ToListAsync();

        return Ok(expenses);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Create([FromBody] ExpenseCreateDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { message = "Vehicle not found." });

        if (dto.TripId.HasValue)
        {
            var trip = await _db.Trips.FindAsync(dto.TripId.Value);
            if (trip == null) return BadRequest(new { message = "Trip not found." });
        }

        var expense = new Expense
        {
            VehicleId = dto.VehicleId, TripId = dto.TripId, Category = dto.Category,
            Liters = dto.Liters, Cost = dto.Cost, Date = dto.Date, Notes = dto.Notes
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        await Audit("Created", "Expense", expense.Id, $"Logged {expense.Category} expense of ₹{expense.Cost} for vehicle {vehicle.Name}");
        return Ok(new ExpenseDto(expense.Id, expense.VehicleId, vehicle.Name, expense.TripId, expense.Category,
            expense.Liters, expense.Cost, expense.Date, expense.Notes));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] ExpenseCreateDto dto)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense == null) return NotFound();

        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { message = "Vehicle not found." });

        expense.VehicleId = dto.VehicleId;
        expense.TripId = dto.TripId;
        expense.Category = dto.Category;
        expense.Liters = dto.Liters;
        expense.Cost = dto.Cost;
        expense.Date = dto.Date;
        expense.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        await Audit("Updated", "Expense", id, $"Updated expense #{id} to ₹{expense.Cost} for vehicle {vehicle.Name}");
        return Ok(new ExpenseDto(expense.Id, expense.VehicleId, vehicle.Name, expense.TripId, expense.Category,
            expense.Liters, expense.Cost, expense.Date, expense.Notes));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Dispatcher")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e == null) return NotFound();
        _db.Expenses.Remove(e);
        await _db.SaveChangesAsync();
        await Audit("Deleted", "Expense", id, $"Deleted expense log #{id}");
        return NoContent();
    }
}
