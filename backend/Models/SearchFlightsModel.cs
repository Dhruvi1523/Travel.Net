using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class SearchFlightsRequest
    {
        [Required(ErrorMessage = "Origin Sky ID is required.")]
        public string OriginSkyId { get; set; } = null!;

        [Required(ErrorMessage = "Destination Sky ID is required.")]
        public string DestinationSkyId { get; set; } = null!;

        [Required(ErrorMessage = "Origin Entity ID is required.")]
        public string OriginEntityId { get; set; } = null!;

        [Required(ErrorMessage = "Destination Entity ID is required.")]
        public string DestinationEntityId { get; set; } = null!;

        [Required(ErrorMessage = "Departure date is required.")]
        public string Date { get; set; } = null!;

        public string? ReturnDate { get; set; }

        [Required(ErrorMessage = "Cabin class is required.")]
        public string CabinClass { get; set; } = "economy";

        [Range(1, int.MaxValue, ErrorMessage = "At least one adult is required.")]
        public int Adults { get; set; } = 1;

        [Range(0, int.MaxValue, ErrorMessage = "Number of children cannot be negative.")]
        public int Children { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Number of infants cannot be negative.")]
        public int Infants { get; set; } = 0;

        public string SortBy { get; set; } = "best";

        [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000.")]
        public int Limit { get; set; } = 100;

        public List<int>? CarriersIds { get; set; }
    }

    public class SearchFlightResult
    {
        public string Id { get; set; } = null!;
        public string SessionId { get; set; } = null!;

        public Leg OutboundLeg { get; set; } = null!;
        public Leg ReturnLeg { get; set; } = null!;

        public string Price { get; set; } = null!;
        public string PricingOptionId { get; set; } = null!;

        public bool IsFreeCancellation { get; set; }
        public bool IsChangeAllowed { get; set; }
        public bool IsSelfTransferAllowed { get; set; }
        public bool IsPartiallyRefundable { get; set; }

        public List<string> Tags { get; set; } = new();
        public double Score { get; set; }
    }

    public class FlightLeg
    {
        public string LegId { get; set; } = null!;
        public string FlightNumber { get; set; } = null!;
        public string AirlineName { get; set; } = null!;
        public string AirlineLogo { get; set; } = null!;

        public string OriginAirportCode { get; set; } = null!;
        public string OriginCityName { get; set; } = null!;
        public string DestinationAirportCode { get; set; } = null!;
        public string DestinationCityName { get; set; } = null!;

        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }

        public int StopCount { get; set; }
        public string StopDescription =>
            StopCount == 0 ? "Non Stop" : $"{StopCount} Stop{(StopCount > 1 ? "s" : "")}";

        public string Duration { get; set; } = null!;
    }
}
