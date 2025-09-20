using QL_Cong_Viec.Models;

namespace QL_Cong_Viec.Service
{
    public class FlightAggregatorService
    {
        private readonly FlightService _flightService;
        private readonly AmadeusService _amadeusService;
        private readonly WikiService _wikiService;

        public FlightAggregatorService(FlightService f, AmadeusService a, WikiService w)
        {
            _flightService = f;
            _amadeusService = a;
            _wikiService = w;
        }

        public async Task<List<FlightDto>> GetFlightsWithExtrasAsync()
        {
            var flights = await _flightService.GetFlightsAsync();

            foreach (var f in flights)
            {
                // lấy giá
                if (!string.IsNullOrEmpty(f.DepartureAirport) && !string.IsNullOrEmpty(f.ArrivalAirport))
                {
                    f.Price = await _amadeusService.GetPriceAsync(f.DepartureAirport, f.ArrivalAirport);
                }

                // lấy ảnh
                if (!string.IsNullOrEmpty(f.ArrivalAirport))
                {
                    f.ImageUrl = await _wikiService.GetImageUrlAsync(f.ArrivalAirport);
                }
            }

            return flights;
        }
    }
}
