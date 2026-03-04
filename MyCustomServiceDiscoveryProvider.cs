using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Net.Http;

namespace OcelotGateway
{
    public class MyCustomServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly string _serviceName;
        private readonly HttpClient _httpClient;

        public MyCustomServiceDiscoveryProvider(string serviceName)
        {
            _serviceName = serviceName;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };
        }

        public async Task<List<Service>> GetAsync()
        {
            Console.WriteLine($"获取服务: {_serviceName}");

            var services = new List<Service>();

            var candidates = GetAllInstances();

            foreach (var instance in candidates)
            {
                var isHealthy = await CheckHealthAsync(instance);

                if (isHealthy)
                {
                    services.Add(instance);
                }
                else
                {
                    Console.WriteLine($"节点不健康: {instance.HostAndPort}");
                }
            }

            return services;
        }

        private List<Service> GetAllInstances()
        {
            var list = new List<Service>();

            if (_serviceName == "MyBackendService")
            {
                list.Add(new Service(_serviceName,
                    new ServiceHostAndPort("127.0.0.1", 8098),
                    "order-001", "", new List<string>()));

                list.Add(new Service(_serviceName,
                    new ServiceHostAndPort("127.0.0.1", 8099),
                    "order-002", "", new List<string>()));
            }

            return list;
        }

        private async Task<bool> CheckHealthAsync(Service service)
        {
            try
            {
                var url = $"http://{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}/api/DemoTest/getg?text=123";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                var response = await _httpClient.GetAsync(url, cts.Token);

                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("健康检查超时");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"健康检查异常: {ex.Message}");
                return false;
            }
        }
    }
}