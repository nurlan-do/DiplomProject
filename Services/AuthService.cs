using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DiplomBackend.Data;
using DiplomBackend.Models;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;

namespace DiplomBackend.Services
{
    public class AuthService
    {
        private const string SecretKey = "SuperSecretKeyForDiplomBackendProject2026@"; // Минимум 32 символа

        public AuthResponseDto? Register(RegisterDto dto)
        {
            if (DataStorage.Users.Any(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return null; // Пользователь уже существует
            }

            var user = new User
            {
                Id = DataStorage.Users.Count + 1,
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = HashPassword(dto.Password),
                City = null
            };

            DataStorage.Users.Add(user);
            var token = GenerateJwtToken(user);

            return new AuthResponseDto { Token = token, Email = user.Email, FullName = user.FullName };
        }

        public AuthResponseDto? Login(LoginDto dto)
        {
            var user = DataStorage.Users.FirstOrDefault(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null || user.PasswordHash != HashPassword(dto.Password))
            {
                return null; // Неверный логин или пароль
            }

            var token = GenerateJwtToken(user);
            return new AuthResponseDto { Token = token, Email = user.Email, FullName = user.FullName };
        }

        public bool UpdateUserCity(string email, string city)
        {
            var user = DataStorage.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;

            user.City = city;
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken)
        {
            try
            {
                // Проверяем валидность токена от Google
                var settings = new GoogleJsonWebSignature.ValidationSettings();
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                var email = payload.Email;
                var name = payload.Name ?? email;

                // Ищем пользователя в нашей памяти
                var user = DataStorage.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    // Если пользователя нет — автоматически регистрируем
                    user = new User
                    {
                        Id = DataStorage.Users.Count + 1,
                        Email = email,
                        FullName = name,
                        PasswordHash = string.Empty, // Пароль не нужен при входе через соцсети
                        City = null
                    };
                    DataStorage.Users.Add(user);
                }

                // Генерируем наш стандартный JWT токен
                var token = GenerateJwtToken(user);
                return new AuthResponseDto { Token = token, Email = user.Email, FullName = user.FullName };
            }
            catch (Exception)
            {
                return null; // Токен недействителен
            }
        }
    }
}
