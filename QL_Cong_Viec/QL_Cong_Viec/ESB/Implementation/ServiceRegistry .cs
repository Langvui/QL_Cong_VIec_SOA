using QL_Cong_Viec.ESB.Interface;
using System.Collections.Concurrent;

namespace QL_Cong_Viec.ESB.Implementation
{
    public class ServiceRegistry : IServiceRegistry
    {
        private readonly ConcurrentDictionary<string, IService> _services = new();
        private readonly ILogger<ServiceRegistry> _logger;

        public ServiceRegistry(ILogger<ServiceRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterService(string serviceName, IService service)
        {
            _services.AddOrUpdate(serviceName, service, (key, existing) => service);
            _logger.LogInformation("Registered service: {ServiceName}", serviceName);
        }

        public IService GetService(string serviceName)
        {
            _services.TryGetValue(serviceName, out var service);
            return service;
        }

        public List<string> GetAvailableServices()
        {
            return _services.Keys.ToList();
        }
    }
}

