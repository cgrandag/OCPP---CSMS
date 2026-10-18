using System;

namespace OcppGateway.Domain
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public string ChargePointId { get; set; } = string.Empty;
        public int ConnectorId { get; set; }
        public string IdTag { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public int StartMeterValue { get; set; }
        public int StopMeterValue { get; set; }
        public string StopReason { get; set; } = string.Empty;

        public decimal PricePerKwh { get; set; } = 0.30m;
        public decimal PricePerMinute { get; set; } = 0m;
        public decimal FlatFee { get; set; } = 0m;
        public decimal TotalCost { get; set; }

        public Charger Charger { get; set; } = null!;
    }
}
