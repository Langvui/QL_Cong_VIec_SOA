using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using NHibernate.Mapping;
using QL_Cong_Viec.ESB.Services;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class FlightsController : Controller
{
    private readonly FlightAggregatorService _aggregator;
    private readonly HealthCheckService _healthCheckService;

    public FlightsController(FlightAggregatorService aggregator, HealthCheckService healthCheckService)
    {
        _aggregator = aggregator;
        _healthCheckService = healthCheckService;
    }

    public IActionResult Index(List<FlightDto>? flights = null)
    {
        return View(flights);
    }

    [HttpGet("Search")]
    public async Task<IActionResult> Search(FlightSearchDto flightSearch)
    {
        if (string.IsNullOrEmpty(flightSearch.From) || string.IsNullOrEmpty(flightSearch.To))
            return BadRequest("Thiếu tham số from hoặc to");

        var flights = await _aggregator.GetFlightsWithExtrasAsync(flightSearch.From,flightSearch.To);
        return View("Index", flights);
    }

    public async Task<IActionResult> Health()
    {
        var healthChecks = await _healthCheckService.GetAllServiceHealthAsync();
        return Json(healthChecks);
    }
}
