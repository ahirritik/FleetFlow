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
        return Ok(new ExpenseDto(expense.Id, expense.VehicleId, vehicle.Name, expense.TripId, expense.Category,
            expense.Liters, expense.Cost, expense.Date, expense.Notes));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e == null) return NotFound();
        _db.Expenses.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
