 
 namespace backend.Hotel.Models;
 public class BookingViewModel
 {
     public int HotelId { get; set; }
     public DateTime CheckIn { get; set; }
     public DateTime CheckOut { get; set; }
     public int Guests { get; set; }
 }