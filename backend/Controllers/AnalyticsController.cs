using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetFlow.API.Data;
using FleetFlow.API.DTOs;
using System.Text;

namespace FleetFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public AnalyticsController(FleetFlowDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalVehicles = await _db.Vehicles.CountAsync();
        var activeFleet = await _db.Vehicles.CountAsync(v => v.Status == "OnTrip");
        var maintenanceAlerts = await _db.Vehicles.CountAsync(v => v.Status == "InShop");
        var availableVehicles = await _db.Vehicles.CountAsync(v => v.Status == "Available");
        var totalDrivers = await _db.Drivers.CountAsync();
        var totalTrips = await _db.Trips.CountAsync(t => t.Status == "Completed");
        var pendingCargo = await _db.Trips.CountAsync(t => t.Status == "Draft");

        var utilization = totalVehicles > 0
            ? Math.Round((double)(activeFleet + maintenanceAlerts) / totalVehicles * 100, 1)
            : 0;

        var totalRevenue = await _db.Trips
            .Where(t => t.Status == "Completed")
            .SumAsync(t => t.CargoWeight * 0.5); // $0.50 per kg as revenue model

        return Ok(new DashboardDto(activeFleet, maintenanceAlerts, utilization, pendingCargo,
            totalVehicles, totalDrivers, totalTrips, Math.Round(totalRevenue, 2)));
    }

    [HttpGet("vehicle-costs")]
    public async Task<IActionResult> GetVehicleCosts()
    {
        var vehicles = await _db.Vehicles.ToListAsync();
        var result = new List<VehicleCostDto>();

        foreach (var v in vehicles)
        {
            var fuelCost = await _db.Expenses
                .Where(e => e.VehicleId == v.Id && e.Category == "Fuel")
                .SumAsync(e => e.Cost);

            var maintenanceCost = await _db.MaintenanceLogs
                .Where(m => m.VehicleId == v.Id)
                .SumAsync(m => m.Cost);

            var totalCost = fuelCost + maintenanceCost;
            var costPerKm = v.Odometer > 0 ? Math.Round(totalCost / v.Odometer, 2) : 0;

            var revenue = await _db.Trips
                .Where(t => t.VehicleId == v.Id && t.Status == "Completed")
                .SumAsync(t => t.CargoWeight * 0.5);

            var roi = v.AcquisitionCost > 0
                ? Math.Round((revenue - totalCost) / v.AcquisitionCost * 100, 2)
                : 0;

            result.Add(new VehicleCostDto(v.Id, v.Name, v.LicensePlate,
                Math.Round(fuelCost, 2), Math.Round(maintenanceCost, 2), Math.Round(totalCost, 2),
                v.Odometer, costPerKm, v.AcquisitionCost, roi));
        }

        return Ok(result);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var vehicles = await _db.Vehicles.ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Vehicle,LicensePlate,Type,Status,Odometer,FuelCost,MaintenanceCost,TotalCost,CostPerKm");

        foreach (var v in vehicles)
        {
            var fuelCost = await _db.Expenses.Where(e => e.VehicleId == v.Id && e.Category == "Fuel").SumAsync(e => e.Cost);
            var maintCost = await _db.MaintenanceLogs.Where(m => m.VehicleId == v.Id).SumAsync(m => m.Cost);
            var total = fuelCost + maintCost;
            var cpk = v.Odometer > 0 ? Math.Round(total / v.Odometer, 2) : 0;
            sb.AppendLine($"{v.Name},{v.LicensePlate},{v.Type},{v.Status},{v.Odometer},{fuelCost:F2},{maintCost:F2},{total:F2},{cpk}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "fleet_report.csv");
    }
}
