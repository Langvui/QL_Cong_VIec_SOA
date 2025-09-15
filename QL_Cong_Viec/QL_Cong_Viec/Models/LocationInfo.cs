using System.Collections.Generic;

namespace QL_Cong_Viec.Models
{
    public class LocationInfo
    {
        private Location location;
        private Weather weather;
        private ExchangeRate exchangeRate;
        private Distance distance;

        public Dictionary<string, object> GetFullInfo()
        {
            Dictionary<string, object> info = new Dictionary<string, object>();

            info["Location"] = location;
            info["Weather"] = weather;
            info["ExchangeRate"] = exchangeRate;
            info["Distance"] = distance;
            return info;
        }
    }
}


