using Microsoft.AspNetCore.Mvc;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.Controllers
{
    public class FlightsController : Controller
    {
        private readonly FlightAggregatorService _aggregator;



        public FlightsController(FlightAggregatorService aggregator)
        {
            _aggregator = aggregator;
        }
        public async Task<IActionResult> Index()
        {
            var flights = await _aggregator.GetFlightsWithExtrasAsync();

            return View(flights);
        }

    }
}
