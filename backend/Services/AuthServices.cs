using MongoDB.Driver;
using BCrypt.Net;
using backend.Models;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver.Linq;

namespace backend.Services
{
    /// <summary>
    /// Defines the contract for authentication-related operations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user with the provided registration details.
        /// </summary>
        /// <param name="model">The registration details including username, password, and email.</param>
        /// <returns>The newly registered user.</returns>
        Task<User> RegisterAsync(RegisterModel model);

        /// <summary>
        /// Authenticates a user and generates access and refresh tokens.
        /// </summary>
        /// <param name="model">The login credentials including username and password.</param>
        /// <returns>An authentication response with tokens and expiration details.</returns>
        Task<AuthResponse> LoginAsync(LoginModel model);

        /// <summary>
        /// Refreshes an expired access token using a refresh token.
        /// </summary>
        /// <param name="accessToken">The expired access token to refresh.</param>
        /// <param name="refreshToken">The refresh token used to authenticate the request.</param>
        /// <returns>A new authentication response with updated tokens.</returns>
        Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken);

        /// <summary>
        /// Retrieves a user by their username from the database.
        /// </summary>
        /// <param name="username">The username of the user to retrieve.</param>
        /// <returns>The user object, or null if not found.</returns>
        Task<User> GetUserByUsernameAsync(string username);
    }

    /// <summary>
    /// Implements authentication-related operations, including user registration, login, token refresh, and user retrieval.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IMongoDbService _mongoDbService;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class with dependency injection.
        /// </summary>
        /// <param name="mongoDbService">The service for interacting with MongoDB.</param>
        /// <param name="tokenService">The service for generating and validating tokens.</param>
        /// <param name="configuration">The configuration settings for JWT and other parameters.</param>
        /// <param name="logger">The logger for recording service events.</param>
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

        /// <summary>
        /// Registers a new user in the system, hashing their password and storing their details.
        /// </summary>
        /// <param name="model">The registration details including username, password, and email.</param>
        /// <returns>The newly created user object.</returns>
        /// <exception cref="Exception">Thrown if the username or email already exists.</exception>
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

        /// <summary>
        /// Authenticates a user with their credentials and issues access and refresh tokens.
        /// </summary>
        /// <param name="model">The login credentials including username and password.</param>
        /// <returns>An authentication response containing access token, refresh token, and expiration.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the username or password is invalid.</exception>
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

            user.RefreshTokens.Add(
                new RefreshToken
                {
                    Token = refreshToken,
                    Expires = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false,
                    Created = DateTime.UtcNow
                });

            await _mongoDbService.User.ReplaceOneAsync(u => u.Id == user.Id, user);

            return new AuthResponse
            {
                Token = accessToken,
                Expires = expiry,
                RefreshToken = refreshToken
            };
        }

        /// <summary>
        /// Refreshes an expired access token using a valid refresh token, issuing new tokens.
        /// </summary>
        /// <param name="accessToken">The expired access token to refresh.</param>
        /// <param name="refreshToken">The refresh token to validate the request.</param>
        /// <returns>A new authentication response with updated tokens.</returns>
        /// <exception cref="SecurityTokenException">Thrown if the access token or refresh token is invalid.</exception>
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

            var storedRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);

            if (storedRefreshToken == null || storedRefreshToken.IsRevoked || storedRefreshToken.Expires <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh failed: Invalid or expired refresh token for {Username}", username);
                throw new SecurityTokenException("Invalid refresh token");
            }

            var newAccessToken = _tokenService.GenerateAccessToken(username);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"]));

            user.RefreshTokens.Remove(storedRefreshToken);
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                IsRevoked = false
            });

            await _mongoDbService.User.ReplaceOneAsync(u => u.Id == user.Id, user);
            _logger.LogInformation("Token refreshed for user {Username}", username);

            return new AuthResponse
            {
                Token = newAccessToken,
                Expires = expiry,
                RefreshToken = newRefreshToken
            };
        }

        /// <summary>
        /// Retrieves a user from the database by their username.
        /// </summary>
        /// <param name="username">The username of the user to find.</param>
        /// <returns>The user object if found, otherwise null.</returns>
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var user = await _mongoDbService.User
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();

            return user; // Returns null if not found
        }
    }
}