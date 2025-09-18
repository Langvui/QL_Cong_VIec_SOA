using Humanizer;
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

        public async Task<List<FlightDto>> GetFlightsWithExtrasAsync(string from, string to, string? date = null)
        {
            var flights = await _flightService.GetFlightsAsync(from, to, date);

            foreach (var f in flights)
            {
                if (!string.IsNullOrEmpty(f.DepartureAirport) && !string.IsNullOrEmpty(f.ArrivalAirport))
                {
                    var priceStr = await _amadeusService.GetPriceAsync(f.DepartureAirport, f.ArrivalAirport);

                    int price;
                    if (!int.TryParse(priceStr, out price) || price <= 0)
                    {
                        price = 1_000_000; // fallback mặc định
                    }

                    f.Price = price;
                }
                else
                {
                    f.Price = 1_000_000; // fallback nếu dữ liệu thiếu
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
