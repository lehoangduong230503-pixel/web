using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChocoLux.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ChocoLuxContext _db;

        public AccountRepository(ChocoLuxContext db)
        {
            _db = db;
        }

        public Task<User?> GetActiveUserWithRoleAsync(string email, string hashedPassword)
        {
            return _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == hashedPassword && u.IsActive);
        }

        public Task<List<ShippingZone>> GetShippingZonesAsync()
        {
            return _db.ShippingZones.OrderBy(s => s.CityName).ToListAsync();
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            return _db.Users.AnyAsync(u => u.Email == email);
        }

        public Task<Role> GetCustomerRoleAsync()
        {
            return _db.Roles.FirstAsync(r => r.RoleName == "CUSTOMER");
        }

        public void AddUser(User user)
        {
            _db.Users.Add(user);
        }

        public void AddAddress(Address address)
        {
            _db.Addresses.Add(address);
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

        public Task<User?> GetUserProfileAsync(int userId)
        {
            return _db.Users
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public Task<User?> GetUserWithRoleAsync(int userId)
        {
            return _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        }

        public Task<bool> HasAnyAddressAsync(int userId)
        {
            return _db.Addresses.AnyAsync(a => a.UserId == userId);
        }

        public Task<Address?> GetAddressByUserAsync(int userId, int addressId)
        {
            return _db.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
        }

        public void RemoveAddress(Address address)
        {
            _db.Addresses.Remove(address);
        }

        public Task<Address?> GetFirstAddressAsync(int userId)
        {
            return _db.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public Task<List<Address>> GetAddressesByUserIdAsync(int userId)
        {
            return _db.Addresses.Where(a => a.UserId == userId).ToListAsync();
        }

        public Task<List<Address>> GetAddressesWithCityByUserIdAsync(int userId)
        {
            return _db.Addresses
                .Include(a => a.City)
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            return _db.Users.FindAsync(userId).AsTask();
        }
    }
}
