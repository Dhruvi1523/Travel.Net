using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
  
   
    /// <summary>
    /// Represents the request data for searching flights using the Sky Scrapper API.
    /// </summary>
    public class SearchFlightsRequest
    {
        /// <summary>
        /// The Sky ID of the origin (e.g., "LOND" for London).
        /// </summary>
        [Required(ErrorMessage = "Origin Sky ID is required.")]
        public string OriginSkyId { get; set; }

        /// <summary>
        /// The Sky ID of the destination (e.g., "NYCA" for New York).
        /// </summary>
        [Required(ErrorMessage = "Destination Sky ID is required.")]
        public string DestinationSkyId { get; set; }

        /// <summary>
        /// The entity ID of the origin (e.g., "27544008").
        /// </summary>
        [Required(ErrorMessage = "Origin Entity ID is required.")]
        public string OriginEntityId { get; set; }

        /// <summary>
        /// The entity ID of the destination (e.g., "27537542").
        /// </summary>
        [Required(ErrorMessage = "Destination Entity ID is required.")]
        public string DestinationEntityId { get; set; }

        /// <summary>
        /// The departure date in YYYY-MM-DD format (e.g., "2025-03-28").
        /// </summary>
        [Required(ErrorMessage = "Departure date is required.")]
        public string Date { get; set; }

        /// <summary>
        /// The return date in YYYY-MM-DD format (e.g., "2025-04-05"). Optional for one-way trips.
        /// </summary>
        public string ReturnDate { get; set; }

        /// <summary>
        /// The cabin class (e.g., "economy", "business").
        /// </summary>
        [Required(ErrorMessage = "Cabin class is required.")]
        public string CabinClass { get; set; } ="economy";

        /// <summary>
        /// The number of adult passengers (minimum 1).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "At least one adult is required.")]
        public int Adults { get; set; } = 1;

        /// <summary>
        /// The number of child passengers (0 or more).
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Number of children cannot be negative.")]
        public int Children { get; set; } = 0;

        /// <summary>
        /// The number of infant passengers (0 or more).
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Number of infants cannot be negative.")]
        public int Infants { get; set; } = 0;

        /// <summary>
        /// The sorting preference for results (e.g., "best", "price", "duration").
        /// </summary>
        public string SortBy { get; set; } = "best";

        /// <summary>
        /// The maximum number of results to return (default 100).
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000.")]
        public int Limit { get; set; } = 100;

        /// <summary>
        /// The list of carrier IDs to filter results (e.g., [32672]).
        /// </summary>
        public List<int> CarriersIds { get; set; }

    }

   
    /// <summary>
    /// Represents a simplified flight search result for the frontend.
    /// </summary>
    public class SearchFlightResult
    {
        /// <summary>
        /// The unique identifier of the itinerary.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The session ID associated with the flight search.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The name of the airline (e.g., "IndiGo").
        /// </summary>
        public string AirlineName { get; set; }

        /// <summary>
        /// Details of the outbound flight leg.
        /// </summary>
        public FlightLeg OutboundLeg { get; set; }

        /// <summary>
        /// Details of the return flight leg.
        /// </summary>
        public FlightLeg ReturnLeg { get; set; }

        /// <summary>
        /// The total price of the itinerary, formatted with currency (e.g., "₹12,010").
        /// </summary>
        public string Price { get; set; }

        /// <summary>
        /// Indicates whether free cancellation is allowed.
        /// </summary>
        public bool IsFreeCancellation { get; set; }
    }

    /// <summary>
    /// Represents a single flight leg (outbound or return).
    /// </summary>
    public class FlightLeg
    {
        /// <summary>
        /// The departure date in a user-friendly format (e.g., "2025-03-27").
        /// </summary>
        public string DepartureDate { get; set; }

        /// <summary>
        /// The departure time in a user-friendly format (e.g., "09:45 PM").
        /// </summary>
        public string DepartureTime { get; set; }

        /// <summary>
        /// The arrival date in a user-friendly format (e.g., "2025-03-28").
        /// </summary>
        public string ArrivalDate { get; set; }

        /// <summary>
        /// The arrival time in a user-friendly format (e.g., "12:30 AM").
        /// </summary>
        public string ArrivalTime { get; set; }

        /// <summary>
        /// The number of stops (e.g., 0 for "Non Stop").
        /// </summary>
        public int StopCount { get; set; }

        /// <summary>
        /// A user-friendly description of stops (e.g., "Non Stop").
        /// </summary>
        public string StopDescription => StopCount == 0 ? "Non Stop" : $"{StopCount} Stop{(StopCount > 1 ? "s" : "")}";

        /// <summary>
        /// The duration of the flight in a user-friendly format (e.g., "2h 55m").
        /// </summary>
        public string Duration { get; set; }
    }

    
}
