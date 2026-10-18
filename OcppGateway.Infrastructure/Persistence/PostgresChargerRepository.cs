using Microsoft.EntityFrameworkCore;
using OcppGateway.Domain;

namespace OcppGateway.Infrastructure.Persistence
{
    public class PostgresChargerRepository : IChargerRepository
    {
        private readonly PostgresDbContext _context;
        private readonly OcppGateway.Application.ChargerStateNotifier _notifier;

        public PostgresChargerRepository(PostgresDbContext context, OcppGateway.Application.ChargerStateNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task SaveStatusAsync(string chargePointId, string status, string errorCode, string info)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger == null)
            {
                charger = new Charger { ChargePointId = chargePointId };
                _context.Chargers.Add(charger);
            }

            charger.Status = status;
            charger.LatestErrorCode = errorCode;
            charger.LatestInfo = info;
            charger.LastHeartbeat = DateTime.UtcNow;
            charger.IsOnline = true;

            await _context.SaveChangesAsync();
            
            _notifier.NotifyStatusChange(chargePointId);
        }

        public async Task SaveChargerInfoAsync(string chargePointId, string model, string firmwareVersion)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger != null)
            {
                charger.Model = model;
                charger.FirmwareVersion = firmwareVersion;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateChargerPowerAsync(string chargePointId, decimal powerKw)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger != null)
            {
                charger.CurrentPowerKw = powerKw;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveNetworkInfoAsync(string chargePointId, int? rssi, string? networkType)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger != null)
            {
                charger.Rssi = rssi;
                charger.NetworkType = networkType;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GetLastStatusAsync(string chargePointId)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            return charger != null 
                ? $"{charger.Status} (Error: {charger.LatestErrorCode})" 
                : "Unknown";
        }

        public async Task<Charger?> GetChargerAsync(string chargePointId)
        {
            return await _context.Chargers
                .Include(c => c.Transactions)
                .Include(c => c.Tariff)
                .FirstOrDefaultAsync(c => c.ChargePointId == chargePointId);
        }

        public async Task<IEnumerable<Charger>> GetAllChargersAsync()
        {
            return await _context.Chargers
                .Include(c => c.Tariff)
                .OrderBy(c => c.ChargePointId)
                .ToListAsync();
        }

        public async Task<Charger?> GetChargerByPhysicalIdAsync(string physicalId)
        {
            return await _context.Chargers.Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.PhysicalIdentifier == physicalId);
        }

        public async Task<Charger> CreateChargerAsync(Charger charger)
        {
            _context.Chargers.Add(charger);
            await _context.SaveChangesAsync();
            return charger;
        }

        public async Task<Charger?> UpdateChargerAsync(string chargePointId, Charger chargerDetails)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger == null) return null;

            charger.Location = chargerDetails.Location;
            charger.Name = chargerDetails.Name;
            charger.PhysicalIdentifier = chargerDetails.PhysicalIdentifier;
            charger.Latitude = chargerDetails.Latitude;
            charger.Longitude = chargerDetails.Longitude;
            charger.Model = chargerDetails.Model;
            charger.FirmwareVersion = chargerDetails.FirmwareVersion;
            charger.ConnectorType = chargerDetails.ConnectorType;
            charger.TariffId = chargerDetails.TariffId;

            await _context.SaveChangesAsync();
            return charger;
        }

        public async Task<bool> DeleteChargerAsync(string chargePointId)
        {
            var charger = await _context.Chargers.FindAsync(chargePointId);
            if (charger == null) return false;

            _context.Chargers.Remove(charger);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Charger)
                .OrderByDescending(t => t.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByChargerAsync(string chargePointId)
        {
            return await _context.Transactions
                .Where(t => t.ChargePointId == chargePointId)
                .OrderByDescending(t => t.StartTime)
                .ToListAsync();
        }

        public async Task<Transaction?> GetActiveTransactionAsync(string chargePointId)
        {
            return await _context.Transactions
                .Where(t => t.ChargePointId == chargePointId && t.StopTime == null)
                .OrderByDescending(t => t.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction> CreateTransactionAsync(string chargePointId, int connectorId, string idTag, DateTime startTime, int startMeter)
        {
            decimal pricePerKwh = 0.30m;
            decimal pricePerMinute = 0m;
            decimal flatFee = 0m;

            var charger = await _context.Chargers.Include(c => c.Tariff).FirstOrDefaultAsync(c => c.ChargePointId == chargePointId);
            if (charger?.Tariff != null)
            {
                pricePerKwh = charger.Tariff.PricePerKwh;
                pricePerMinute = charger.Tariff.PricePerMinute;
                flatFee = charger.Tariff.FlatFee;
            }

            if (!string.IsNullOrEmpty(idTag))
            {
                var user = await _context.Users.Include(u => u.Tariff).FirstOrDefaultAsync(u => u.IdTag == idTag);
                if (user?.Tariff != null)
                {
                    pricePerKwh = user.Tariff.PricePerKwh;
                    pricePerMinute = user.Tariff.PricePerMinute;
                    flatFee = user.Tariff.FlatFee;
                }
            }

            var tx = new Transaction
            {
                ChargePointId = chargePointId,
                ConnectorId = connectorId,
                IdTag = idTag,
                StartTime = startTime,
                StartMeterValue = startMeter,
                PricePerKwh = pricePerKwh,
                PricePerMinute = pricePerMinute,
                FlatFee = flatFee
            };
            
            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();
            return tx;
        }

        public async Task UpdateTransactionAsync(int transactionId, DateTime stopTime, int stopMeter, string stopReason)
        {
            var tx = await _context.Transactions.FindAsync(transactionId);
            if (tx != null)
            {
                tx.StopTime = stopTime;
                tx.StopMeterValue = stopMeter;
                tx.StopReason = stopReason;
                
                decimal energyKwh = Math.Max(0, (stopMeter - tx.StartMeterValue) / 1000m);
                decimal energyCost = energyKwh * tx.PricePerKwh;

                decimal durationMinutes = (decimal)(stopTime - tx.StartTime).TotalMinutes;
                decimal timeCost = Math.Max(0, durationMinutes) * tx.PricePerMinute;

                tx.TotalCost = energyCost + timeCost + tx.FlatFee;
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetLatestMeterValueAsync(int transactionId)
        {
            var latest = await _context.MeterValues
                .Where(m => m.TransactionId == transactionId && m.Measurand == "Energy.Active.Import.Register")
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            if (latest != null && int.TryParse(latest.Value, out int meterValue))
            {
                return meterValue;
            }

            var tx = await _context.Transactions.FindAsync(transactionId);
            return tx?.StartMeterValue ?? 0;
        }

        public async Task SaveMeterValueAsync(MeterValue meterValue)
        {
            _context.MeterValues.Add(meterValue);
            await _context.SaveChangesAsync();
        }
    }
}
