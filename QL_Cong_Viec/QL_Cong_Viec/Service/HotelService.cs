using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QL_Cong_Viec.ViewModels;

namespace QL_Cong_Viec.Service
{
    public class HotelService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HotelService> _logger;
        private readonly IConfiguration _configuration;

        public HotelService(HttpClient httpClient, ILogger<HotelService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Lấy API key từ configuration
            var apiKey = _configuration["RapidAPI:BookingApiKey"];
            var apiHost = _configuration["RapidAPI:BookingHost"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiHost))
            {
                _logger.LogError("RapidAPI configuration is missing in appsettings.json");
                throw new InvalidOperationException("RapidAPI configuration is missing");
            }

            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("x-rapidapi-host", apiHost);
        }

        public async Task<List<HotelResultViewModel>> SearchHotelsAsync(string location, DateTime checkIn, DateTime checkOut, int adults, int rooms)
        {
            var results = new List<HotelResultViewModel>();
            try
            {
                // Bước 1: Gọi API lấy danh sách địa điểm
                var destUrl = $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={Uri.EscapeDataString(location)}";
                _logger.LogInformation("Calling destination API: {url}", destUrl);

                var destResponse = await _httpClient.GetAsync(destUrl);
                var destJson = await destResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Destination API status: {status}", destResponse.StatusCode);
                _logger.LogInformation("Destination API response: {json}", destJson.Length > 500 ? destJson.Substring(0, 500) + "..." : destJson);

                if (!destResponse.IsSuccessStatusCode)
                {
                    return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = $"API Error (Destination): {destResponse.StatusCode}", Price = "N/A" }
                    };
                }

                var destRoot = JsonDocument.Parse(destJson).RootElement;

                if (!destRoot.TryGetProperty("data", out var dataArray) || dataArray.ValueKind != JsonValueKind.Array || dataArray.GetArrayLength() == 0)
                {
                    _logger.LogWarning("No destinations found for {location}", location);
                    return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = "Không tìm thấy địa điểm", Price = "N/A" }
                    };
                }

                // Chọn destination đầu tiên (không chỉ city)
                var firstItem = dataArray.EnumerateArray().FirstOrDefault();

                if (firstItem.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("No destination found in results for {location}", location);
                    return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = "Không tìm thấy địa điểm phù hợp", Price = "N/A" }
                    };
                }

                var destId = firstItem.GetProperty("dest_id").GetString();
                var searchType = firstItem.GetProperty("search_type").GetString();
                _logger.LogInformation("Using destination {destId} with searchType {searchType}", destId, searchType);

                // Bước 2: Gọi API tìm khách sạn với cấu trúc URL mới
                var hotelUrl =
                    $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels" +
                    $"?dest_id={destId}" +
                    $"&search_type={searchType}" +
                    $"&arrival_date={checkIn:yyyy-MM-dd}" +
                    $"&departure_date={checkOut:yyyy-MM-dd}" +
                    $"&adults={adults}" +
                    $"&room_qty={rooms}" +
                    $"&page_number=1" +
                    $"&units=metric" +
                    $"&temperature_unit=c" +
                    $"&languagecode=vi" +
                    $"&currency_code=VND";

                _logger.LogInformation("Calling hotel API: {url}", hotelUrl);

                var hotelResponse = await _httpClient.GetAsync(hotelUrl);
                var hotelJson = await hotelResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Hotels API status: {status}", hotelResponse.StatusCode);
                _logger.LogInformation("Hotels API response preview: {json}", hotelJson.Length > 1000 ? hotelJson.Substring(0, 1000) + "..." : hotelJson);

                if (!hotelResponse.IsSuccessStatusCode)
                {
                    return new List<HotelResultViewModel>
                    {
                        new HotelResultViewModel { Name = $"API Error (Hotels): {hotelResponse.StatusCode}", Price = "N/A" }
                    };
                }

                var root = JsonDocument.Parse(hotelJson).RootElement;

                // DEBUG: In ra tất cả properties của root
                _logger.LogInformation("Root element properties:");
                foreach (var prop in root.EnumerateObject())
                {
                    _logger.LogInformation("Property: {name}, Type: {type}", prop.Name, prop.Value.ValueKind);

                    // Nếu là array, in số lượng items
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogInformation("Array {name} has {count} items", prop.Name, prop.Value.GetArrayLength());
                    }
                }

                // Thử parse theo các cấu trúc khác nhau
                bool parsed = false;

                // Thử cấu trúc 1: root.data
                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                    {
                        _logger.LogInformation("Found 'data' array with {count} items", dataElement.GetArrayLength());
                        ParseHotelsFromArray(dataElement, results);
                        parsed = true;
                    }
                    // Thử data.hotels
                    else if (dataElement.TryGetProperty("hotels", out var hotelsInData) &&
                             hotelsInData.ValueKind == JsonValueKind.Array && hotelsInData.GetArrayLength() > 0)
                    {
                        _logger.LogInformation("Found 'data.hotels' array with {count} items", hotelsInData.GetArrayLength());
                        ParseHotelsFromArray(hotelsInData, results);
                        parsed = true;
                    }
                    // Thử data.result
                    else if (dataElement.TryGetProperty("result", out var resultInData) &&
                             resultInData.ValueKind == JsonValueKind.Array && resultInData.GetArrayLength() > 0)
                    {
                        _logger.LogInformation("Found 'data.result' array with {count} items", resultInData.GetArrayLength());
                        ParseHotelsFromArray(resultInData, results);
                        parsed = true;
                    }
                }

                // Thử cấu trúc 2: root.result
                if (!parsed && root.TryGetProperty("result", out var resultElement) &&
                    resultElement.ValueKind == JsonValueKind.Array && resultElement.GetArrayLength() > 0)
                {
                    _logger.LogInformation("Found 'result' array with {count} items", resultElement.GetArrayLength());
                    ParseHotelsFromArray(resultElement, results);
                    parsed = true;
                }

                // Thử cấu trúc 3: root.hotels
                if (!parsed && root.TryGetProperty("hotels", out var hotelsElement) &&
                    hotelsElement.ValueKind == JsonValueKind.Array && hotelsElement.GetArrayLength() > 0)
                {
                    _logger.LogInformation("Found 'hotels' array with {count} items", hotelsElement.GetArrayLength());
                    ParseHotelsFromArray(hotelsElement, results);
                    parsed = true;
                }

                if (!parsed)
                {
                    _logger.LogWarning("No hotels array found in response for {destId}. Available properties: {props}",
                        destId, string.Join(", ", root.EnumerateObject().Select(p => $"{p.Name}({p.Value.ValueKind})")));
                }

                _logger.LogInformation("Parsed {count} hotels", results.Count);

                // Nếu vẫn không có kết quả, thêm một item thông báo
                if (results.Count == 0)
                {
                    results.Add(new HotelResultViewModel
                    {
                        Name = $"Không tìm thấy khách sạn cho {location}",
                        Price = "N/A",
                        Address = "API trả về thành công nhưng không có dữ liệu khách sạn",
                        Image = ""
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in SearchHotelsAsync");
                results.Add(new HotelResultViewModel
                {
                    Name = "Lỗi khi tìm kiếm khách sạn",
                    Price = "N/A",
                    Address = ex.Message,
                    Image = ""
                });
            }

            return results;
        }

        private void ParseHotelsFromArray(JsonElement hotelsArray, List<HotelResultViewModel> results)
        {
            foreach (var h in hotelsArray.EnumerateArray())
            {
                try
                {
                    _logger.LogDebug("Parsing hotel item with properties: {props}",
                        string.Join(", ", h.EnumerateObject().Select(p => p.Name)));

                    // Thử parse theo cách cũ (với property wrapper)
                    if (h.TryGetProperty("property", out var property))
                    {
                        ParseHotelFromProperty(property, results);
                    }
                    // Thử parse trực tiếp
                    else
                    {
                        ParseHotelDirectly(h, results);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse hotel item");
                }
            }
        }

        private void ParseHotelFromProperty(JsonElement property, List<HotelResultViewModel> results)
        {
            string price = "N/A";
            string imageUrl = "";
            string name = "Unknown";
            string address = "N/A";
            double latitude = 0;
            double longitude = 0;

            // Parse tên khách sạn
            if (property.TryGetProperty("name", out var nameElement))
                name = nameElement.GetString() ?? "Unknown";

            // Parse hình ảnh
            if (property.TryGetProperty("mainPhotoUrl", out var imageUrlElement))
                imageUrl = imageUrlElement.GetString() ?? "";
            else if (property.TryGetProperty("photoUrls", out var photoUrlsElement) &&
                     photoUrlsElement.ValueKind == JsonValueKind.Array && photoUrlsElement.GetArrayLength() > 0)
                imageUrl = photoUrlsElement[0].GetString() ?? "";

            // Parse giá
            if (property.TryGetProperty("priceBreakdown", out var priceBreakdown))
            {
                if (priceBreakdown.TryGetProperty("grossPrice", out var grossPrice) &&
                    grossPrice.TryGetProperty("value", out var priceValue) &&
                    grossPrice.TryGetProperty("currency", out var currencyCode))
                {
                    price = $"{priceValue.GetDouble():N0} {currencyCode.GetString()}";
                }
                else if (priceBreakdown.TryGetProperty("strikethroughPrice", out var strikePrice) &&
                         strikePrice.TryGetProperty("value", out var strikePriceValue) &&
                         strikePrice.TryGetProperty("currency", out var strikeCurrency))
                {
                    price = $"{strikePriceValue.GetDouble():N0} {strikeCurrency.GetString()}";
                }
            }

            // Parse tọa độ
            if (property.TryGetProperty("latitude", out var latElement))
                latitude = latElement.GetDouble();
            if (property.TryGetProperty("longitude", out var lngElement))
                longitude = lngElement.GetDouble();

            // Parse địa chỉ
            if (property.TryGetProperty("wishlistName", out var wishlistElement))
                address = wishlistElement.GetString() ?? "N/A";

            results.Add(new HotelResultViewModel
            {
                Name = name,
                Image = imageUrl,
                Price = price,
                Address = address,
                Latitude = latitude,
                Longitude = longitude
            });
        }

        private void ParseHotelDirectly(JsonElement hotel, List<HotelResultViewModel> results)
        {
            string price = "N/A";
            string imageUrl = "";
            string name = "Unknown";
            string address = "N/A";
            double latitude = 0;
            double longitude = 0;

            // Thử các tên property khác nhau cho tên khách sạn
            if (hotel.TryGetProperty("hotel_name", out var hotelNameElement))
                name = hotelNameElement.GetString() ?? "Unknown";
            else if (hotel.TryGetProperty("name", out var nameElement))
                name = nameElement.GetString() ?? "Unknown";
            else if (hotel.TryGetProperty("title", out var titleElement))
                name = titleElement.GetString() ?? "Unknown";

            // Parse giá
            if (hotel.TryGetProperty("price", out var priceElement))
                price = priceElement.GetString() ?? "N/A";
            else if (hotel.TryGetProperty("min_total_price", out var minPriceElement))
                price = $"{minPriceElement.GetDouble():N0} VND";

            // Parse hình ảnh
            if (hotel.TryGetProperty("image", out var imageElement))
                imageUrl = imageElement.GetString() ?? "";
            else if (hotel.TryGetProperty("photo_url", out var photoElement))
                imageUrl = photoElement.GetString() ?? "";
            else if (hotel.TryGetProperty("main_photo_url", out var mainPhotoElement))
                imageUrl = mainPhotoElement.GetString() ?? "";

            // Parse tọa độ
            if (hotel.TryGetProperty("latitude", out var latElement))
                latitude = latElement.GetDouble();
            if (hotel.TryGetProperty("longitude", out var lngElement))
                longitude = lngElement.GetDouble();

            // Parse địa chỉ
            if (hotel.TryGetProperty("address", out var addressElement))
                address = addressElement.GetString() ?? "N/A";

            results.Add(new HotelResultViewModel
            {
                Name = name,
                Image = imageUrl,
                Price = price,
                Address = address,
                Latitude = latitude,
                Longitude = longitude
            });
        }
    }
}