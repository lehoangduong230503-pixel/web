using ChocoLux.Models;

namespace ChocoLux.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<User?> GetActiveUserWithRoleAsync(string email, string hashedPassword);
        Task<List<ShippingZone>> GetShippingZonesAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<Role> GetCustomerRoleAsync();
        void AddUser(User user);
        void AddAddress(Address address);
        Task SaveChangesAsync();
        Task<User?> GetUserProfileAsync(int userId);
        Task<User?> GetUserWithRoleAsync(int userId);
        Task<bool> HasAnyAddressAsync(int userId);
        Task<Address?> GetAddressByUserAsync(int userId, int addressId);
        void RemoveAddress(Address address);
        Task<Address?> GetFirstAddressAsync(int userId);
        Task<List<Address>> GetAddressesByUserIdAsync(int userId);
        Task<List<Address>> GetAddressesWithCityByUserIdAsync(int userId);
        Task<User?> GetUserByIdAsync(int userId);
    }
}
