namespace QL_Cong_Viec.ViewModels
{
    public class HotelSearchViewModel
    {
        public string Location { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Adults { get; set; }
        public int Rooms { get; set; }
        public List<HotelResultViewModel> Results { get; set; } = new();
    }

    public class HotelResultViewModel
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string Price { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
