using System.ComponentModel.DataAnnotations;

namespace backend.Models
{


    public class SearchFlightsRequest
    {
      
        [Required(ErrorMessage = "Origin Sky ID is required.")]
        public string OriginSkyId { get; set; }

        
        [Required(ErrorMessage = "Destination Sky ID is required.")]
        public string DestinationSkyId { get; set; }

        
        [Required(ErrorMessage = "Origin Entity ID is required.")]
        public string OriginEntityId { get; set; }

       
        [Required(ErrorMessage = "Destination Entity ID is required.")]
        public string DestinationEntityId { get; set; }

        [Required(ErrorMessage = "Departure date is required.")]
        public string Date { get; set; }

       
        public string ReturnDate { get; set; }

       
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

      
        public List<int> CarriersIds { get; set; }

    }

    public class SearchFlightResult
    {
        public string Id { get; set; }
        public string SessionId { get; set; }

        public Leg OutboundLeg { get; set; }
        public Leg ReturnLeg { get; set; }

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
