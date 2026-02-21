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
    public async Task<IActionResult> GetDashboard([FromQuery] string? region, [FromQuery] string? type, [FromQuery] string? status)
    {
        var vq = _db.Vehicles.AsQueryable();
        if (!string.IsNullOrEmpty(region)) vq = vq.Where(v => v.Region == region);
        if (!string.IsNullOrEmpty(type)) vq = vq.Where(v => v.Type == type);
        if (!string.IsNullOrEmpty(status)) vq = vq.Where(v => v.Status == status);

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
        sb.AppendLine("Vehicle,LicensePlate,Type,Status,Odometer,FuelCost,MaintenanceCost,TotalCost,CostPerKm(₹)");

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

        QuestPDF.Settings.License = LicenseType.Community;
        var totalFleetCost = rows.Sum(r => r.Total);
        var maxCost = rows.Any() ? rows.Max(r => r.Total) : 0;
        var avgCpk = rows.Any(r => r.Odo > 0) ? rows.Where(r => r.Odo > 0).Average(r => r.Total / r.Odo) : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10));
                page.PageColor(Colors.White);

                // --- HEADER ---
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FleetFlow").FontSize(24).Bold().FontColor("#3b82f6"); // Primary blue
                            c.Item().Text("Executive Operational Report").FontSize(14).FontColor(Colors.Grey.Darken2);
                        });
                        row.AutoItem().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Date: {DateTime.Now:MMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().AlignRight().Text($"Generated By: Analytics System").FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // --- CONTENT ---
                page.Content().Column(content =>
                {
                    // --- KPI SUMMARY DASHBOARD ---
                    content.Item().PaddingBottom(20).Row(row =>
                    {
                        void KpiBlock(RowDescriptor r, string label, string value, string color)
                        {
                            r.RelativeItem().Background(color).Padding(15).Column(c =>
                            {
                                c.Item().Text(label).FontSize(11).FontColor(Colors.White).SemiBold();
                                c.Item().Text(value).FontSize(20).FontColor(Colors.White).Bold();
                            });
                            r.Spacing(15);
                        }

                        KpiBlock(row, "Total Fleet Cost", $"₹{totalFleetCost:N0}", "#3b82f6"); // Blue
                        KpiBlock(row, "Active Vehicles", $"{rows.Count(r => r.Status != "Retired")}", "#22c55e"); // Green
                        KpiBlock(row, "Avg Cost / km", $"₹{avgCpk:N2}", "#f59e0b"); // Amber
                        KpiBlock(row, "Total Odometer", $"{rows.Sum(r => r.Odo):N0} km", "#8b5cf6"); // Purple
                    });

                    // --- VISUALIZATION (Horizontal Bar Chart) ---
                    content.Item().PaddingBottom(20).Column(chartCol =>
                    {
                        chartCol.Item().PaddingBottom(10).Text("Cost Breakdown by Vehicle").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                        chartCol.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(barCol =>
                        {
                            foreach (var r in rows.OrderByDescending(x => x.Total).Take(8)) // Top 8
                            {
                                var pct = maxCost > 0 ? (float)(r.Total / maxCost) : 0;
                                barCol.Item().PaddingBottom(6).Row(row =>
                                {
                                    row.ConstantItem(120).AlignMiddle().Text(r.Name).FontSize(10).FontColor(Colors.Grey.Darken3).SemiBold();
                                    row.RelativeItem().AlignMiddle().Row(innerRow =>
                                    {
                                        var leftPct = Math.Max(pct, 0.001f); // Must be > 0
                                        var rightPct = Math.Max(1 - pct, 0.001f); // Must be > 0
                                        innerRow.RelativeItem(leftPct).Background("#3b82f6").Height(14); // Bar
                                        innerRow.RelativeItem(rightPct).Background(Colors.Transparent).Height(14); // Remainder
                                    });
                                    row.ConstantItem(70).AlignRight().AlignMiddle().Text($"₹{r.Total:N0}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                });
                            }
                        });
                    });

                    // --- DETAILED TABLE ---
                    content.Item().PaddingBottom(10).Text("Detailed Financial Ledger").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Vehicle
                            cols.RelativeColumn(1.5f); // Plate
                            cols.RelativeColumn(1); // Type
                            cols.RelativeColumn(1); // Status
                            cols.RelativeColumn(1.2f); // Odo
                            cols.RelativeColumn(1.5f); // Fuel
                            cols.RelativeColumn(1.5f); // Maint
                            cols.RelativeColumn(1.5f); // Total
                            cols.RelativeColumn(1); // /km
                        });

                        // Header
                        var headers = new[] { "Vehicle", "License Plate", "Type", "Status", "Odometer", "Fuel Cost", "Maint. Cost", "Total Cost", "₹/km" };
                        foreach (var h in headers)
                        {
                            table.Cell().Background("#1e293b").PaddingVertical(8).PaddingHorizontal(5).Text(h)
                                 .FontColor(Colors.White).Bold().FontSize(10).AlignLeft();
                        }

                        // Data rows
                        var isAlt = false;
                        foreach (var r in rows)
                        {
                            var bg = isAlt ? Colors.Grey.Lighten4 : Colors.White;
                            var padding = 6u;

                            table.Cell().Background(bg).Padding(padding).Text(r.Name).SemiBold();
                            table.Cell().Background(bg).Padding(padding).Text(r.Plate).FontColor(Colors.Grey.Darken2);
                            table.Cell().Background(bg).Padding(padding).Text(r.Type);
                            // Status Pill simulation
                            var statusColor = r.Status == "Available" ? Colors.Green.Medium : (r.Status == "InShop" ? Colors.Orange.Medium : Colors.Blue.Medium);
                            table.Cell().Background(bg).Padding(padding).Text(r.Status).FontColor(statusColor).Bold();

                            table.Cell().Background(bg).Padding(padding).AlignRight().Text($"{r.Odo:N0}");
                            table.Cell().Background(bg).Padding(padding).AlignRight().Text($"₹{r.Fuel:N2}");
                            table.Cell().Background(bg).Padding(padding).AlignRight().Text($"₹{r.Maint:N2}");
                            table.Cell().Background(bg).Padding(padding).AlignRight().Text($"₹{r.Total:N2}").SemiBold();
                            table.Cell().Background(bg).Padding(padding).AlignRight().Text($"₹{r.Cpk}");

                            isAlt = !isAlt;
                        }

                        // Footer Total Row
                        table.Cell().ColumnSpan(5).Background("#e2e8f0").Padding(8).AlignRight().Text("FLEET TOTAL:").Bold().FontSize(11);
                        table.Cell().Background("#e2e8f0").Padding(8).AlignRight().Text($"₹{rows.Sum(r => r.Fuel):N2}").Bold().FontSize(10);
                        table.Cell().Background("#e2e8f0").Padding(8).AlignRight().Text($"₹{rows.Sum(r => r.Maint):N2}").Bold().FontSize(10);
                        table.Cell().Background("#e2e8f0").Padding(8).AlignRight().Text($"₹{totalFleetCost:N2}").Bold().FontSize(11);
                        table.Cell().Background("#e2e8f0").Padding(8).AlignRight().Text($"₹{avgCpk:N2}").Bold().FontSize(10);
                    });
                });

                // --- FOOTER ---
                page.Footer().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Confidential Internal Document").FontSize(9).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        });

        var stream = new MemoryStream();
        document.GeneratePdf(stream);
        stream.Position = 0;

        return File(stream, "application/pdf", "fleet_report.pdf");
    }
}
