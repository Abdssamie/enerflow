using Microsoft.AspNetCore.Mvc;
using Enerflow.API.Services;

namespace Enerflow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IDWSIMService _dwsimService;

    public HealthController(IDWSIMService dwsimService)
    {
        _dwsimService = dwsimService;
    }

    [HttpGet]
    public IActionResult Check()
    {
        try
        {
            var version = _dwsimService.GetDWSIMVersion();
            return Ok(new
            {
                Status = "Healthy",
                DWSIMVersion = version,
                Message = "Engine initialized successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Internal Sever Error",
                Error = ex.Message
            });
        }
    }
}
