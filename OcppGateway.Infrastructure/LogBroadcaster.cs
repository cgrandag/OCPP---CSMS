using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OcppGateway.Infrastructure
{
    public class LogBroadcaster
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _listeners = new();

        public void AddListener(string chargePointId, WebSocket socket)
        {
            var list = _listeners.GetOrAdd(chargePointId, _ => new ConcurrentBag<WebSocket>());
            list.Add(socket);
        }

        public void RemoveListener(string chargePointId, WebSocket socket)
        {
            if (_listeners.TryGetValue(chargePointId, out var list))
            {
            }
        }

        public async Task BroadcastLogAsync(string chargePointId, string direction, string message)
        {
            if (!_listeners.TryGetValue(chargePointId, out var listeners) || listeners.IsEmpty)
                return;

            var logEntry = new
            {
                timestamp = System.DateTime.UtcNow,
                direction = direction,
                payload = message
            };

            var json = JsonSerializer.Serialize(logEntry);
            var bytes = Encoding.UTF8.GetBytes(json);
            var activeSocks = new List<WebSocket>();

            foreach (var socket in listeners)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                        activeSocks.Add(socket);
                    }
                    catch
                    {
                    }
                }
            }

            if (activeSocks.Count != listeners.Count)
            {
                var newBag = new ConcurrentBag<WebSocket>(activeSocks);
                _listeners[chargePointId] = newBag;
            }
        }
    }
}
