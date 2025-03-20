using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    /// <summary>
    /// Represents a user stored in MongoDB.
    /// </summary>
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // MongoDB will generate this

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!; // Hashed password

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!; // Ensures valid email format
        public RefreshToken RefreshToken { get; set; }    // Store multiple refresh tokens
    }
}