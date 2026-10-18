using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcppGateway.Domain
{
    public class OcppMessage
    {
        public string ChargePointId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
