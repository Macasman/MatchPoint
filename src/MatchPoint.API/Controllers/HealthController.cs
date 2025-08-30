using Microsoft.AspNetCore.Mvc;
using MatchPoint.Infrastructure.Persistence;
using System.Data;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly SqlDbContext _db;
    public HealthController(SqlDbContext db) => _db = db;

    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        var result = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return Ok(new { ok = result == 1 });
    }
}
