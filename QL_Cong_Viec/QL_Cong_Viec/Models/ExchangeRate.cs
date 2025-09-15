namespace QL_Cong_Viec.Models
{
    public class ExchangeRate
    {
        private double rate;
        private String fromCurrency;
        private String toCurrency;

        public double convertAmount(double amount)
        {
            return amount * rate;
        }
    }
}
