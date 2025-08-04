using Microsoft.AspNetCore.Mvc;
namespace TeleCasino.DiceGameApi.Controllers;
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Healthy");
    }
}