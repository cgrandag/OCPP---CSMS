using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OcppGateway.Domain;
using OcppGateway.Domain.Entities;

namespace OcppGateway.Infrastructure.Persistence
{
    public class PostgresUserRepository : IUserRepository
    {
        private readonly PostgresDbContext _context;

        public PostgresUserRepository(PostgresDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Tariff)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByIdTagAsync(string idTag)
        {
            return await _context.Users
                .Include(u => u.Tariff)
                .FirstOrDefaultAsync(u => u.IdTag == idTag);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateUserAsync(int id, User userDetails)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            user.Username = userDetails.Username;
            user.Email = userDetails.Email;
            user.Role = userDetails.Role;
            user.PasswordHash = userDetails.PasswordHash;

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
