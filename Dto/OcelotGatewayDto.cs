namespace OcelotGateway.Dto
{
    /// <summary>
    /// ocelot网关数据模型
    /// </summary>
    public class OcelotGatewayDto
    {
        /// <summary>
        ///   服务名称
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        ///   实例地址
        /// </summary>
        public string? ExampleAddress { get; set; }

        /// <summary>
        ///   状态
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        ///   健康检查
        /// </summary>
        public string? HealthCheck { get; set; }
    }
}