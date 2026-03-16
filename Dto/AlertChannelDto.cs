using System.ComponentModel.DataAnnotations;

namespace OcelotGateway.Dto
{
    /// <summary>
    /// 告警通道
    /// </summary>
    public class AlertChannelDto
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// 实例地址
        /// </summary>
        public string? ExampleAddress { get; set; }

        /// <summary>
        /// 失败类型
        /// </summary>
        public string? FailureType { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMsg { get; set; }

        /// <summary>
        /// 节点
        /// </summary>
        public DownstreamHostAndPortsItem Node { get; set; }
    }
}