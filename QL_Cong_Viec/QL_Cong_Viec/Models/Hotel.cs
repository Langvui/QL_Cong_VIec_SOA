namespace QL_Cong_Viec.Models
{
    public class Hotel
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }

        // Quan hệ: 1 Hotel có nhiều Weather
        public List<Weather> Weathers { get; set; } = new List<Weather>();
    }
}
