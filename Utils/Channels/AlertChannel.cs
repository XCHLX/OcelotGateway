using OcelotGateway.Dto;
using System.Threading.Channels;

namespace OcelotGateway.Utils.Channels
{
    public class AlertChannel
    {
        public Channel<KeyValuePair<string, ServiceDiscoveryChannel>> MyChannel { get; }
            = Channel.CreateUnbounded<KeyValuePair<string, ServiceDiscoveryChannel>>();
    }
}