using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using OcelotGateway.Dto;
using System.Net.Http;

namespace OcelotGateway.Providers
{
    /// <summary>
    /// 自定义服务发现提供者实现类
    /// 继承自 IServiceDiscoveryProvider 接口，用于服务发现功能
    /// </summary>
    public class MyCustomServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        // 服务名称字段
        private readonly string _serviceName;

        /// <summary>
        /// 读取配置文件
        /// </summary>
        private readonly IConfiguration _configuration;

        private readonly IMemoryCache _cache;  // 内存缓存接口，用于缓存服务信息
        private readonly HttpClient _httpClient;  // HTTP 客户端，用于发送 HTTP 请求

        /// <summary>
        /// 初始化自定义服务发现提供者
        /// </summary>
        /// <param name="serviceName">服务名称</param>
        /// <param name="configuration">配置接口</param>
        /// <param name="cache">内存缓存接口</param>
        /// <param name="httpClientFactory">HTTP 客户端工厂</param>
        public MyCustomServiceDiscoveryProvider(string serviceName,
            IConfiguration configuration,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory)
        {
            _serviceName = serviceName;
            _configuration = configuration;
            _cache = cache;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// 异步获取服务列表
        /// </summary>
        /// <returns>服务实例列表</returns>
        public async Task<List<Service>> GetAsync()
        {
            //Console.WriteLine($"获取服务：{_serviceName}");

            var services = new List<Service>();

            // 获取所有可用实例
            var candidates = GetAllInstances();
            // 返回候选服务或空列表
            return candidates ?? services;
        }

        /// <summary>
        /// 获取所有服务实例
        /// </summary>
        /// <returns>服务实例列表</returns>
        private List<Service> GetAllInstances()
        {
            var list = new List<Service>();

            // 尝试从缓存中获取服务信息
            var cacheKey = $"service_health_{_serviceName}";
            if (_cache.TryGetValue(cacheKey, out List<Service>? services))
            {
                return services ?? list;
            }
            else
            {
                // 从配置文件中绑定服务发现列表
                var serviceDiscoveryList = new List<ServiceDiscovery>();
                _configuration.Bind("ServiceDiscovery", serviceDiscoveryList);
                if (serviceDiscoveryList.Count > 0)
                {
                    // 查找匹配当前服务名称的配置信息
                    var serviceInfo = serviceDiscoveryList.Where(it => it.ServiceName == _serviceName).FirstOrDefault();
                    if (serviceInfo != null)
                    {
                        if (serviceInfo.DownstreamHostAndPorts != null)
                        {
                            // 遍历下游主机和端口并转换为 Service 对象
                            foreach (var item in serviceInfo.DownstreamHostAndPorts)
                            {
                                list.Add(new Service(_serviceName,
                                new ServiceHostAndPort(item.Host, item.Port),
                                item.Host + item.Port, "", new List<string>()));
                            }
                        }
                    }
                }
            }
            return list;
        }
    }
}