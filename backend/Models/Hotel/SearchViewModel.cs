using System.ComponentModel.DataAnnotations;

 namespace backend.Hotel.Models
{
    public class SearchViewModel
    {
        [Required(ErrorMessage = "Check-in date is required")]
        [Display(Name = "Check-In")]
        public DateTime CheckIn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Check-out date is required")]
        [Display(Name = "Check-Out")]
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Please select number of rooms")]
        [Range(1, 10, ErrorMessage = "Number of rooms must be between 1 and 10")]
        [Display(Name = "Rooms")]
        public int Rooms { get; set; } = 1;

        [Required(ErrorMessage = "Please select number of adults")]
        [Range(1, 10, ErrorMessage = "Number of adults must be between 1 and 10")]
        [Display(Name = "Adults")]
        public int Adults { get; set; } = 2;

        [Display(Name = "Children")]
        public int Children { get; set; } = 0;

        [Display(Name = "Price Range")]
        public string? PriceRange { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;
    }
}