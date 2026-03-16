using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Ocelot.Values;
using OcelotGateway.Dto;
using System.Xml.Linq;

namespace OcelotGateway.Controllers
{
    [Route("homes/[action]")]
    public class HomesController : Controller
    {
        /// <summary>
        /// 测试控制器
        /// </summary>
        private readonly IConfiguration _configuration;      // 配置信息

        /// <summary>
        /// 缓存接口，用于缓存服务信息
        /// </summary>
        private readonly IMemoryCache _cache;  // 内存缓存接口，用于缓存服务信息

        /// <summary>
        ///   数据库
        /// </summary>
        /// <param name="configuration"></param>
        public HomesController(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        /// <summary>
        /// 测试接口
        /// http://localhost:7000/internal/homes/ping
        /// </summary>
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok("mvc ok");
        }

        /// <summary>
        /// 列表页面
        /// http://localhost:7000/internal/homes/index
        /// </summary>
        [HttpGet]
        public IActionResult Index(string pwd)
        {
            if (pwd != DateTime.Now.ToString("MMdd")) return Unauthorized();

            var ocelotGatewayDtos = new List<OcelotGatewayDto>();
            var serviceDiscoveryList = new List<ServiceDiscovery>();
            _configuration.Bind("ServiceDiscovery", serviceDiscoveryList);

            foreach (var service in serviceDiscoveryList)
            {
                if (service.DownstreamHostAndPorts == null) continue;

                var cacheKey = $"service_health_{service.ServiceName}";
                var cacheService = _cache.Get<List<Service>>(cacheKey) ?? new List<Service>();

                foreach (var item in service.DownstreamHostAndPorts)
                {
                    var any = cacheService.Any(it => it.Id == $"{item.Host}:{item.Port}");

                    ocelotGatewayDtos.Add(new OcelotGatewayDto()
                    {
                        ExampleAddress = $"{item.Host}:{item.Port}",
                        HealthCheck = service.Health,
                        ServiceName = service.ServiceName,
                        Alias = service.Alias,
                        State = any
                    });
                }
            }

            return View(ocelotGatewayDtos);
        }
    }
}