using OcelotGateway.Dto;
using OcelotGateway.Utils;
using OcelotGateway.Utils.Channels;
using System.Threading.Channels;

namespace OcelotGateway.BackgroundServices
{
    /// <summary>
    /// 告警服务
    /// </summary>
    public class AlertBackgroundService : BackgroundService
    {
        private readonly AlertChannel _alertChannel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="alertChannel"></param>
        public AlertBackgroundService(AlertChannel alertChannel)
        {
            _alertChannel = alertChannel;
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _alertChannel.MyChannel.Reader.ReadAllAsync(stoppingToken))
            {
                //Console.WriteLine(message + "=============");
                //Console.WriteLine(message.Key);
                //Console.WriteLine(message.Value.ServiceName);

                //DingTalkNotifier.SendTextMessageAsync(message.Value.DingDingWebHook, message.Value.DingDingSecret, message.Value.Message).Wait();
            }
        }
    }
}