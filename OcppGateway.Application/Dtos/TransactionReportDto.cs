using System;

namespace OcppGateway.Application.Dtos
{
    public class TransactionReportDto
    {
        public int TransactionId { get; set; }
        public string ChargePointId { get; set; } = string.Empty;
        public string IdTag { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        
        public string Duration { get; set; } = "Unknown";
        public double TotalKw { get; set; }
        
        public string Status { get; set; } = "Unknown";
        public string StopReason { get; set; } = string.Empty;
    }
}
