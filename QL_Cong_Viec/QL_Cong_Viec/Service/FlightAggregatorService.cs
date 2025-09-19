using Humanizer;
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

        public async Task<List<FlightDto>> GetFlightsWithExtrasAsync(string from,string to)
        {
            try
            {
                // Get flights through ESB
                var flightRequest = new ServiceRequest
                {
                    ServiceName = "FlightService",
                    Operation = "GetFlights",
                    SourceService = "FlightAggregator"
                };

                var flights = await _serviceBus.SendRequestAsync<List<FlightDto>>(flightRequest);

                // Process each flight
                var tasks = flights.Select(async flight =>
                {
                    // Get price through ESB
                    if (!string.IsNullOrEmpty(flight.DepartureAirport) && !string.IsNullOrEmpty(flight.ArrivalAirport))
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
                                    { "depIata", flight.DepartureAirport },
                                    { "arrIata", flight.ArrivalAirport }
                                }
                            };

                            var priceString = await _serviceBus.SendRequestAsync<string>(priceRequest);
                            if (int.TryParse(priceString, out var price))
                            {
                                flight.Price = price;
                            }
                            else
                            {
                                _logger.LogWarning("Invalid price format: {price}", priceString);
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get price for flight {dep}-{arr}",
                                flight.DepartureAirport, flight.ArrivalAirport);
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
                return flights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFlightsWithExtrasAsync");
                throw;
            }
        }
    }
}