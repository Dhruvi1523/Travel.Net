using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var user = await _authService.RegisterAsync(model);
                _logger.LogInformation("User {Username} registered via API", user.Username);
                return Ok("Registration Successfully..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user with username {Username}", model.Username);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var authResponse = await _authService.LoginAsync(model);
                _logger.LogInformation("User {Username} logged in via API", model.Username);

                Response.Cookies.Append("TravelAccessToken", authResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = authResponse.Expires
                });

                Response.Cookies.Append("TravelRefreshToken", authResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                return Ok("Login Successfully...");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for {Username}: {Message}", model.Username, ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user {Username}", model.Username);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var accessToken = Request.Cookies["TravelAccessToken"];
                var refreshToken = Request.Cookies["TravelRefreshToken"];
                var authResponse = await _authService.RefreshTokenAsync(accessToken, refreshToken);
                _logger.LogInformation("Token refreshed via API");
                Response.Cookies.Append("TravelAccessToken", authResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = authResponse.Expires
                });

                Response.Cookies.Append("TravelRefreshToken", authResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                return Ok("Refresh Completed...");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Refresh failed: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpGet("me")]
        // [Authorize] // Protects this endpoint
        public async Task<IActionResult> GetUserDetails()
        {
            try
            {
                // Get username from JWT claims
                var username = User?.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("No username found in token");
                    return Unauthorized("Invalid token");
                }

                // Fetch user from MongoDB via AuthService
                var user = await _authService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    return NotFound("User not found");
                }

                _logger.LogInformation("User details retrieved for {Username}", username);
                return Ok(new
                {
                    user.Username,
                    user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}