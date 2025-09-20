using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Models;

namespace QL_Cong_Viec.Service
{
    public class FlightAggregatorService
    {
        private readonly IServiceBus _serviceBus;
        private readonly ILogger<FlightAggregatorService> _logger;

        public FlightAggregatorService(IServiceBus serviceBus, ILogger<FlightAggregatorService> logger)
        {
            _serviceBus = serviceBus;
            _logger = logger;
        }

        public async Task<List<FlightDto>> GetFlightsWithExtrasAsync(string from, string to)
        {
            try
            {
                _logger.LogInformation("Getting flights from {from} to {to}", from, to);

                // ✅ Sửa: Truyền parameters vào request
                var flightRequest = new ServiceRequest
                {
                    ServiceName = "FlightService",
                    Operation = "GetFlights",
                    SourceService = "FlightAggregator",
                    Parameters = new Dictionary<string, object>
                    {
                        { "from", from },
                        { "to", to }
                    }
                };

                var flights = await _serviceBus.SendRequestAsync<List<FlightDto>>(flightRequest);

                if (flights == null || !flights.Any())
                {
                    _logger.LogWarning("No flights found for {from} -> {to}", from, to);
                    return new List<FlightDto>();
                }

                _logger.LogInformation("Found {count} flights, enriching with extras...", flights.Count);

                // Process each flight
                var tasks = flights.Select(async flight =>
                {
                    // ✅ Sửa: Dùng DepartureIata và ArrivalIata thay vì DepartureAirport và ArrivalAirport
                    if (!string.IsNullOrEmpty(flight.DepartureIata) && !string.IsNullOrEmpty(flight.ArrivalIata))
                    {
                        try
                        {
                            var priceRequest = new ServiceRequest
                            {
                                ServiceName = "AmadeusService",
                                Operation = "GetPrice",
                                SourceService = "FlightAggregator",
                                Parameters = new Dictionary<string, object>
                                {
                                    { "depIata", flight.DepartureIata },
                                    { "arrIata", flight.ArrivalIata }
                                }
                            };

                            var priceString = await _serviceBus.SendRequestAsync<string>(priceRequest);

                            // ✅ Sửa: Parse price tốt hơn
                            if (!string.IsNullOrEmpty(priceString))
                            {
                                // Trích xuất số từ chuỗi price (bỏ chữ và ký tự đặc biệt)
                                var numbers = new string(priceString.Where(char.IsDigit).ToArray());
                                if (int.TryParse(numbers, out var price))
                                {
                                    flight.Price = price;
                                }
                                else
                                {
                                    _logger.LogWarning("Invalid price format: {price}", priceString);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get price for flight {dep}-{arr}",
                                flight.DepartureIata, flight.ArrivalIata);
                        }
                    }

                    // Get image through ESB
                    if (!string.IsNullOrEmpty(flight.ArrivalAirport))
                    {
                        try
                        {
                            var imageRequest = new ServiceRequest
                            {
                                ServiceName = "WikiService",
                                Operation = "GetImageUrl",
                                SourceService = "FlightAggregator",
                                Parameters = new Dictionary<string, object>
                                {
                                    { "keyword", flight.ArrivalAirport }
                                }
                            };

                            flight.ImageUrl = await _serviceBus.SendRequestAsync<string>(imageRequest);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get image for {airport}", flight.ArrivalAirport);
                        }
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogInformation("Successfully enriched {count} flights", flights.Count);
                return flights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFlightsWithExtrasAsync for {from} -> {to}", from, to);
                return new List<FlightDto>(); // Trả về list rỗng thay vì throw
            }
        }
    }
}