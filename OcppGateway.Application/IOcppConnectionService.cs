using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcppGateway.Application
{
    public interface IOcppConnectionService
    {
        Task<string?> ProcessMessageAsync(string chargePointId, string messageRaw);
    }
}
