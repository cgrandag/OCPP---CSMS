using OcppGateway.Domain;

namespace OcppGateway.Application
{
    public interface ISmartChargingService
    {
        Task AnalyzeMeterValuesAsync(string chargePointId, int? transactionId, IEnumerable<MeterValue> meterValues);
    }
}
