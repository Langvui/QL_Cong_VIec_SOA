using QL_Cong_Viec.ESB.Interface;
using QL_Cong_Viec.ESB.Models;
using QL_Cong_Viec.Service;

namespace QL_Cong_Viec.ESB.Services
{
    public class AmadeusServiceAdapter : IService
    {
        
        private readonly AmadeusService _amadeusService;
        private readonly ILogger<AmadeusServiceAdapter> _logger;

        public string ServiceName => "AmadeusService";
        public bool IsHealthy { get; private set; } = true;

        public AmadeusServiceAdapter(AmadeusService amadeusService, ILogger<AmadeusServiceAdapter> logger)
        {
            _amadeusService = amadeusService;
            _logger = logger;
        }

        public async Task<ServiceResponse> HandleRequestAsync(ServiceRequest request)
        {
            try
            {
                switch (request.Operation.ToLower())
                {
                    case "getprice":
                        var depIata = request.Parameters["depIata"].ToString();
                        var arrIata = request.Parameters["arrIata"].ToString();
                        var price = await _amadeusService.GetPriceAsync(depIata, arrIata);

                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = true,
                            Data = price
                        };

                    default:
                        return new ServiceResponse
                        {
                            RequestId = request.RequestId,
                            Success = false,
                            ErrorMessage = $"Operation {request.Operation} not supported"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AmadeusService operation {Operation}", request.Operation);
                IsHealthy = false;

                return new ServiceResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
