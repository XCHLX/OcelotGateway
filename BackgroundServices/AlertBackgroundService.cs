using Microsoft.AspNetCore.Http;
using OcelotGateway.Dto;
using OcelotGateway.Utils;
using OcelotGateway.Utils.Channels;
using System.Collections.Concurrent;

namespace OcelotGateway.BackgroundServices
{
    /// <summary>
    /// 告警服务（滑动时间窗口优化）
    /// </summary>
    public class AlertBackgroundService : BackgroundService
    {
        private readonly AlertChannel _alertChannel;

        // 每个服务的失败时间队列
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _failureDict = new();

        // 记录每个服务上次告警时间
        private readonly ConcurrentDictionary<string, DateTime> _lastAlertDict = new();

        private readonly int _threshold = 3;
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
        private readonly IConfiguration _configuration;      // 配置信息

        public AlertBackgroundService(AlertChannel alertChannel, IConfiguration configuration)
        {
            _alertChannel = alertChannel;
            _configuration = configuration;

            _threshold = _configuration.GetValue<int>("AlertThreshold", 1);
            _timeWindow = TimeSpan.FromSeconds(_configuration.GetValue<int>("AlertTimeWindow", 60));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var alert in _alertChannel.MyChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await HandleAlert(alert);
                    }
                    catch (Exception ex)
                    {
                        // 兜底：单条告警处理失败只记日志，不能让异常逃出 ExecuteAsync，
                        // 否则 .NET 6+ 默认 StopHost 行为会直接终止整个网关进程。
                        // 注意这里不能用 stoppingToken 取消的异常去中断循环，否则关停时会刷错误日志。
                        TextLogger.Log($"告警处理异常: {ex}", "告警异常");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 宿主正在关停，正常退出
            }
        }

        /// <summary>
        /// 处理单条告警：滑动窗口计数 + 达到阈值后推送钉钉
        /// </summary>
        private async Task HandleAlert(AlertChannelDto alert)
        {
            Console.WriteLine($"{alert.ServiceName}-----{alert.ErrorMsg}");
            if (string.IsNullOrWhiteSpace(alert.ServiceName))
                return;

            var queue = _failureDict.GetOrAdd(alert.ServiceName + alert.ExampleAddress, _ => new ConcurrentQueue<DateTime>());
            var now = DateTime.UtcNow;

            // 添加当前失败时间
            queue.Enqueue(now);

            // 移除窗口之外的失败记录
            while (queue.TryPeek(out var time) && (now - time) > _timeWindow)
            {
                queue.TryDequeue(out _);
            }

            // 判断是否达到告警阈值
            if (queue.Count >= _threshold)
            {
                // 获取上次告警时间
                _lastAlertDict.TryGetValue(alert.ServiceName + alert.ExampleAddress, out var lastAlertTime);

                // 当前时间和上次告警时间的间隔大于窗口才发送告警
                if ((now - lastAlertTime) > _timeWindow)
                {
                    await SendDingTalkAlert(alert, queue.Count);
                    _lastAlertDict[alert.ServiceName + alert.ExampleAddress] = now;
                }
            }
        }

        /// <summary>
        /// 发送钉钉告警
        /// </summary>
        private async Task SendDingTalkAlert(AlertChannelDto alert, int count)
        {
            //string erorMsg = $"【钉钉告警】服务 {alert.ServiceName} {alert.Node.Host}:{alert.Node.Port} {alert.FailureType} {alert.ErrorMsg} 在 {_timeWindow.TotalMinutes} 分钟内失败 {count} 次！";
            string baseurl = _configuration[$"GlobalConfiguration:BaseUrl"] + "/homes/Index?pwd=" + DateTime.Now.ToString("MMdd");
            string markdown = $@"
###  ⛔ 网关异常
- **服务**  **`{alert.ServiceName}`**` {alert.Node.Host}:{alert.Node.Port}` <br>
- **网关管理**：[点击查看]( {baseurl} )<br>
- **异常时间**：{DateTime.Now}<br>
- **错误信息**：`{alert.ErrorMsg} ` <br>
> `在 {_timeWindow.TotalMinutes} 分钟内失败 {count} 次！`
";

            var serviceDiscoveryList = new List<ServiceDiscovery>();  // 服务发现列表
            _configuration.Bind("ServiceDiscovery", serviceDiscoveryList);  // 从配置中绑定服务发现信息
            var serviceDiscovery = serviceDiscoveryList.Where(it => it.ServiceName == alert.ServiceName).FirstOrDefault();
            if (serviceDiscovery != null)
            {
                await DingTalkNotifier.SendTextMessageAsync(serviceDiscovery.DingDingWebHook, serviceDiscovery.DingDingSecret, $"{alert.ServiceName}-异常通知", markdown);
            }
        }
    }
}