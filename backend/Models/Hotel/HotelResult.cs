namespace backend.Hotel.Models
{
    public class HotelResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new List<string>();
        public decimal TotalPrice { get; set; }
        public string? ReviewScore { get; set; }
        public int? ReviewCount { get; set; }
    }
}