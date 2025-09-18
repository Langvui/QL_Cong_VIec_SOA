using System.Net.Http.Headers;
using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class AmadeusService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string? _accessToken;

        public AmadeusService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _clientId = config["Amadeus:ClientId"] ?? "";
            _clientSecret = config["Amadeus:ClientSecret"] ?? "";
        }

        private async Task AuthenticateAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken)) return;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            });

            var response = await _httpClient.PostAsync("https://test.api.amadeus.com/v1/security/oauth2/token", content);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<string?> GetPriceAsync(string depIata, string arrIata)
        {
            await AuthenticateAsync();

            var url = $"https://test.api.amadeus.com/v2/shopping/flight-offers?originLocationCode={depIata}&destinationLocationCode={arrIata}&departureDate=2025-09-20&adults=1&max=1";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                return data[0].GetProperty("price").GetProperty("total").GetString() + " USD";
            }

            return null;
        }
    }
}
