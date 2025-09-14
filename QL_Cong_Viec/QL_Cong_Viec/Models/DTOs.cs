namespace QL_Cong_Viec.Models
{
    public class HotelDto
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
    }

    public class WeatherDto
    {
        public string Temperature { get; set; }
        public double Humidity { get; set; }
        public string Description { get; set; }
    }

    public class SearchWeatherDto
    {
        public string Address { get; set; }
    }
}
