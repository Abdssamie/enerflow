using Microsoft.AspNetCore.Mvc;
using Enerflow.API.Services;

namespace Enerflow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowsheetController : ControllerBase
{
    private readonly IFlowsheetService _flowsheetService;
    private readonly IWebHostEnvironment _env;

    public FlowsheetController(IFlowsheetService flowsheetService, IWebHostEnvironment env)
    {
        _flowsheetService = flowsheetService;
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadAndLoad(IFormFile file)
    {
        // TODO: Temporary file is never cleaned up — resource leak.
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.FileName.EndsWith(".dwxmz", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .dwxmz files are supported.");

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dwxmz");
        
        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        try
        {
            var id = _flowsheetService.LoadFlowsheet(tempPath);
            return Ok(new { FlowsheetId = id, FileName = file.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Failed to load flowsheet.", Error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult ListLoaded()
    {
        return Ok(_flowsheetService.GetLoadedFlowsheetIds());
    }

    [HttpDelete("{id}")]
    public IActionResult Unload(Guid id)
    {
        try
        {
            _flowsheetService.UnloadFlowsheet(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }
}
