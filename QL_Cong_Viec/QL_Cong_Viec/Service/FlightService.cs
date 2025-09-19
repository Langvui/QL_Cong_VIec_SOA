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

        public async Task<List<FlightDto>> GetFlightsAsync(string from, string to, string? date = null)
        {
            string url = $"http://api.aviationstack.com/v1/flights?access_key={_apiKey}" +
                         $"&dep_iata={from}&arr_iata={to}";

            

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"API call failed. Status: {response.StatusCode}. Details: {error}"
                );
            }

            var json = await response.Content.ReadAsStringAsync();

            var flights = new List<FlightDto>();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var data in dataArray.EnumerateArray())
                {
                    var dep = data.TryGetProperty("departure", out var depJson) ? depJson : default;
                    var arr = data.TryGetProperty("arrival", out var arrJson) ? arrJson : default;
                    var airlineObj = data.TryGetProperty("airline", out var airlineJson) ? airlineJson : default;
                    var flightObj = data.TryGetProperty("flight", out var flightJson) ? flightJson : default;

                    var flight = new FlightDto
                    {
                        FlightDate = data.TryGetProperty("flight_date", out var flightDate) ? flightDate.GetString() : null,
                        FlightStatus = data.TryGetProperty("flight_status", out var flightStatus) ? flightStatus.GetString() : null,

                        // Departure
                        DepartureAirport = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("airport", out var depAirportProp) ? depAirportProp.GetString() : null,
                        DepartureIata = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("iata", out var depIata) ? depIata.GetString() : null,
                        DepartureScheduled = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("scheduled", out var depScheduled) && depScheduled.ValueKind == JsonValueKind.String
    ? DateTime.TryParse(depScheduled.GetString(), out var depSch) ? depSch : (DateTime?)null
    : null,

                        DepartureActual = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("actual", out var depActual) && depActual.ValueKind == JsonValueKind.String
    ? DateTime.TryParse(depActual.GetString(), out var depAct) ? depAct : (DateTime?)null
    : null,


                        // Arrival
                        ArrivalAirport = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("airport", out var arrAirportProp) ? arrAirportProp.GetString() : null,
                        ArrivalIata = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("iata", out var arrIata) ? arrIata.GetString() : null,
                        ArrivalScheduled = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("scheduled", out var arrScheduled) && arrScheduled.ValueKind == JsonValueKind.String
    ? DateTime.TryParse(arrScheduled.GetString(), out var arrSch) ? arrSch : (DateTime?)null
    : null,

                        ArrivalActual = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("actual", out var arrActual) && arrActual.ValueKind == JsonValueKind.String
    ? DateTime.TryParse(arrActual.GetString(), out var arrAct) ? arrAct : (DateTime?)null
    : null,
                        ArrivalDelay = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("delay", out var arrDelay) && arrDelay.ValueKind == JsonValueKind.Number
                                            ? arrDelay.GetInt32()
                                            : (int?)null,

                        // Airline
                        AirlineName = airlineObj.ValueKind != JsonValueKind.Undefined && airlineObj.TryGetProperty("name", out var airlineName) ? airlineName.GetString() : null,
                        AirlineIata = airlineObj.ValueKind != JsonValueKind.Undefined && airlineObj.TryGetProperty("iata", out var airlineIata) ? airlineIata.GetString() : null,

                        // Flight
                        FlightNumber = flightObj.ValueKind != JsonValueKind.Undefined && flightObj.TryGetProperty("number", out var flightNum) ? flightNum.GetString() : null,
                        FlightIata = flightObj.ValueKind != JsonValueKind.Undefined && flightObj.TryGetProperty("iata", out var flightIata) ? flightIata.GetString() : null
                    };

                    flights.Add(flight);
                }
            }

            return flights;
        }

    }
}
