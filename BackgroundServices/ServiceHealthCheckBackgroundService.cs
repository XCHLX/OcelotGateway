using Microsoft.Extensions.Caching.Memory;  // 引入内存缓存功能
using Ocelot.Values;                       // 引入Ocelot网关的值类型
using OcelotGateway.Dto;
using OcelotGateway.Utils;
using OcelotGateway.Utils.Channels;

namespace OcelotGateway.BackgroundServices                     // Ocelot网关命名空间
{
    /// <summary>
    /// 服务健康检查
    /// 后台服务，用于定期检查下游服务的健康状态
    /// </summary>
    public class ServiceHealthCheckBackgroundService : BackgroundService
    {
        // 依赖注入的服务
        private readonly IServiceProvider _serviceProvider;  // 服务提供者，用于获取其他服务

        private readonly IConfiguration _configuration;      // 配置信息
        private readonly IMemoryCache _cache;               // 内存缓存，用于存储健康检查结果
        private readonly IHttpClientFactory _httpClientFactory; // HTTP客户端工厂，用于创建HTTP客户端
        private readonly AlertChannel _alertChannel;

        /// <summary>
        /// 构造函数，注入所需服务
        /// </summary>
        public ServiceHealthCheckBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            AlertChannel alertChannel)
        {
            // 初始化各个服务实例
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _alertChannel = alertChannel;
        }

        /// <summary>
        /// 后台服务主执行方法
        /// 定期执行健康检查
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 当服务未取消时，持续执行健康检查
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAllServices();  // 执行所有服务的健康检查
                var interval = _configuration.GetValue<int>("ServiceHealthCheckIntervalSeconds", 5);
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);  // 每分钟执行一次
            }
        }

        /// <summary>
        /// 检查所有下游服务的健康状态
        /// </summary>
        private async Task CheckAllServices()
        {
            var serviceDiscoveryList = new List<ServiceDiscovery>();  // 服务发现列表
            _configuration.Bind("ServiceDiscovery", serviceDiscoveryList);  // 从配置中绑定服务发现信息

            var httpClient = _httpClientFactory.CreateClient();  // 创建HTTP客户端

            // 遍历所有服务
            foreach (var service in serviceDiscoveryList)
            {
                var healthyList = new List<Service>();  // 健康服务列表
                if (service.DownstreamHostAndPorts == null)
                {
                    continue;  // 如果没有下游地址，跳过
                }
                // 检查每个服务的每个节点
                foreach (var node in service.DownstreamHostAndPorts)
                {
                    try
                    {
                        var url = $"http://{node.Host}:{node.Port}{service.Health}";  // 构建健康检查URL
                        var isok = await CheckHealthAsync(url, service);  // 执行健康检查

                        if (isok)
                        {
                            // 如果服务健康，添加到健康列表
                            healthyList.Add(new Service(
                                service.ServiceName,
                                new ServiceHostAndPort(node.Host, node.Port),
                                $"{node.Host}:{node.Port}",
                                service.ServiceName,
                                []
                            ));
                        }
                    }
                    catch { }  // 捕获并忽略异常
                }
                // 如果没有健康节点，跳过
                if (healthyList.Count == 0)
                {
                    _cache.Remove($"service_health_{service.ServiceName}");
                    continue;
                }
                // 将健康服务列表缓存起来，有效期10分钟
                _cache.Set(
                    $"service_health_{service.ServiceName}",
                    healthyList,
                    TimeSpan.FromMinutes(10));
            }
            await TextLogger.LogAsync($"健康检查已更新", "健康检查");

            Console.WriteLine("健康检查已更新" + DateTime.Now);  // 输出更新时间
        }

        /// <summary>
        /// 检查单个服务的健康状态
        /// </summary>
        /// <param name="url">健康检查URL</param>
        /// <returns>服务是否健康</returns>
        private async Task<bool> CheckHealthAsync(string url, ServiceDiscovery serviceDiscovery)
        {
            var service = new ServiceDiscoveryChannel()
            {
                ServiceName = serviceDiscovery.ServiceName,
                Health = serviceDiscovery.Health,
                DownstreamHostAndPorts = serviceDiscovery.DownstreamHostAndPorts,
                TimeOut = serviceDiscovery.TimeOut,
                DingDingWebHook = serviceDiscovery.DingDingWebHook,
                DingDingSecret = serviceDiscovery.DingDingSecret
            };
            try
            {
                // 设置2秒超时
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(serviceDiscovery.TimeOut));
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    service.Message = response.ReasonPhrase;
                    await TextLogger.LogAsync($"{url}健康检查失败", "健康检查");
                    _alertChannel.MyChannel.Writer.TryWrite(new KeyValuePair<string, ServiceDiscoveryChannel>(url, service));
                }
                await TextLogger.LogAsync($"{url}健康检查成功", "健康检查成功");
                return response.IsSuccessStatusCode;  // 返回HTTP状态码是否成功
            }
            catch (TaskCanceledException)
            {
                await TextLogger.LogAsync($"{url}健康检查超时", "健康检查");
                _alertChannel.MyChannel.Writer.TryWrite(new KeyValuePair<string, ServiceDiscoveryChannel>(url, service));
                return false;  // 超时返回不健康
            }
            catch (Exception ex)
            {
                await TextLogger.LogAsync($"{url}健康检查异常: {ex.Message}", "健康检查");
                _alertChannel.MyChannel.Writer.TryWrite(new KeyValuePair<string, ServiceDiscoveryChannel>(url, service));

                return false;  // 异常返回不健康
            }
        }
    }
}