using System.Threading.Channels;

namespace OcppGateway.Application
{
    public class ChargerStateNotifier
    {
        private readonly Channel<string> _channel;

        public ChargerStateNotifier()
        {
            _channel = Channel.CreateUnbounded<string>();
        }

        public void NotifyStatusChange(string chargePointId)
        {
            _channel.Writer.TryWrite(chargePointId);
        }

        public IAsyncEnumerable<string> SubscribeAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
