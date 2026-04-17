using ChocoLux.Models;
using ChocoLux.ViewModels;

namespace ChocoLux.Services.Interfaces
{
    public interface IAccountService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<List<ShippingZone>> GetShippingZonesAsync();
        Task<RegisterResult> RegisterAsync(RegisterViewModel model, int? cityId, string? addressDetail);
        Task<User?> GetProfileAsync(int userId);
        Task<User?> UpdateProfileAsync(int userId, User model);
        Task AddAddressAsync(int userId, int cityId, string addressDetail);
        Task<bool> DeleteAddressAsync(int userId, int addressId);
        Task SetDefaultAddressAsync(int userId, int addressId);
        Task<AccountAddressesResult?> GetAddressesAsync(int userId);
    }

    public class RegisterResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class AccountAddressDto
    {
        public int Id { get; set; }
        public string AddressDetail { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public string FullAddress { get; set; } = string.Empty;
    }

    public class AccountAddressesResult
    {
        public List<AccountAddressDto> Addresses { get; set; } = new();
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
