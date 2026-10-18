using System;
using System.Collections.Generic;

namespace OcppGateway.Domain
{
    public class Charger
    {
        public string ChargePointId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";
        public string LatestErrorCode { get; set; } = "NoError";
        public string LatestInfo { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
        public bool IsOnline { get; set; }
        public string Location { get; set; } = "Sede Principal";
        public string? PhysicalIdentifier { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? Model { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? ConnectorType { get; set; }
        public decimal? CurrentPowerKw { get; set; }

        public int? Rssi { get; set; }
        public string? NetworkType { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        
        public int? TariffId { get; set; }
        public Tariff? Tariff { get; set; }
    }
}
