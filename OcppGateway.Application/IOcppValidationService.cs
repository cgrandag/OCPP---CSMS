using System.Threading.Tasks;

namespace OcppGateway.Application
{
    public interface IOcppValidationService
    {
        Task<bool> IsIdTagAuthorizedAsync(string idTag);
    }
}
