using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetFlow.API.Data;
using FleetFlow.API.DTOs;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FleetFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public AnalyticsController(FleetFlowDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string? region)
    {
        var vq = _db.Vehicles.AsQueryable();
        if (!string.IsNullOrEmpty(region)) vq = vq.Where(v => v.Region == region);

        var totalVehicles = await vq.CountAsync();
        var activeFleet = await vq.CountAsync(v => v.Status == "OnTrip");
        var maintenanceAlerts = await vq.CountAsync(v => v.Status == "InShop");
        var totalDrivers = await _db.Drivers.CountAsync();
        var totalTrips = await _db.Trips.CountAsync(t => t.Status == "Completed");
        var pendingCargo = await _db.Trips.CountAsync(t => t.Status == "Draft");

        var utilization = totalVehicles > 0
            ? Math.Round((double)(activeFleet + maintenanceAlerts) / totalVehicles * 100, 1)
            : 0;

        var totalRevenue = await _db.Trips
            .Where(t => t.Status == "Completed")
            .SumAsync(t => t.CargoWeight * 0.5);

        return Ok(new DashboardDto(activeFleet, maintenanceAlerts, utilization, pendingCargo,
            totalVehicles, totalDrivers, totalTrips, Math.Round(totalRevenue, 2)));
    }

    [HttpGet("vehicle-breakdown")]
    public async Task<IActionResult> GetVehicleBreakdown()
    {
        var byType = await _db.Vehicles
            .GroupBy(v => v.Type)
            .Select(g => new { name = g.Key, value = g.Count() })
            .ToListAsync();

        var byStatus = await _db.Vehicles
            .GroupBy(v => v.Status)
            .Select(g => new { name = g.Key, value = g.Count() })
            .ToListAsync();

        return Ok(new { byType, byStatus });
    }

    [HttpGet("vehicle-costs")]
    [Authorize(Roles = "Manager,Analyst")]
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

            // Fuel efficiency (km/L)
            var totalLiters = await _db.Expenses
                .Where(e => e.VehicleId == v.Id && e.Category == "Fuel" && e.Liters.HasValue)
                .SumAsync(e => e.Liters ?? 0);
            var fuelEfficiency = totalLiters > 0 ? Math.Round(v.Odometer / totalLiters, 1) : 0;

            result.Add(new VehicleCostDto(v.Id, v.Name, v.LicensePlate,
                Math.Round(fuelCost, 2), Math.Round(maintenanceCost, 2), Math.Round(totalCost, 2),
                v.Odometer, costPerKm, v.AcquisitionCost, roi, Math.Round(totalLiters, 1), fuelEfficiency));
        }

        return Ok(result);
    }

    [HttpGet("export/csv")]
    [Authorize(Roles = "Manager,Analyst")]
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

    [HttpGet("export/pdf")]
    [Authorize(Roles = "Manager,Analyst")]
    public async Task<IActionResult> ExportPdf()
    {
        var vehicles = await _db.Vehicles.ToListAsync();
        var rows = new List<(string Name, string Plate, string Type, string Status, double Odo, double Fuel, double Maint, double Total, double Cpk)>();

        foreach (var v in vehicles)
        {
            var fuelCost = await _db.Expenses.Where(e => e.VehicleId == v.Id && e.Category == "Fuel").SumAsync(e => e.Cost);
            var maintCost = await _db.MaintenanceLogs.Where(m => m.VehicleId == v.Id).SumAsync(m => m.Cost);
            var total = fuelCost + maintCost;
            var cpk = v.Odometer > 0 ? Math.Round(total / v.Odometer, 2) : 0;
            rows.Add((v.Name, v.LicensePlate, v.Type, v.Status, v.Odometer, Math.Round(fuelCost, 2), Math.Round(maintCost, 2), Math.Round(total, 2), cpk));
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FleetFlow — Fleet Report").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2); // Vehicle
                        cols.RelativeColumn(1.5f); // Plate
                        cols.RelativeColumn(1); // Type
                        cols.RelativeColumn(1); // Status
                        cols.RelativeColumn(1.5f); // Odometer
                        cols.RelativeColumn(1.5f); // Fuel
                        cols.RelativeColumn(1.5f); // Maint
                        cols.RelativeColumn(1.5f); // Total
                        cols.RelativeColumn(1); // $/km
                    });

                    // Header
                    var headers = new[] { "Vehicle", "Plate", "Type", "Status", "Odometer", "Fuel Cost", "Maint. Cost", "Total Cost", "$/km" };
                    foreach (var h in headers)
                        table.Cell().Background(Colors.Blue.Medium).Padding(5).Text(h).FontColor(Colors.White).Bold().FontSize(9);

                    // Data rows
                    var isAlt = false;
                    foreach (var r in rows)
                    {
                        var bg = isAlt ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bg).Padding(5).Text(r.Name);
                        table.Cell().Background(bg).Padding(5).Text(r.Plate);
                        table.Cell().Background(bg).Padding(5).Text(r.Type);
                        table.Cell().Background(bg).Padding(5).Text(r.Status);
                        table.Cell().Background(bg).Padding(5).AlignRight().Text($"{r.Odo:N0} km");
                        table.Cell().Background(bg).Padding(5).AlignRight().Text($"${r.Fuel:N2}");
                        table.Cell().Background(bg).Padding(5).AlignRight().Text($"${r.Maint:N2}");
                        table.Cell().Background(bg).Padding(5).AlignRight().Text($"${r.Total:N2}");
                        table.Cell().Background(bg).Padding(5).AlignRight().Text($"${r.Cpk}");
                        isAlt = !isAlt;
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;

        return File(stream, "application/pdf", "fleet_report.pdf");
    }
}
