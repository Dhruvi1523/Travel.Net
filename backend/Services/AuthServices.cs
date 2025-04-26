using MongoDB.Driver;
using BCrypt.Net;
using backend.Models;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver.Linq;

namespace backend.Services
{

    public interface IAuthService
    {
        Task<User> RegisterAsync(RegisterModel model);
        Task<AuthResponse> LoginAsync(LoginModel model);
        Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<User> GetUserByUsernameAsync(string username);
    }


    public class AuthService : IAuthService
    {
        private readonly IMongoDbService _mongoDbService;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IMongoDbService mongoDbService,
            ITokenService tokenService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _mongoDbService = mongoDbService;
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<User> RegisterAsync(RegisterModel model)
        {
            var existingUser = await _mongoDbService.User
                .Find(u => u.Username == model.Username || u.Email == model.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
            {
                _logger.LogWarning("Registration Failed: Username {Username} or Email {Email} already Exist", model.Username, model.Email);
                throw new Exception("Username Already Exist..");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash
            };

            await _mongoDbService.User.InsertOneAsync(newUser);

            _logger.LogInformation("User {newUser} Registered Successfully...", model.Username);

            return newUser;
        }

        public async Task<AuthResponse> LoginAsync(LoginModel model)
        {
            var user = await _mongoDbService.User
                .Find(u => u.Username == model.Username)
                .FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login Failed: Username {Username} not found.", model.Username);
                throw new UnauthorizedAccessException("Invalid Username or Password");
            }

            _logger.LogInformation("User {user} Logged Successfully...", model.Username);

            var accessToken = _tokenService.GenerateAccessToken(model.Username);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"]));

            user.RefreshToken =
                new RefreshToken
                {
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false,
                    Created = DateTime.UtcNow
                };

            await _mongoDbService.User.ReplaceOneAsync(u => u.Id == user.Id, user);

            return new AuthResponse
            {
                Token = accessToken,
                Expires = expiry,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
            var username = principal?.Identity?.Name;

            var user = await _mongoDbService.User
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Refresh failed: User {Username} not found", username);
                throw new SecurityTokenException("Invalid token");
            }


            if (user.RefreshToken == null || user.RefreshToken.IsRevoked || user.RefreshToken.Expires <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh failed: Invalid or expired refresh token for {Username}", username);
                throw new SecurityTokenException("Invalid refresh token");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(username);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"]));


            user.RefreshToken = new RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                IsRevoked = false
            };

            await _mongoDbService.User.ReplaceOneAsync(u => u.Id == user.Id, user);
            _logger.LogInformation("Token refreshed for user {Username}", username);

            return new AuthResponse
            {
                Token = newAccessToken,
                Expires = expiry,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var user = await _mongoDbService.User
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();

            return user; // Returns null if not found
        }
    }
}