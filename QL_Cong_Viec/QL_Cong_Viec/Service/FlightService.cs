using QL_Cong_Viec.Models;
using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class FlightService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // ✅ Thay đổi: Dùng IHttpClientFactory thay vì HttpClient
        public FlightService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = config["AviationStackApiKey:APIKey"] ?? "";
        }

        public async Task<List<FlightDto>> GetFlightsAsync()
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


                        // Departure
                        DepartureAirport = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("airport", out var depAirportProp) ? depAirportProp.GetString() : null,
                        DepartureIata = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("iata", out var depIata) ? depIata.GetString() : null,
                        DepartureScheduled = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("scheduled", out var depScheduled) && depScheduled.ValueKind == JsonValueKind.String
                            ? DateTime.TryParse(depScheduled.GetString(), out var depSch) ? depSch : (DateTime?)null : null,
                        DepartureActual = dep.ValueKind != JsonValueKind.Undefined && dep.TryGetProperty("actual", out var depActual) && depActual.ValueKind == JsonValueKind.String
                            ? DateTime.TryParse(depActual.GetString(), out var depAct) ? depAct : (DateTime?)null : null,

                        // Arrival
                        ArrivalAirport = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("airport", out var arrAirportProp) ? arrAirportProp.GetString() : null,
                        ArrivalIata = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("iata", out var arrIata) ? arrIata.GetString() : null,
                        ArrivalScheduled = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("scheduled", out var arrScheduled) && arrScheduled.ValueKind == JsonValueKind.String
                            ? DateTime.TryParse(arrScheduled.GetString(), out var arrSch) ? arrSch : (DateTime?)null : null,
                        ArrivalActual = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("actual", out var arrActual) && arrActual.ValueKind == JsonValueKind.String
                            ? DateTime.TryParse(arrActual.GetString(), out var arrAct) ? arrAct : (DateTime?)null : null,
                        ArrivalDelay = arr.ValueKind != JsonValueKind.Undefined && arr.TryGetProperty("delay", out var arrDelay) && arrDelay.ValueKind == JsonValueKind.Number
                            ? arrDelay.GetInt32() : (int?)null,

                        // Airline
                        AirlineName = airlineObj.ValueKind != JsonValueKind.Undefined && airlineObj.TryGetProperty("name", out var airlineName) ? airlineName.GetString() : null,
                        AirlineIata = airlineObj.ValueKind != JsonValueKind.Undefined && airlineObj.TryGetProperty("iata", out var airlineIata) ? airlineIata.GetString() : null,

                        // Flight
                        FlightNumber = flightObj.ValueKind != JsonValueKind.Undefined && flightObj.TryGetProperty("number", out var flightNum) ? flightNum.GetString() : null,
                        FlightIata = flightObj.ValueKind != JsonValueKind.Undefined && flightObj.TryGetProperty("iata", out var flightIata) ? flightIata.GetString() : null

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
