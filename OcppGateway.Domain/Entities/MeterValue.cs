using System;

namespace OcppGateway.Domain
{
    public class MeterValue
    {
        public long Id { get; set; }
        public string ChargePointId { get; set; } = string.Empty;
        public int TransactionId { get; set; }
        public int ConnectorId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Measurand { get; set; } = "Energy.Active.Import.Register";
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = "Wh";
    }
}
