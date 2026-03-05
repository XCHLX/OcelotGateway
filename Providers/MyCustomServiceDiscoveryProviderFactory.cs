using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;

namespace OcelotGateway.Providers
{
    /// <summary>
    /// 自定义服务发现提供者工厂
    /// </summary>
    public class MyCustomServiceDiscoveryProviderFactory
        : IServiceDiscoveryProviderFactory
    {
        private readonly IServiceProvider _sp;

        public MyCustomServiceDiscoveryProviderFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public string Name => "MyCustom";   // ⚠ 关键

        public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
        {
            if (serviceConfig.Type == Name)
            {
                //var provider = new MyCustomServiceDiscoveryProvider(route.ServiceName);
                var provider = ActivatorUtilities
                .CreateInstance<MyCustomServiceDiscoveryProvider>(
                    _sp, route.ServiceName);
                return new OkResponse<IServiceDiscoveryProvider>(provider);
            }

            // 必须返回 ErrorResponse，不能 return null
            return new ErrorResponse<IServiceDiscoveryProvider>(
                new List<Error>
                {
                new UnableToFindServiceDiscoveryProviderError(
                    serviceConfig.Type)
                });
        }
    }
}