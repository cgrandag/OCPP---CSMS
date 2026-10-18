using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using OcppGateway.Application;
using OcppGateway.Domain;
using OcppGateway.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace OcppGateway.Infrastructure.Adapters
{
    public class WebSocketAdapter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebSocketAdapter> _logger;
        private readonly ConnectionManager _connectionManager;
        private readonly LogBroadcaster _logBroadcaster;

        private readonly TimeSpan _timeoutLimit = TimeSpan.FromMinutes(2);

        public WebSocketAdapter(
            IServiceScopeFactory scopeFactory,
            ILogger<WebSocketAdapter> logger,
            ConnectionManager connectionManager,
            LogBroadcaster logBroadcaster)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connectionManager = connectionManager;
            _logBroadcaster = logBroadcaster;
        }

        public async Task HandleConnectionAsync(WebSocket webSocket, string chargePointId)
        {
            _connectionManager.AddSocket(chargePointId, webSocket);
            _logger.LogInformation("[MANAGER] Cargador {Id} conectado y registrado.", chargePointId);

            var buffer = new byte[1024 * 4];
            using var scopeLog = _logger.BeginScope("Cargador: {ChargePointId}", chargePointId);

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    using var cts = new CancellationTokenSource(_timeoutLimit);

                    try
                    {
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            _ = _logBroadcaster.BroadcastLogAsync(chargePointId, "IN", message);

                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var ocppService = scope.ServiceProvider.GetRequiredService<IOcppConnectionService>();
                                
                                string? responseToSend = await ocppService.ProcessMessageAsync(chargePointId, message);

                                if (!string.IsNullOrEmpty(responseToSend))
                                {
                                    _ = _logBroadcaster.BroadcastLogAsync(chargePointId, "OUT", responseToSend);
                                    var responseBytes = Encoding.UTF8.GetBytes(responseToSend);
                                    await webSocket.SendAsync(
                                        new ArraySegment<byte>(responseBytes),
                                        WebSocketMessageType.Text,
                                        true,
                                        CancellationToken.None);
                                }
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("[INFRA] Cierre formal solicitado por el cargador.");
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("[TIMEOUT] ⏳ {Seconds}s sin recibir datos. Asumiendo desconexión.", _timeoutLimit.TotalSeconds);
                        break;
                    }
                    catch (WebSocketException ex)
                    {
                        _logger.LogWarning("[INFRA] ⚠️ Conexión cerrada abruptamente: {Msg}", ex.Message);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[INFRA] Error general en WebSocket.");
            }
            finally
            {
                _connectionManager.RemoveSocket(chargePointId);
                _logger.LogInformation("[MANAGER] Cargador {Id} eliminado del pool.", chargePointId);

                using (var finalScope = _scopeFactory.CreateScope())
                {
                    var repository = finalScope.ServiceProvider.GetRequiredService<IChargerRepository>();
                    await repository.SaveStatusAsync(chargePointId, "Offline", "NoError", "Socket Disconnected");
                    
                    try
                    {
                        var activeTx = await repository.GetActiveTransactionAsync(chargePointId);
                        if (activeTx != null)
                        {
                            _logger.LogWarning("[MANAGER] Cerrando transacción activa {TxId} por desconexión del cargador {Id}.", activeTx.TransactionId, chargePointId);
                            int latestMeter = await repository.GetLatestMeterValueAsync(activeTx.TransactionId);
                            await repository.UpdateTransactionAsync(activeTx.TransactionId, DateTime.UtcNow, latestMeter, "SocketDisconnected");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[MANAGER] Error cerrando transacción pendiente para {Id}.", chargePointId);
                    }
                }

                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    webSocket.Abort();
                }
            }
        }
    }
}