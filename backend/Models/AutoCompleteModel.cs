
namespace backend.Models{
    /// <summary>
    /// Represents a single autocomplete result for airport or city suggestions.
    /// </summary>
    public class AutocompleteResult
    {
        /// <summary>
        /// The display label for the autocomplete dropdown (e.g., "New York Newark (EWR)").
        /// </summary>
        public required string Label { get; set; }

        /// <summary>
        /// The subtitle providing additional context (e.g., "United States").
        /// </summary>
        public required string Subtitle { get; set; }

        /// <summary>
        /// The value object containing detailed data for the selected entity.
        /// </summary>
        public required AutocompleteValue Value { get; set; }
    }

    /// <summary>
    /// Represents the detailed data for a selected airport or city.
    /// </summary>
    public class AutocompleteValue
    {
        /// <summary>
        /// The IATA code or city code (e.g., "EWR").
        /// </summary>
        public required string SkyId { get; set; }

        /// <summary>
        /// The unique entity identifier (e.g., "95565059").
        /// </summary>
        public required string EntityId { get; set; }

        /// <summary>
        /// The type of entity (e.g., "AIRPORT" or "CITY").
        /// </summary>
        public required string EntityType { get; set; }

        /// <summary>
        /// The localized name of the entity (e.g., "New York Newark").
        /// </summary>
        public required string LocalizedName { get; set; }
    }
}