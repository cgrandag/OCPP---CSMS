using System.Collections.Generic;
using System.Threading.Tasks;
using OcppGateway.Domain.Entities;

namespace OcppGateway.Domain
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByIdTagAsync(string idTag);
        Task<User> CreateUserAsync(User user);
        Task<User?> UpdateUserAsync(int id, User userDetails);
        Task<bool> DeleteUserAsync(int id);
    }
}
