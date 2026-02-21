using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FleetFlow.API.Data;

namespace FleetFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager")]
public class AuditController : ControllerBase
{
    private readonly FleetFlowDbContext _db;
    public AuditController(FleetFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? entityType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(a => a.EntityType == entityType);

        var total = await q.CountAsync();
        var logs = await q.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.UserId, a.UserName, a.Action, a.EntityType, a.EntityId, a.Details, a.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }
}
