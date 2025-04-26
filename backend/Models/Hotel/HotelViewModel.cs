 namespace backend.Hotel.Models
{
    public class HotelViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public decimal PricePerNight { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int Discount { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Location{get;set;} = string.Empty;
    }
}
