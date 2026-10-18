using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OcppGateway.Infrastructure.Adapters;
using System.Net.WebSockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OcppGateway.Endpoints
{
    public static class OcppEndpoints
    {
        public static void MapOcppEndpoints(this WebApplication app)
        {
            app.MapWhen(context => 
                context.WebSockets.IsWebSocketRequest && 
                context.Request.Path.Value != null && 
                context.Request.Path.Value.Count(c => c == '/') == 1 &&
                !context.Request.Path.Value.TrimStart('/').Equals("api", StringComparison.OrdinalIgnoreCase), 
            appBuilder => 
            {
                appBuilder.Run(async context =>
                {
                    var chargePointId = context.Request.Path.Value!.TrimStart('/');
                    var wsAdapter = context.RequestServices.GetRequiredService<WebSocketAdapter>();

                    string? subProtocol = null;
                    if (context.WebSockets.WebSocketRequestedProtocols.Contains("ocpp1.6")) subProtocol = "ocpp1.6";
                    else if (context.WebSockets.WebSocketRequestedProtocols.Contains("ocpp1.6j")) subProtocol = "ocpp1.6j";

                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
                    await wsAdapter.HandleConnectionAsync(webSocket, chargePointId);
                });
            });

            app.MapGet("/api/logs/{chargePointId}", async (HttpContext context, string chargePointId, Infrastructure.LogBroadcaster logBroadcaster) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    logBroadcaster.AddListener(chargePointId, webSocket);
                    
                    try
                    {
                        var buffer = new byte[1024];
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (result.MessageType == WebSocketMessageType.Close)
                                break;
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        logBroadcaster.RemoveListener(chargePointId, webSocket);
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });
        }
    }
}
