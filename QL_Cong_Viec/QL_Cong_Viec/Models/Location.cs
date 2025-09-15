namespace QL_Cong_Viec.Models
{
    public class Location
    {
        private String locationId;
        private String name;
        private double latitude;
        private double longitude;

        public String getCoordinates()
        {
            return latitude + ", " + longitude;
        }
    }
}
