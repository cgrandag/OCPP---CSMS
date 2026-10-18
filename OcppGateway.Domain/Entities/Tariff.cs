using System;
using System.Collections.Generic;

namespace OcppGateway.Domain
{
    public class Tariff
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PricePerKwh { get; set; }
        public decimal PricePerMinute { get; set; }
        public decimal FlatFee { get; set; }

        public ICollection<Charger> Chargers { get; set; } = new List<Charger>();
    }
}
