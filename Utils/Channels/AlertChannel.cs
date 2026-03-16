using OcelotGateway.Dto;
using System.Threading.Channels;

namespace OcelotGateway.Utils.Channels
{
    public class AlertChannel
    {
        public Channel<AlertChannelDto> MyChannel { get; }
            = Channel.CreateUnbounded<AlertChannelDto>();
    }
}