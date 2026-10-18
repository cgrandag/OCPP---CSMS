using Microsoft.Extensions.Logging;
using OcppGateway.Domain;

namespace OcppGateway.Application
{
    public class SmartChargingService : ISmartChargingService
    {
        private readonly IChargerControlService _chargerControl;
        private readonly ILogger<SmartChargingService> _logger;

        private const double SOC_LIMIT = 95.0;

        public SmartChargingService(
            IChargerControlService chargerControl,
            ILogger<SmartChargingService> logger)
        {
            _chargerControl = chargerControl;
            _logger = logger;
        }

        public async Task AnalyzeMeterValuesAsync(string chargePointId, int? transactionId, IEnumerable<MeterValue> meterValues)
        {
            if (meterValues == null) return;

            foreach (var mv in meterValues)
            {
                if (mv.Measurand == "SoC" && double.TryParse(mv.Value, out double soc))
                {
                    if (soc >= SOC_LIMIT)
                    {
                        _logger.LogWarning("[SMART CHARGING] {Id}: Batería al {SoC}% (Límite: {Lim}%). Deteniendo carga...", 
                            chargePointId, soc, SOC_LIMIT);

                        if (transactionId.HasValue)
                        {
                            await _chargerControl.SendStopTransactionAsync(chargePointId, transactionId.Value);
                        }
                        else
                        {
                            _logger.LogWarning("No se puede detener: Falta TransactionId.");
                        }
                    }
                }
            }
        }
    }
}
