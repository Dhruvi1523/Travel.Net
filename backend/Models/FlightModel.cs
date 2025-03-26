

namespace backend.Models
{
    public class Airport
    {

        public string AirportEntityId { get; set; }
        public string AirportName { get; set; }
        public string DisplayCode { get; set; }
        public string CityName { get; set; }
     
    }

    public class Carrier{
        public string CarrierId {get; set; }
        public string AlternatedId { get; set; }
        public string CarrierName { get; set; }
        public string CarrierLogo { get; set; }
        public string DisplayCode {get; set; }
        public string DisplayCodeType { get; set; }
        public string BrandColour { get; set; }
    }

    public class Leg{
        public string LegId { get; set;}
        public string FlightNumber { get; set; }
        public Airport Origin { get; set; }
        public Airport Destination { get; set;}
        public Carrier MarketingCarrier { get; set; }
        public Carrier OperatingCarrier { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }

        public int StopCount { get; set; }
        public string StopDescription => StopCount == 0 ? "Non Stop" : $"{StopCount} Stop{(StopCount > 1 ? "s" : "")}";
        public string Duration { get; set; }

        public int DayChange { get; set; } = 0 ;

    }

    public class BookingAgent{
        public string Id { get; set; }
        public string AgentId { get; set; }
        public string Name { get; set; }
        public string BookingProposition { get; set; }

        public string BookingUrl { get; set; }
        public string Price {get; set;}

        public string RatingValue { get; set; }
        public string RatingCount { get; set; }

    }
}