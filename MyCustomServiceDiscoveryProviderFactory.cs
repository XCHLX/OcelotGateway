namespace OcelotGateway
{
    using Ocelot.Configuration;
    using Ocelot.Errors;
    using Ocelot.Responses;
    using Ocelot.ServiceDiscovery;
    using Ocelot.ServiceDiscovery.Providers;

    public class MyCustomServiceDiscoveryProviderFactory
        : IServiceDiscoveryProviderFactory
    {
        public string Name => "MyCustom";   // ⚠ 关键

        public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
        {
            if (serviceConfig.Type == Name)
            {
                var provider = new MyCustomServiceDiscoveryProvider(route.ServiceName);
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