using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OcppGateway.Domain;

namespace OcppGateway.Application
{
    public class OcppValidationService : IOcppValidationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OcppValidationService> _logger;

        public OcppValidationService(IUserRepository userRepository, ILogger<OcppValidationService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> IsIdTagAuthorizedAsync(string idTag)
        {
            if (string.IsNullOrWhiteSpace(idTag))
            {
                _logger.LogWarning("Validation Failed: Empty or null IdTag provided.");
                return false;
            }

            var user = await _userRepository.GetUserByIdTagAsync(idTag);
            
            if (user == null)
            {
                _logger.LogWarning("Validation Failed: User with IdTag '{IdTag}' not found.", idTag);
                return false;
            }

            if (!user.IsAuthorized)
            {
                _logger.LogWarning("Validation Failed: User '{Username}' (IdTag: {IdTag}) is not authorized.", user.Username, idTag);
                return false;
            }

            _logger.LogInformation("Validation Success: User '{Username}' (IdTag: {IdTag}) is authorized.", user.Username, idTag);
            return true;
        }
    }
}
