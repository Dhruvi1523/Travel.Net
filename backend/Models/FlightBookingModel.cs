using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    public class FlightBookingModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // MongoDB document ID

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string FlightClass { get; set; }

        [Required]
        public string AgentId { get; set; }

        [Required]
        public string ItineraryId { get; set; }

        [Required]
        public string BookingId { get; set; }

        
        public string? FlightDate {get; set;}

        
        public string? Destination {get; set;}

        
        public string? Origin { get; set; }

        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? UserId { get; set; }

        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? PaymentStatus { get; set; } = "Pending";
    }
}
