using QL_Cong_Viec.Models;
using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class FlightService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public FlightService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["AviationStackApiKey:APIKey"] ?? "";
        }

        public async Task<List<FlightDto>> GetFlightsAsync()
        {
            string url = $"http://api.aviationstack.com/v1/flights?access_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);

            var flights = new List<FlightDto>();
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var data in dataArray.EnumerateArray())
                {
                    var depAirport = data.TryGetProperty("departure", out var dep) && dep.TryGetProperty("airport", out var depAirportProp)
                        ? depAirportProp.GetString()
                        : null;

                    var arrAirport = data.TryGetProperty("arrival", out var arr) && arr.TryGetProperty("airport", out var arrAirportProp)
                        ? arrAirportProp.GetString()
                        : null;

                    // Tách city từ airport
                    string? depCity = null;
                    if (!string.IsNullOrEmpty(depAirport))
                    {
                        depCity = depAirport.Contains("(")
                            ? depAirport.Split('(')[0].Trim()
                            : depAirport;
                    }

                    string? arrCity = null;
                    if (!string.IsNullOrEmpty(arrAirport))
                    {
                        arrCity = arrAirport.Contains("(")
                            ? arrAirport.Split('(')[0].Trim()
                            : arrAirport;
                    }

                    var flight = new FlightDto
                    {
                        FlightDate = data.TryGetProperty("flight_date", out var flightDate) ? flightDate.GetString() : null,
                        FlightStatus = data.TryGetProperty("flight_status", out var flightStatus) ? flightStatus.GetString() : null,
                        DepartureAirport = depAirport,
                        ArrivalAirport = arrAirport,
                        Airline = data.TryGetProperty("airline", out var airline) && airline.TryGetProperty("name", out var airlineName) ? airlineName.GetString() : null,
                        ArrivalCity = arrCity
                    };

                    flights.Add(flight);
                }
            }

            return flights;
        }

    }
}
