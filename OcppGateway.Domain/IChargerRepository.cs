using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcppGateway.Domain
{
    public interface IChargerRepository
    {
        Task SaveStatusAsync(string chargePointId, string status, string errorCode, string info);
        Task<Charger?> GetChargerAsync(string chargePointId);
        Task<Charger?> GetChargerByPhysicalIdAsync(string physicalId);
        Task<IEnumerable<Charger>> GetAllChargersAsync();
        
        Task<Charger> CreateChargerAsync(Charger charger);
        Task<Charger?> UpdateChargerAsync(string chargePointId, Charger chargerDetails);
        Task<bool> DeleteChargerAsync(string chargePointId);

        Task<IEnumerable<Transaction>> GetAllTransactionsAsync();
        Task<IEnumerable<Transaction>> GetTransactionsByChargerAsync(string chargePointId);
        Task<Transaction?> GetActiveTransactionAsync(string chargePointId);
        Task<Transaction> CreateTransactionAsync(string chargePointId, int connectorId, string idTag, DateTime startTime, int startMeter);
        Task UpdateTransactionAsync(int transactionId, DateTime stopTime, int stopMeter, string stopReason);
        Task<int> GetLatestMeterValueAsync(int transactionId);
        Task SaveMeterValueAsync(MeterValue meterValue);
        Task SaveChargerInfoAsync(string chargePointId, string model, string firmwareVersion);
        Task UpdateChargerPowerAsync(string chargePointId, decimal powerKw);
        Task SaveNetworkInfoAsync(string chargePointId, int? rssi, string? networkType);
        
        Task<string> GetLastStatusAsync(string chargePointId);
    }
}
