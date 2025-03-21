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
        public string CabinClass { get; set; } = "economy";

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

    public class SearchFlightResult
    {
        public string Id { get; set; }
        public string SessionId { get; set; }

        public FlightLeg OutboundLeg { get; set; }
        public FlightLeg ReturnLeg { get; set; }

        public string Price { get; set; }
        public string PricingOptionId { get; set; }

        public bool IsFreeCancellation { get; set; }
        public bool IsChangeAllowed { get; set; }
        public bool IsSelfTransferAllowed { get; set; }
        public bool IsPartiallyRefundable { get; set; }

        public List<string> Tags { get; set; }
        public double Score { get; set; }
    }

    public class FlightLeg
    {
        public string LegId { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string AirlineLogo { get; set; }

        public string OriginAirportCode { get; set; }
        public string OriginCityName { get; set; }
        public string DestinationAirportCode { get; set; }
        public string DestinationCityName { get; set; }

        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }

        public int StopCount { get; set; }
        public string StopDescription => StopCount == 0 ? "Non Stop" : $"{StopCount} Stop{(StopCount > 1 ? "s" : "")}";

        public string Duration { get; set; }

      
    }

   


}
