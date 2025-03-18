using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /// <summary>
    /// Model for user registration requests.
    /// </summary>
    public class RegisterModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// Model for user login requests.
    /// </summary>
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }

    /// <summary>
    /// Response model for successful authentication.
    /// </summary>
    public class AuthResponse
    {
        [Required]
        public string Token { get; set; } = null!; // JWT token

        [Required]
        public DateTime Expires { get; set; } // Token expiration time
        [Required]
        public string RefreshToken { get; set; } = null!; // Refresh token
    }
    public class RefreshToken
    {
        public string Token { get; set; } = null!; // The refresh token string
        public DateTime Expires { get; set; } // When it expires
        public bool IsRevoked { get; set; } // For revocation
        public DateTime Created { get; set; } // When it was issued
    }

    public class RefreshTokenRequest{
        public string AccessToken { get ; set ;} = null! ;
        public string RefreshToken { get ; set ;} = null! ;
    }
}