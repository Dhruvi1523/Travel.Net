using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BookingModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BookingId { get; set; }

    // User Info
    [Required(ErrorMessage = "UserId is required")]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    // Flight Info
    [Required(ErrorMessage = "FlightId is required")]
    public int FlightId { get; set; }

    [ForeignKey("FlightId")]
    public Flight Flight { get; set; }

    // Passenger Info
    [Required(ErrorMessage = "Passenger name is required")]
    [StringLength(100, ErrorMessage = "Passenger name cannot exceed 100 characters")]
    public string PassengerName { get; set; }

    [Required(ErrorMessage = "ID proof type is required")]
    [StringLength(50, ErrorMessage = "ID proof type cannot exceed 50 characters")]
    public string IdProofType { get; set; } // e.g., Aadhar, PAN, Driving License

    [Required(ErrorMessage = "ID proof number is required")]
    [StringLength(30, ErrorMessage = "ID proof number cannot exceed 30 characters")]
    public string IdProofNumber { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
    public string Gender { get; set; }

    // Booking Details
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime BookingDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Seat number is required")]
    [StringLength(10, ErrorMessage = "Seat number cannot exceed 10 characters")]
    public string SeatNumber { get; set; }

    [Required(ErrorMessage = "Class is required")]
    [StringLength(20, ErrorMessage = "Class cannot exceed 20 characters")]
    public string Class { get; set; } // Economy, Business, etc.

    [Required]
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    public string Status { get; set; } = "Pending"; // Confirmed, Cancelled, etc.

    // Payment Info
    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
    public decimal Price { get; set; }

    [Required]
    public bool IsPaid { get; set; } = false;

    [Required(ErrorMessage = "Payment method is required")]
    [StringLength(20, ErrorMessage = "Payment method cannot exceed 20 characters")]
    public string PaymentMethod { get; set; } // UPI, Card, NetBanking, etc.

    // Optional
    [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
    public string SpecialRequests { get; set; }
}
