using System.ComponentModel.DataAnnotations;

namespace backend.Models{
    
    public class GetFlightDetailsRequest{
      
        [Required(ErrorMessage = "ItineraryId is required.")]
        public string ItineraryId { get; set; }

        [Required(ErrorMessage = "Legs are required.")]
        [MinLength(1, ErrorMessage = "At least one leg is required.")]
        public List<RequestLeg> Legs { get; set; }

        [Required(ErrorMessage = "SessionId is required.")]
        public string SessionId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "At least one adult is required.")]
        public int Adults { get; set; } = 1; // Default value: 1

        [Range(0, int.MaxValue, ErrorMessage = "Children cannot be negative.")]
        public int Children { get; set; } = 0; // Default value: 0

        [Range(0, int.MaxValue, ErrorMessage = "Infants cannot be negative.")]
        public int Infants { get; set; } = 0; // Default value: 0
        
        public string CabinClass { get; set; }
    }

   
    public class RequestLeg
    {
        [Required(ErrorMessage = "Destination is required for each leg.")]
        public string Destination { get; set; }
        
        [Required(ErrorMessage = "Origin is required for each leg.")]
        public string Origin { get; set; }
       
        [Required(ErrorMessage = "Date is required for each leg.")]
        public string Date { get; set; }
    }


    public class GetFlightDetailsResponse {

        public string ItineraryId { get; set; }
        public Airport Origin {get; set; }
        public Airport Destination {get; set; }
        public List<Leg> legs{ get; set; }
        public List<BookingAgent> bookingAgents{ get; set; }
        public string DestinationImage {get; set; }
        public string BookingSessionId { get; set; }
    }
}