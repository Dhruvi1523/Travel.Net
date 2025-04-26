namespace backend.Package.Models
{
    public class DayPlan
    {
        public string Day { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Meal { get; set; } = string.Empty;
        public string? Transfer { get; set; }
    }

    public class TourPackage
    {
        public string Id { get; set; } = string.Empty; // "kerala", "shimla", etc.
        public string Title { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Itinerary { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public string DiscountLabel { get; set; } = string.Empty;
        public string Hotel { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<DayPlan> DayPlan { get; set; } = new();
        public string ShortDescription { get; set; } = string.Empty;

    }
}