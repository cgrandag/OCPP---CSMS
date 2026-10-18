using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OcppGateway.Application.AI;
using OcppGateway.Application;
using OcppGateway.Infrastructure;
using OcppGateway.Infrastructure.Adapters;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OcppGateway.Domain;

namespace OcppGateway.Endpoints
{
    public static class CommandEndpoints
    {
        public static void MapCommandEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/command/{chargePointId}/start", async (string chargePointId, StartCommandRequest request, ConnectionManager manager, IOcppValidationService validationService) =>
            {
                var socket = manager.GetSocket(chargePointId);
                if (socket == null || socket.State != WebSocketState.Open) return Results.Problem("Desconectado", statusCode: 404);

                string msgId = Guid.NewGuid().ToString();
                
                string idTag = string.IsNullOrWhiteSpace(request?.IdTag) ? "WEB-USER-001" : request.IdTag;

                bool isAuthorized = await validationService.IsIdTagAuthorizedAsync(idTag);
                if (!isAuthorized)
                {
                    return Results.Problem("IdTag no autorizado o usuario deshabilitado.", statusCode: 403);
                }

                var commandPayload = new { connectorId = 1, idTag = idTag };
                var ocppMessage = new object[] { 2, msgId, "RemoteStartTransaction", commandPayload };

                string jsonString = JsonSerializer.Serialize(ocppMessage);
                await socket.SendAsync(Encoding.UTF8.GetBytes(jsonString), WebSocketMessageType.Text, true, CancellationToken.None);

                return Results.Ok(new { Status = "Enviado Start", MsgId = msgId });
            });

            app.MapPost("/api/command/{chargePointId}/stop", async (string chargePointId, ConnectionManager manager, IChargerRepository repo) =>
            {
                var socket = manager.GetSocket(chargePointId);
                if (socket == null || socket.State != WebSocketState.Open) return Results.Problem("Desconectado", statusCode: 404);

                var activeTx = await repo.GetActiveTransactionAsync(chargePointId);
                int txId = activeTx != null ? activeTx.TransactionId : 0;

                string msgId = Guid.NewGuid().ToString();
                var commandPayload = new { transactionId = txId };
                var ocppMessage = new object[] { 2, msgId, "RemoteStopTransaction", commandPayload };

                string jsonString = JsonSerializer.Serialize(ocppMessage);
                await socket.SendAsync(Encoding.UTF8.GetBytes(jsonString), WebSocketMessageType.Text, true, CancellationToken.None);

                return Results.Ok(new { Status = "Enviado Stop", MsgId = msgId });
            });

            app.MapPost("/api/command/{chargePointId}/reset", async (string chargePointId, ConnectionManager manager) =>
            {
                var socket = manager.GetSocket(chargePointId);
                if (socket == null || socket.State != WebSocketState.Open) return Results.Problem("Desconectado", statusCode: 404);

                string msgId = Guid.NewGuid().ToString();
                var commandPayload = new { type = "Soft" };
                var ocppMessage = new object[] { 2, msgId, "Reset", commandPayload };

                string jsonString = JsonSerializer.Serialize(ocppMessage);
                await socket.SendAsync(Encoding.UTF8.GetBytes(jsonString), WebSocketMessageType.Text, true, CancellationToken.None);

                return Results.Ok(new { Status = "Enviado Reset", MsgId = msgId });
            });

            app.MapPost("/api/command/{chargePointId}/unlock", async (string chargePointId, ConnectionManager manager) =>
            {
                var socket = manager.GetSocket(chargePointId);
                if (socket == null || socket.State != WebSocketState.Open) return Results.Problem("Desconectado", statusCode: 404);

                string msgId = Guid.NewGuid().ToString();
                var commandPayload = new { connectorId = 1 };
                var ocppMessage = new object[] { 2, msgId, "UnlockConnector", commandPayload };

                string jsonString = JsonSerializer.Serialize(ocppMessage);
                await socket.SendAsync(Encoding.UTF8.GetBytes(jsonString), WebSocketMessageType.Text, true, CancellationToken.None);

                return Results.Ok(new { Status = "Enviado Unlock Connector", MsgId = msgId });
            });

            app.MapGet("/chat", async (string pregunta, string cargadorId, IChatbotService ai) =>
            {
                var respuesta = await ai.AskAssistantAsync(cargadorId, pregunta);
                return Results.Ok(new { Bot = respuesta });
            });

            app.MapGet("/api/chargers", async (IChargerRepository repo) =>
            {
                var chargers = await repo.GetAllChargersAsync();
                return Results.Ok(chargers);
            });

            app.MapGet("/api/scan/{physicalId}", async (string physicalId, IChargerRepository repo) =>
            {
                var charger = await repo.GetChargerByPhysicalIdAsync(physicalId);
                if (charger == null)
                    return Results.NotFound(new { Message = "No se encontró ningún cargador con ese código físico." });

                return Results.Ok(new 
                { 
                    ChargePointId = charger.ChargePointId,
                    PhysicalIdentifier = charger.PhysicalIdentifier,
                    Status = charger.Status,
                    IsOnline = charger.IsOnline
                });
            });

            app.MapGet("/api/dashboard/stream", async (HttpContext ctx, OcppGateway.Application.ChargerStateNotifier notifier, CancellationToken ct) =>
            {
                ctx.Response.Headers.Append("Content-Type", "text/event-stream");
                ctx.Response.Headers.Append("Cache-Control", "no-cache");
                ctx.Response.Headers.Append("Connection", "keep-alive");
                
                await ctx.Response.Body.FlushAsync(ct);

                try
                {
                    await foreach (var chargePointId in notifier.SubscribeAsync(ct))
                    {
                        var data = $"data: {{\"chargePointId\": \"{chargePointId}\"}}\n\n";
                        await ctx.Response.WriteAsync(data, ct);
                        await ctx.Response.Body.FlushAsync(ct);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            });
        }
    }

    public class StartCommandRequest
    {
        public string? IdTag { get; set; }
    }
}
