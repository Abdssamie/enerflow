using Enerflow.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Enerflow.API.Controllers;

[ApiController]
[Route("api/v1/catalogs")]
public class CatalogsController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogsController> _logger;

    public CatalogsController(ICatalogService catalogService, ILogger<CatalogsController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets available chemical compounds, optionally filtered by search term.
    /// </summary>
    /// <param name="search">Optional search term to filter by name, formula, CAS number, or category.</param>
    [HttpGet("compounds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCompounds([FromQuery] string? search = null)
    {
        var compounds = _catalogService.GetCompounds(search);

        _logger.LogDebug("Retrieved {Count} compounds with search term: {Search}",
            compounds.Count(), search ?? "(none)");

        return Ok(new
        {
            count = compounds.Count(),
            items = compounds
        });
    }

    /// <summary>
    /// Gets available thermodynamic property packages.
    /// </summary>
    [HttpGet("property_packages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPropertyPackages()
    {
        var packages = _catalogService.GetPropertyPackages();

        return Ok(new
        {
            count = packages.Count(),
            items = packages
        });
    }

    /// <summary>
    /// Gets available flash algorithms for phase equilibrium calculations.
    /// </summary>
    [HttpGet("flash_algorithms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetFlashAlgorithms()
    {
        var algorithms = _catalogService.GetFlashAlgorithms();

        return Ok(new
        {
            count = algorithms.Count(),
            items = algorithms
        });
    }

    /// <summary>
    /// Gets available unit operation types.
    /// </summary>
    [HttpGet("unit_ops")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetUnitOperations()
    {
        var unitOps = _catalogService.GetUnitOperations();

        return Ok(new
        {
            count = unitOps.Count(),
            items = unitOps
        });
    }

    /// <summary>
    /// Gets available systems of units.
    /// </summary>
    [HttpGet("unit_systems")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetUnitSystems()
    {
        var systems = Enum.GetValues<Enerflow.Domain.Enums.SystemOfUnits>()
            .Select(s => new
            {
                name = s.ToString(),
                description = s switch
                {
                    Enerflow.Domain.Enums.SystemOfUnits.SI => "International System of Units (K, Pa, kg/s)",
                    Enerflow.Domain.Enums.SystemOfUnits.CGS => "Centimeter-Gram-Second system",
                    Enerflow.Domain.Enums.SystemOfUnits.English => "English/Imperial units (°F, psi, lb/h)",
                    _ => "Unknown system"
                }
            });

        return Ok(new
        {
            count = systems.Count(),
            items = systems
        });
    }
}
