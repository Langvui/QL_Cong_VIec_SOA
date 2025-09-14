using System.Text.Json;

namespace QL_Cong_Viec.Service
{
    public class WikiService
    {
        private readonly HttpClient _http;

        public WikiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string?> GetImageUrlAsync(string keyword)
        {
            // B1: gọi search để tìm tiêu đề gần đúng
            string searchUrl = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(keyword)}&format=json";
            var searchRequest = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            searchRequest.Headers.UserAgent.ParseAdd("MyFlightApp/1.0 (contact: youremail@example.com)");

            var searchResp = await _http.SendAsync(searchRequest);
            if (!searchResp.IsSuccessStatusCode) return null;

            var searchJson = await searchResp.Content.ReadAsStringAsync();
            using var searchDoc = JsonDocument.Parse(searchJson);

            string? bestTitle = null;
            if (searchDoc.RootElement.TryGetProperty("query", out var query) &&
                query.TryGetProperty("search", out var results) &&
                results.ValueKind == JsonValueKind.Array &&
                results.GetArrayLength() > 0)
            {
                bestTitle = results[0].GetProperty("title").GetString();
            }

            if (string.IsNullOrEmpty(bestTitle))
                return null;

            // B2: gọi API pageimages để lấy thumbnail
            string url = $"https://en.wikipedia.org/w/api.php?action=query&titles={Uri.EscapeDataString(bestTitle)}&prop=pageimages&format=json&pithumbsize=500";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("MyFlightApp/1.0 (contact: youremail@example.com)");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("query", out var q2) &&
                q2.TryGetProperty("pages", out var pages))
            {
                foreach (var page in pages.EnumerateObject())
                {
                    if (page.Value.TryGetProperty("thumbnail", out var thumb) &&
                        thumb.TryGetProperty("source", out var source))
                    {
                        return source.GetString();
                    }
                }
            }

            return null;
        }
    }
}
