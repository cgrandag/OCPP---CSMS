using System.Threading.Tasks;

namespace OcppGateway.Domain
{
    public interface IChargerControlService
    {
        Task SendStartTransactionAsync(string chargePointId);
        Task SendStopTransactionAsync(string chargePointId, int transactionId);
    }
}