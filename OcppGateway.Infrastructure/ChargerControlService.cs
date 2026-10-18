using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OcppGateway.Domain;

namespace OcppGateway.Infrastructure
{
    public class ChargerControlService : IChargerControlService
    {
        private readonly ConnectionManager _connectionManager;

        public ChargerControlService(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task SendStartTransactionAsync(string chargePointId)
        {
            var socket = _connectionManager.GetSocket(chargePointId);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var cmd = new object[] {
                    2,
                    Guid.NewGuid().ToString(),
                    "RemoteStartTransaction",
                    new { connectorId = 1, idTag = "SYSTEM-AUTO" }
                };

                await SendJsonAsync(socket, cmd);
            }
        }

        public async Task SendStopTransactionAsync(string chargePointId, int transactionId)
        {
            var socket = _connectionManager.GetSocket(chargePointId);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var cmd = new object[] {
                    2,
                    Guid.NewGuid().ToString(),
                    "RemoteStopTransaction",
                    new { transactionId = transactionId }
                };

                await SendJsonAsync(socket, cmd);
            }
        }

        private async Task SendJsonAsync(WebSocket socket, object payload)
        {
            string json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}