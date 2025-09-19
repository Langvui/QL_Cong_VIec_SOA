using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NHibernate.Mapping;
using QL_Cong_Viec.Models;
using QL_Cong_Viec.Service;

public class FlightsController : Controller
{
    private readonly FlightAggregatorService _aggregator;
    private readonly IMemoryCache _cache;

    public FlightsController(FlightAggregatorService aggregator, IMemoryCache cache)
    {
        _aggregator = aggregator;
        _cache = cache;
    }

    public IActionResult Index(List<FlightDto>? flights = null)
    {
        return View(flights);
    }

  
    [HttpGet]
    public async Task<IActionResult> Search(string tripType, string from, string to, string depart, string? @return, string passengers)

    {
        string cacheKey = $"flights_{from}_{to}";
        List<FlightDto> flights;

        if (!_cache.TryGetValue(cacheKey, out flights))
        {
            // Lấy flights VÀ set price cùng lúc
            flights = await _aggregator.GetFlightsWithExtrasAsync(from, to);
            // Cache AFTER setting price
            _cache.Set(cacheKey, flights, TimeSpan.FromMinutes(5));
        }

        HttpContext.Session.SetString("lastCacheKey", cacheKey);
        ViewData["to"] = to;
        ViewData["from"] = from;
        return View("Index", flights);
    }

    public IActionResult Details(string id)
    {
        var cacheKey = HttpContext.Session.GetString("lastCacheKey");
        if (!string.IsNullOrEmpty(cacheKey) && _cache.TryGetValue(cacheKey, out List<FlightDto>? flights))
        {
            var flight = flights?.FirstOrDefault(f => f.FlightNumber == id);
            if (flight != null)
            {
                return View(flight);
            }
        }

        return NotFound();
    }
}
