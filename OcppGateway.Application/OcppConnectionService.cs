using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using OcppGateway.Domain;
using OcppGateway.Application.Messaging;

namespace OcppGateway.Application
{
    public class OcppConnectionService : IOcppConnectionService
    {
        private readonly IChargerRepository _repository;
        private readonly ILogger<OcppConnectionService> _logger;
        private readonly IChargerControlService _controlService;
        private readonly IMessageProducer _messageProducer;
        private readonly ISmartChargingService _smartCharging;
        private readonly IOcppValidationService _validationService;

        public OcppConnectionService(
            IChargerRepository repository,
            ILogger<OcppConnectionService> logger,
            IChargerControlService controlService,
            IMessageProducer messageProducer,
            ISmartChargingService smartCharging,
            IOcppValidationService validationService)
        {
            _repository = repository;
            _logger = logger;
            _controlService = controlService;
            _messageProducer = messageProducer;
            _smartCharging = smartCharging;
            _validationService = validationService;
        }

        public async Task<string?> ProcessMessageAsync(string chargePointId, string messageRaw)
        {
            try
            {
                var jsonNode = JsonNode.Parse(messageRaw);
                if (jsonNode is not JsonArray jsonArray || jsonArray.Count < 3) return null;

                int typeId = jsonArray[0]?.GetValue<int>() ?? -1;
                string msgId = jsonArray[1]?.GetValue<string>() ?? "";
                
                if (typeId != (int)OcppMessageType.Call) return null;

                string action = jsonArray[2]?.ToString().Trim('"') ?? "Unknown";
                var payloadNode = jsonArray[3];

                _logger.LogInformation("IN <== {Id}: {Action}", chargePointId, action);

                object? responsePayload = null;

                switch (action)
                {
                    case "BootNotification":
                        responsePayload = await HandleBootNotification(chargePointId, payloadNode);
                        break;
                    case "Heartbeat":
                        responsePayload = await HandleHeartbeat(chargePointId);
                        break;
                    case "StatusNotification":
                        responsePayload = await HandleStatusNotification(chargePointId, payloadNode);
                        break;
                    case "Authorize":
                        responsePayload = await HandleAuthorize(payloadNode);
                        break;
                    case "StartTransaction":
                        responsePayload = await HandleStartTransaction(chargePointId, payloadNode);
                        break;
                    case "StopTransaction":
                        responsePayload = await HandleStopTransaction(chargePointId, payloadNode);
                        break;
                    case "MeterValues":
                        responsePayload = await HandleMeterValues(chargePointId, payloadNode);
                        break;
                    default:
                         _logger.LogWarning("Unknown action: {Action}", action);
                        break;
                }

                if (responsePayload == null) return JsonSerializer.Serialize(new object[] { (int)OcppMessageType.CallResult, msgId, new { } });

                return JsonSerializer.Serialize(new object[] { (int)OcppMessageType.CallResult, msgId, responsePayload });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando mensaje");
                return null;
            }
        }

        private async Task<BootNotificationResponse> HandleBootNotification(string chargePointId, JsonNode? payload)
        {
             var req = payload?.Deserialize<BootNotificationRequest>();
             
             await _repository.SaveStatusAsync(chargePointId, "Booting", "NoError", "Device Booted");
             if (req != null) 
             {
                 await _repository.SaveChargerInfoAsync(chargePointId, req.ChargePointModel, req.FirmwareVersion ?? "Unknown");
             }

             return new BootNotificationResponse
             {
                 Status = "Accepted",
                 CurrentTime = DateTime.UtcNow.ToString("o"),
                 Interval = 60
             };
        }

        private async Task<HeartbeatResponse> HandleHeartbeat(string chargePointId)
        {
            var charger = await _repository.GetChargerAsync(chargePointId);
            if (charger != null)
            {
                if (charger.Status == "Unknown" || charger.Status == "Offline" || charger.Status == "Unavailable" || charger.Status == "Disconnected")
                {
                    await _repository.SaveStatusAsync(chargePointId, "Online", "NoError", "");
                }
                else
                {
                    await _repository.SaveStatusAsync(chargePointId, charger.Status, charger.LatestErrorCode ?? "NoError", "Heartbeat");
                }
            }
            else
            {
                await _repository.SaveStatusAsync(chargePointId, "Online", "NoError", "");
            }
            return new HeartbeatResponse();
        }

        private async Task<object> HandleStatusNotification(string chargePointId, JsonNode? payload)
        {
            var req = payload?.Deserialize<StatusNotificationRequest>();
            if (req == null) return new { };

            if (req.ErrorCode != "NoError")
            {
                _logger.LogError("ALERTA CRITICA {Id}: {Status} | Error: {Error}", chargePointId, req.Status, req.ErrorCode);
            }

            await _repository.SaveStatusAsync(chargePointId, req.Status, req.ErrorCode, req.Info ?? "");
            return new { };
        }

        private async Task<AuthorizeResponse> HandleAuthorize(JsonNode? payload)
        {
            var req = payload?.Deserialize<AuthorizeRequest>();
            var isAuthorized = await _validationService.IsIdTagAuthorizedAsync(req?.IdTag ?? string.Empty);
            
            return new AuthorizeResponse 
            { 
                IdTagInfo = new IdTagInfo { Status = isAuthorized ? "Accepted" : "Invalid" } 
            };
        }

        private async Task<StartTransactionResponse> HandleStartTransaction(string chargePointId, JsonNode? payload)
        {
            var req = payload?.Deserialize<StartTransactionRequest>();
            if (req == null) return new StartTransactionResponse();

            var isAuthorized = await _validationService.IsIdTagAuthorizedAsync(req.IdTag);
            if (!isAuthorized)
            {
                _logger.LogWarning("⛔ {Id} INTRANSACTION REJECTED (Unauth Tag: {Tag})", chargePointId, req.IdTag);
                return new StartTransactionResponse
                {
                    TransactionId = 0,
                    IdTagInfo = new IdTagInfo { Status = "Invalid" }
                };
            }

            DateTime timestamp = DateTime.TryParse(req.Timestamp, out var ts) ? ts.ToUniversalTime() : DateTime.UtcNow;

            _logger.LogInformation("⚡ {Id} INICIANDO CARGA (Tag: {Tag})", chargePointId, req.IdTag);

            var tx = await _repository.CreateTransactionAsync(chargePointId, req.ConnectorId, req.IdTag, timestamp, req.MeterStart);
            await _repository.SaveStatusAsync(chargePointId, "Charging", "NoError", "Transaction Started");

            return new StartTransactionResponse
            {
                TransactionId = tx.TransactionId,
                IdTagInfo = new IdTagInfo { Status = "Accepted" }
            };
        }

        private async Task<StopTransactionResponse> HandleStopTransaction(string chargePointId, JsonNode? payload)
        {
            var req = payload?.Deserialize<StopTransactionRequest>();
            if (req == null) return new StopTransactionResponse();

            DateTime timestamp = DateTime.TryParse(req.Timestamp, out var ts) ? ts.ToUniversalTime() : DateTime.UtcNow;
            
            _logger.LogInformation("Carga finalizada (Tx: {TxId})", req.TransactionId);

            await _repository.UpdateTransactionAsync(req.TransactionId, timestamp, req.MeterStop, req.Reason ?? "Local");
            await _repository.SaveStatusAsync(chargePointId, "Finishing", "NoError", "Transaction Stopped");
            await _repository.UpdateChargerPowerAsync(chargePointId, 0m);

            return new StopTransactionResponse 
            { 
                IdTagInfo = new IdTagInfo { Status = "Accepted" } 
            };
        }

        private async Task<object> HandleMeterValues(string chargePointId, JsonNode? payload)
        {
             var req = payload?.Deserialize<MeterValuesRequest>();
             if (req == null) return new { };

             if (req.MeterValue != null)
             {
                 foreach (var mv in req.MeterValue)
                 {
                     DateTime timestamp = DateTime.TryParse(mv.Timestamp, out var ts) ? ts.ToUniversalTime() : DateTime.UtcNow;

                     if (mv.SampledValue != null)
                     {
                         foreach (var sv in mv.SampledValue)
                         {
                             string measurand = sv.Measurand ?? "Energy.Active.Import.Register";
                             string value = sv.Value ?? "0";
                             string unit = sv.Unit ?? "Wh";

                             _logger.LogInformation("{Meas}: {Val} {Unit}", measurand, value, unit);

                             var mvDto = new OcppGateway.Domain.MeterValue
                             {
                                 ChargePointId = chargePointId,
                                 TransactionId = req.TransactionId ?? 0,
                                 ConnectorId = req.ConnectorId,
                                 Timestamp = timestamp,
                                 Measurand = measurand,
                                 Value = value,
                                 Unit = unit
                             };

                             if (measurand == "Power.Active.Import")
                             {
                                 if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal powerVal))
                                 {
                                     decimal kw = (unit == "W") ? powerVal / 1000m : powerVal;
                                     await _repository.UpdateChargerPowerAsync(chargePointId, Math.Round(kw, 2));
                                 }
                             }

                             _messageProducer.SendMessage(mvDto, "meter_values");
                             await _repository.SaveMeterValueAsync(mvDto);
                             await _smartCharging.AnalyzeMeterValuesAsync(chargePointId, req.TransactionId, new[] { mvDto });
                         }
                     }
                 }
             }
             return new { };
        }
    }
}