namespace OcelotGateway.Dto
{
    public class ServiceDiscoveryChannel : ServiceDiscovery
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// 服务发现
    /// </summary>
    public class ServiceDiscovery
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// 健康检查路径
        /// </summary>
        public string? Health { get; set; }

        /// <summary>
        /// 下游服务主机和端口列表
        /// </summary>
        public List<DownstreamHostAndPortsItem>? DownstreamHostAndPorts { get; set; }

        /// <summary>
        ///  服务调用超时时间
        /// </summary>
        public int TimeOut { get; set; } = 2;

        /// <summary>
        /// 钉钉WebHook
        /// </summary>
        public string? DingDingWebHook { get; set; }

        /// <summary>
        /// 钉钉密钥
        /// </summary>
        public string? DingDingSecret { get; set; }
    }

    /// <summary>
    /// 下游地址和端口
    /// </summary>
    public class DownstreamHostAndPortsItem
    {
        /// <summary>
        /// 下游地址
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// 下游端口
        /// </summary>
        public int Port { get; set; }
    }
}