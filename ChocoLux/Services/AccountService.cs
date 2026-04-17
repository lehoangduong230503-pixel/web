using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;
using ChocoLux.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace ChocoLux.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public Task<User?> AuthenticateAsync(string email, string password)
        {
            var hashedPw = HashPassword(password);
            return _accountRepository.GetActiveUserWithRoleAsync(email, hashedPw);
        }

        public Task<List<ShippingZone>> GetShippingZonesAsync()
        {
            return _accountRepository.GetShippingZonesAsync();
        }

        public async Task<RegisterResult> RegisterAsync(RegisterViewModel model, int? cityId, string? addressDetail)
        {
            if (await _accountRepository.EmailExistsAsync(model.Email))
            {
                return new RegisterResult { Success = false, Error = "Email này đã được sử dụng" };
            }

            var customerRole = await _accountRepository.GetCustomerRoleAsync();

            var user = new User
            {
                Username = model.Email,
                Password = HashPassword(model.Password),
                FullName = model.FullName,
                Phone = model.PhoneNumber,
                Email = model.Email,
                RoleId = customerRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _accountRepository.AddUser(user);
            await _accountRepository.SaveChangesAsync();

            if (cityId.HasValue && !string.IsNullOrWhiteSpace(addressDetail))
            {
                _accountRepository.AddAddress(new Address
                {
                    UserId = user.Id,
                    CityId = cityId.Value,
                    AddressDetail = addressDetail,
                    IsDefault = true
                });
                await _accountRepository.SaveChangesAsync();
            }

            return new RegisterResult { Success = true };
        }

        public Task<User?> GetProfileAsync(int userId)
        {
            return _accountRepository.GetUserProfileAsync(userId);
        }

        public async Task<User?> UpdateProfileAsync(int userId, User model)
        {
            var user = await _accountRepository.GetUserWithRoleAsync(userId);
            if (user == null) return null;

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.UpdatedAt = DateTime.Now;

            await _accountRepository.SaveChangesAsync();
            return user;
        }

        public async Task AddAddressAsync(int userId, int cityId, string addressDetail)
        {
            var hasAny = await _accountRepository.HasAnyAddressAsync(userId);

            _accountRepository.AddAddress(new Address
            {
                UserId = userId,
                CityId = cityId,
                AddressDetail = addressDetail,
                IsDefault = !hasAny
            });
            await _accountRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAddressAsync(int userId, int addressId)
        {
            var address = await _accountRepository.GetAddressByUserAsync(userId, addressId);
            if (address == null) return false;

            var wasDefault = address.IsDefault;
            _accountRepository.RemoveAddress(address);
            await _accountRepository.SaveChangesAsync();

            if (wasDefault)
            {
                var next = await _accountRepository.GetFirstAddressAsync(userId);
                if (next != null)
                {
                    next.IsDefault = true;
                    await _accountRepository.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            var addresses = await _accountRepository.GetAddressesByUserIdAsync(userId);
            foreach (var address in addresses)
            {
                address.IsDefault = address.Id == addressId;
            }
            await _accountRepository.SaveChangesAsync();
        }

        public async Task<AccountAddressesResult?> GetAddressesAsync(int userId)
        {
            var addresses = await _accountRepository.GetAddressesWithCityByUserIdAsync(userId);
            var user = await _accountRepository.GetUserByIdAsync(userId);
            if (user == null) return null;

            return new AccountAddressesResult
            {
                Addresses = addresses.Select(a => new AccountAddressDto
                {
                    Id = a.Id,
                    AddressDetail = a.AddressDetail,
                    CityName = a.City?.CityName ?? string.Empty,
                    IsDefault = a.IsDefault,
                    FullAddress = a.AddressDetail + ", " + (a.City?.CityName ?? string.Empty)
                }).ToList(),
                FullName = user.FullName ?? string.Empty,
                Phone = user.Phone
            };
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
