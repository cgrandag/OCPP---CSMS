using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace OcppGateway.Infrastructure
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _activeSockets = new();

        public void AddSocket(string chargePointId, WebSocket socket)
        {
            _activeSockets.AddOrUpdate(chargePointId, socket, (key, oldSocket) => socket);
        }

        public WebSocket? GetSocket(string chargePointId)
        {
            _activeSockets.TryGetValue(chargePointId, out var socket);
            return socket;
        }

        public void RemoveSocket(string chargePointId)
        {
            _activeSockets.TryRemove(chargePointId, out _);
        }

        public int Count => _activeSockets.Count;
    }
}