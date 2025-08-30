using Microsoft.AspNetCore.Mvc;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("ping")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "pong", at = DateTime.UtcNow });
}
