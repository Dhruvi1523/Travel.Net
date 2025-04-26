console.log("version 3")

// Initialize Flatpickr for date pickers
document.addEventListener('DOMContentLoaded', () => {
    flatpickr("#departureDate", {
        dateFormat: "Y-m-d",
        minDate: "today",
        theme: "material_red"
    });

    flatpickr("#returnDate", {
        dateFormat: "Y-m-d",
        minDate: "today",
        theme: "material_red"
    });

    // Initially show/hide return date based on flight type
    toggleReturnDate();
    // Initialize travelers input display
    updateTravelersDisplay();
});

// Toggle Return Date visibility based on flight type
function toggleReturnDate() {
    const flightType = document.getElementById('flightType').value;
    const returnDateContainer = document.getElementById('returnDateContainer');
    const returnDateInput = document.getElementById('returnDate');
    if (flightType === 'oneway') {
        returnDateContainer.style.display = 'none';
        returnDateInput.removeAttribute('required');
    } else {
        returnDateContainer.style.display = 'block';
        returnDateInput.setAttribute('required', 'required');
    }
}


// Swap Locations
function swapLocations() {
    const fromInput = document.getElementById('fromInput');
    const toInput = document.getElementById('toInput');
    const tempValue = fromInput.value;
    const tempId = fromInput.dataset.airportId || '';
    fromInput.value = toInput.value;
    fromInput.dataset.airportId = toInput.dataset.airportId || '';
    toInput.value = tempValue;
    toInput.dataset.airportId = tempId;
}

// Autocomplete Functionality
const fromInput = document.getElementById('fromInput');
const toInput = document.getElementById('toInput');
const fromSuggestions = document.getElementById('fromSuggestions');
const toSuggestions = document.getElementById('toSuggestions');

async function fetchAutocompleteSuggestions(query) {
    try {
        // Encode the query to handle special characters or spaces
        const encodedQuery = encodeURIComponent(query);
        // Call the backend API directly using Axios
        const response = await axios.get(`/api/flight/search-airport?query=${encodedQuery}`);
        // Check if the response is successful and has data
        if (response.data.Success && Array.isArray(response.data.Data)) {
            // Map the API response to the expected format: { id, name, city }
            return response.data.Data.map(item => ({
                id: item.Value.SkyId,
                entityId: item.Value.EntityId,
                name: item.Label,     // Use Label as the airport name (e.g., "Mumbai (BOM)")
                city: item.Subtitle   // Use Subtitle as the city (e.g., "India")
            }));
        }
        return []; // Return empty array if no data or unsuccessful
    } catch (error) {
        console.error('Failed to fetch autocomplete suggestions:', error);
        return []; // Return an empty array on error to avoid breaking the UI
    }
}

// Display suggestions
function displaySuggestions(suggestions, container, input) {
    container.innerHTML = '';
    if (suggestions.length === 0) {
        container.classList.add('hidden');
        return;
    }
    container.classList.remove('hidden');
    suggestions.forEach(suggestion => {
        const div = document.createElement('div');
        div.classList.add('suggestion-item');
        div.textContent = `${suggestion.name} - ${suggestion.city}`; // Display "Mumbai (BOM) - India"
        div.addEventListener('click', () => {
            input.value = suggestion.name; // Set input value to "Mumbai (BOM)"
            input.dataset.airportId = suggestion.id;
            input.dataset.airportEntityId = suggestion.entityId;
            container.classList.add('hidden');
        });
        container.appendChild(div);
    });
}

// Handle input for "From"
fromInput.addEventListener('input', async () => {
    const query = fromInput.value.trim();
    if (query.length < 2) {
        fromSuggestions.classList.add('hidden');
        return;
    }
    const suggestions = await fetchAutocompleteSuggestions(query);
    displaySuggestions(suggestions, fromSuggestions, fromInput);
});

// Handle input for "To"
toInput.addEventListener('input', async () => {
    const query = toInput.value.trim();
    if (query.length < 2) {
        toSuggestions.classList.add('hidden');
        return;
    }
    const suggestions = await fetchAutocompleteSuggestions(query);
    displaySuggestions(suggestions, toSuggestions, toInput);
});

// Hide suggestions when clicking outside
document.addEventListener('click', (e) => {
    if (!fromInput.contains(e.target) && !fromSuggestions.contains(e.target)) {
        fromSuggestions.classList.add('hidden');
    }
    if (!toInput.contains(e.target) && !toSuggestions.contains(e.target)) {
        toSuggestions.classList.add('hidden');
    }
    if (!document.getElementById('travelersInput').contains(e.target) && !document.getElementById('travelersDropdown').contains(e.target)) {
        document.getElementById('travelersDropdown').classList.add('hidden');
    }
});

// Travelers Dropdown Functionality
function toggleTravelerDropdown() {
    const dropdown = document.getElementById('travelersDropdown');
    dropdown.classList.toggle('hidden');
}

function updateTravelers(type, change) {
    const adultsInput = document.getElementById('adults');
    const childrenInput = document.getElementById('children');
    let adults = parseInt(adultsInput.value);
    let children = parseInt(childrenInput.value);

    if (type === 'adults') {
        adults = Math.max(1, adults + change); // Minimum 1 adult
        adultsInput.value = adults;
    } else if (type === 'children') {
        children = Math.max(0, children + change); // Minimum 0 children
        childrenInput.value = children;
    }

    updateTravelersDisplay();
}

function updateTravelersDisplay() {
    const adults = document.getElementById('adults').value;
    const children = document.getElementById('children').value;
    const total = parseInt(adults) + parseInt(children);
    document.getElementById('travelersInput').value = `${total} Traveler${total > 1 ? 's' : ''}`;
}

// Search Flights
async function searchFlights(event) {
    event.preventDefault(); // Prevent form submission

    const fromInput = document.getElementById('fromInput');
    const toInput = document.getElementById('toInput');
    const flightType = document.getElementById('flightType').value;
    const departureDate = document.getElementById('departureDate').value;
    const returnDate = document.getElementById('returnDate').value;
    const adults = document.getElementById('adults').value;
    const children = document.getElementById('children').value;
    const cabinClass = "Economy";

    // Validate required fields
    if (!fromInput.value || !toInput.value || !departureDate || (flightType === 'return' && !returnDate)) {
        alert('Please fill in all required fields.');
        return;
    }

    const requestData = {
        "originSkyId": fromInput.dataset.airportId || "",
        "destinationSkyId": toInput.dataset.airportId || "",
        "originEntityId": fromInput.dataset.airportEntityId || "",
        "destinationEntityId": toInput.dataset.airportEntityId || "",
        "date": departureDate,
        "returnDate": flightType === "return" ? returnDate : null,
        "cabinClass": "Economy",
        "adults": adults,
        "children": children,
        "infants": 0,
        "sortBy": "Best",
        "limit": 10,
        "carriersIds": []
    }


    try {
        const response = await fetch("/api/flight/search-flight", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(requestData)
        });
        const result = await response.json();
        console.log(result)
        displayFlights(result.Data);
    } catch (error) {
        console.error("Error fetching flights:", error);
    }


    console.log('Search Parameters:', {
        from: fromInput.value,
        fromId: fromInput.dataset.airportId || '',
        fromEntityId: fromInput.dataset.airportEntityId || '',
        to: toInput.value,
        toId: toInput.dataset.airportId || '',
        toEntityId: toInput.dataset.airportEntityId || '',
        flightType,
        departureDate,
        returnDate: flightType === 'return' ? returnDate : null,
        adults,
        children,
        cabinClass
    });

    alert('Searching for flights... (This is a placeholder for now)');
}

function displayFlights(data) {
    const flightResults = document.getElementById("flight-result");
    flightResults.innerHTML = "";

    if (data.length === 0) {
        flightResults.innerHTML = `
            <div class="no-flights">
                <i class="fas fa-plane-slash"></i>
                <h3>No flights found matching your criteria</h3>
                <p>Try adjusting your search filters</p>
            </div>
        `;
        return;
    }



    data.forEach((flight) => {
        const flightContainer = document.createElement("div");
        flightContainer.classList.add("flight-container");

        // Format price


        flightContainer.innerHTML = `
            <div class="flight-header">
                <div class="airline-info">
                    <img src="${flight.OutboundLeg.MarketingCarrier.CarrierLogo}" 
                         alt="${flight.OutboundLeg.MarketingCarrier.CarrierName}" class="airline-logo">
                    <div>
                        <h3>${flight.OutboundLeg.MarketingCarrier.CarrierName}</h3>
                        <p>Flight #${flight.OutboundLeg.FlightNumber}</p>
                    </div>
                </div>
                <div class="flight-price">
                    <span class="price-amount">${flight.Price}</span>
                    <span class="price-per-person">per person</span>
                </div>
            </div>
            
            <div class="flight-legs-container">
                <!-- Outbound Leg -->
                <div class="flight-leg outbound-leg">
                    <div class="leg-header">
                        <h4>Outbound Flight</h4>
                        <span class="flight-duration">${flight.OutboundLeg.Duration}</span>
                    </div>
                    
                    <div class="leg-details">
                        <div class="departure-info">
                            <div class="time">${formatTime(flight.OutboundLeg.Departure)}</div>
                            <div class="date">${formatDate(flight.OutboundLeg.Departure)}</div>
                            <div class="airport">${flight.OutboundLeg.Origin.CityName} (${flight.OutboundLeg.Origin.DisplayCode})</div>
                        </div>
                        
                        <div class="flight-stops">
                            <div class="stops-line">
                                <div class="stop-dot"></div>
                                <div class="stop-line"></div>
                                ${flight.OutboundLeg.StopCount > 0 ? '<div class="stop-dot intermediate"></div>' : ''}
                                <div class="stop-dot"></div>
                            </div>
                            <div class="stops-text">${flight.OutboundLeg.StopCount === 0 ? 'Non-stop' : `${flight.OutboundLeg.StopCount} stop(s)`}</div>
                        </div>
                        
                        <div class="arrival-info">
                            <div class="time">${formatTime(flight.OutboundLeg.Arrival)}</div>
                            <div class="date">${formatDate(flight.OutboundLeg.Arrival)}</div>
                            <div class="airport">${flight.OutboundLeg.Destination.CityName} (${flight.OutboundLeg.Destination.DisplayCode})</div>
                        </div>
                    </div>
                    
                    <div class="leg-footer">
  <button class="details-btn" onclick='showFlightDetails(${JSON.stringify(flight.SessionId)}, ${JSON.stringify(flight.OutboundLeg)})'>
    <i class="fas fa-info-circle"></i> Get Details
  </button>
</div>

                </div>
                
                <!-- Return Leg (if exists) -->
                ${flight.ReturnLeg ? `
                <div class="flight-leg return-leg">
                    <div class="leg-header">
                        <h4>Return Flight</h4>
                        <span class="flight-duration">${flight.ReturnLeg.Duration}</span>
                    </div>
                    
                    <div class="leg-details">
                        <div class="departure-info">
                            <div class="time">${formatTime(flight.ReturnLeg.Departure)}</div>
                            <div class="date">${formatDate(flight.ReturnLeg.Departure)}</div>
                            <div class="airport">${flight.ReturnLeg.Origin.CityName} (${flight.ReturnLeg.Origin.DisplayCode})</div>
                        </div>
                        
                        <div class="flight-stops">
                            <div class="stops-line">
                                <div class="stop-dot"></div>
                                <div class="stop-line"></div>
                                ${flight.ReturnLeg.StopCount > 0 ? '<div class="stop-dot intermediate"></div>' : ''}
                                <div class="stop-dot"></div>
                            </div>
                            <div class="stops-text">${flight.ReturnLeg.StopCount === 0 ? 'Non-stop' : `${flight.ReturnLeg.StopCount} stop(s)`}</div>
                        </div>
                        
                        <div class="arrival-info">
                            <div class="time">${formatTime(flight.ReturnLeg.Arrival)}</div>
                            <div class="date">${formatDate(flight.ReturnLeg.Arrival)}</div>
                            <div class="airport">${flight.ReturnLeg.Destination.CityName} (${flight.ReturnLeg.Destination.DisplayCode})</div>
                        </div>
                    </div>
                    
                    <div class="leg-footer">
  <button class="details-btn" onclick='showFlightDetails("${JSON.stringify(flight.SessionId)}", ${JSON.stringify(flight.ReturnLeg)})'>
    <i class="fas fa-info-circle"></i> Get Details
  </button>
</div>

                </div>
                ` : ''}
            </div>
            
          
        `;

        flightResults.appendChild(flightContainer);
    });
}

// Helper functions for formatting
function formatTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}


async function showFlightDetails(sessionId, leg) {
    
       const showFlightRequestedData = {
            "itineraryId" : leg.LegId ,
            "sessionId" : sessionId ,
            "Legs" : [{"destination": leg.Destination.DisplayCode,
                "origin": leg.Origin.DisplayCode ,
                "date" : leg.Departure.split('T')[0]}],
            "cabinClass": "Economy",
            "adults": document.getElementById('adults').value,
            "children": document.getElementById('children').value,
            "infants": 0
        }
    console.log(showFlightRequestedData)
    console.log(sessionId)
    try {
        const response = await fetch("/api/flight/get-flight-details", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(showFlightRequestedData)
        });
        
        const result = await response.json();
        
        // Check if the response contains the redirection URL
        if (result.redirectUrl) {
            // Redirect to the provided URL
            window.location.href = result.redirectUrl;
        } else {
            // Handle failure or error (you could show an error message, etc.)
            console.log("Redirect failed or an error occurred.");
        }
        
        
        console.log("done")

    } catch (error) {
        console.error("Error fetching flights:", error);
    }
}

const Data =[
    {
        "Id": "10957-2504281215--32672-0-13867-2504281510|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281215--32672-0-13867-2504281510",
            "FlightNumber": "2483",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/AI.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T12:15:00",
            "Arrival": "2025-04-28T15:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,113",
        "PricingOptionId": "i9YRNO19rIRl",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "second_cheapest"
        ],
        "Score": 0.999
    },
    {
        "Id": "10957-2504280930--32213-0-13867-2504281215|13867-2505051750--32213-0-10957-2505052045",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504280930--32213-0-13867-2504281215",
            "FlightNumber": "5044",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T09:30:00",
            "Arrival": "2025-04-28T12:15:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 45m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051750--32213-0-10957-2505052045",
            "FlightNumber": "698",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T17:50:00",
            "Arrival": "2025-05-05T20:45:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,375",
        "PricingOptionId": "YBK92WjWwAHo",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "third_cheapest",
            "shortest"
        ],
        "Score": 0.98081
    },
    {
        "Id": "10957-2504281340--32672-0-13867-2504281635|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281340--32672-0-13867-2504281635",
            "FlightNumber": "2839",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/AI.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T13:40:00",
            "Arrival": "2025-04-28T16:35:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,113",
        "PricingOptionId": "-Wxh5HRIlcC0",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "second_cheapest"
        ],
        "Score": 0.964287
    },
    {
        "Id": "10957-2504281215--32213-0-13867-2504281505|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281215--32213-0-13867-2504281505",
            "FlightNumber": "434",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T12:15:00",
            "Arrival": "2025-04-28T15:05:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 50m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,375",
        "PricingOptionId": "06SclQkCfrqU",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [],
        "Score": 0.943854
    },
    {
        "Id": "10957-2504280930--32213-0-13867-2504281215|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504280930--32213-0-13867-2504281215",
            "FlightNumber": "5044",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T09:30:00",
            "Arrival": "2025-04-28T12:15:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 45m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,375",
        "PricingOptionId": "PZ93gBGisxS2",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "third_cheapest",
            "shortest"
        ],
        "Score": 0.942971
    },
    {
        "Id": "10957-2504281215--32213-0-13867-2504281505|13867-2505051750--32213-0-10957-2505052045",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281215--32213-0-13867-2504281505",
            "FlightNumber": "434",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T12:15:00",
            "Arrival": "2025-04-28T15:05:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 50m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051750--32213-0-10957-2505052045",
            "FlightNumber": "698",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T17:50:00",
            "Arrival": "2025-05-05T20:45:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,375",
        "PricingOptionId": "J7Ty_XlLtAXY",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [],
        "Score": 0.942175
    },
    {
        "Id": "10957-2504281600--32213-0-13867-2504281850|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281600--32213-0-13867-2504281850",
            "FlightNumber": "6091",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T16:00:00",
            "Arrival": "2025-04-28T18:50:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 50m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,102",
        "PricingOptionId": "eyORCswoR_M0",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "cheapest",
            "third_shortest"
        ],
        "Score": 0.942113
    },
    {
        "Id": "10957-2504280930--32213-0-13867-2504281215|13867-2505051930--32213-0-10957-2505052225",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504280930--32213-0-13867-2504281215",
            "FlightNumber": "5044",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T09:30:00",
            "Arrival": "2025-04-28T12:15:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 45m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051930--32213-0-10957-2505052225",
            "FlightNumber": "613",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T19:30:00",
            "Arrival": "2025-05-05T22:25:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,375",
        "PricingOptionId": "yMwXduGZwnmq",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "third_cheapest",
            "shortest"
        ],
        "Score": 0.941467
    },
    {
        "Id": "10957-2504280930--32213-0-13867-2504281215|13867-2505051545--32213-0-10957-2505051840",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504280930--32213-0-13867-2504281215",
            "FlightNumber": "5044",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T09:30:00",
            "Arrival": "2025-04-28T12:15:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 45m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051545--32213-0-10957-2505051840",
            "FlightNumber": "5042",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T15:45:00",
            "Arrival": "2025-05-05T18:40:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,631",
        "PricingOptionId": "qv_TFa9Jsiki",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "second_shortest"
        ],
        "Score": 0.939268
    },
    {
        "Id": "10957-2504281615--32672-0-13867-2504281910|13867-2505051315--32213-0-10957-2505051610",
        "SessionId": "KLUv_SCNzQMA0kgdHbDrIGmTVB2mqTgpaaNw_J-KFDUnK73HXIMmOtgQDxQgy88YZVOGxt434S_78zZT-k5udtDfVFWWUgAG3C4NywhG0vZ37mmdW-vs9Ul5Gfe4y9eXkzk5AsJdddQ9uzfc2FC_XCUvQ2Uvx9fwPUwGZ4gKQJCGAA==",
        "OutboundLeg": {
            "LegId": "10957-2504281615--32672-0-13867-2504281910",
            "FlightNumber": "537",
            "Origin": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "Destination": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "MarketingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/AI.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32672",
                "AlternatedId": "AI",
                "CarrierName": "Air India",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-04-28T16:15:00",
            "Arrival": "2025-04-28T19:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "ReturnLeg": {
            "LegId": "13867-2505051315--32213-0-10957-2505051610",
            "FlightNumber": "2387",
            "Origin": {
                "AirportEntityId": "MAA",
                "AirportName": "Chennai",
                "DisplayCode": "MAA",
                "CityName": "Chennai"
            },
            "Destination": {
                "AirportEntityId": "DEL",
                "AirportName": "Indira Gandhi International ",
                "DisplayCode": "DEL",
                "CityName": "New Delhi"
            },
            "MarketingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": "https://logos.skyscnr.com/images/airlines/favicon/49.png",
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "OperatingCarrier": {
                "CarrierId": "-32213",
                "AlternatedId": "49",
                "CarrierName": "IndiGo",
                "CarrierLogo": null,
                "DisplayCode": null,
                "DisplayCodeType": null,
                "BrandColour": null
            },
            "Departure": "2025-05-05T13:15:00",
            "Arrival": "2025-05-05T16:10:00",
            "StopCount": 0,
            "StopDescription": "Non Stop",
            "Duration": "2h 55m",
            "DayChange": 0
        },
        "Price": "₹12,113",
        "PricingOptionId": "4M4WHJlVTQEC",
        "IsFreeCancellation": false,
        "IsChangeAllowed": false,
        "IsSelfTransferAllowed": false,
        "IsPartiallyRefundable": false,
        "Tags": [
            "second_cheapest"
        ],
        "Score": 0.935288
    }
]

const getFlightsDetailesData = 
{
    "ItineraryId": "10957-2504281615--32672-0-13867-2504281910",
    "BookingSessionId": "6b845220-9637-47a6-a694-7c993869f27e",
    "Origin": {
      "AirportEntityId": "10957",
      "AirportName": "Indira Gandhi International",
      "CityName": "New Delhi",
      "DisplayCode": "DEL"
    },
    "Destination": {
      "AirportEntityId": "13867",
      "AirportName": "Chennai",
      "CityName": "Chennai",
      "DisplayCode": "MAA"
    },
    "DestinationImage": "https://content.skyscnr.com/6444159c8f0ec14c96012de8a2502245/eyeem-26693940-86115263.jpg",
    "legs": [
      {
        "LegId": "10957-2504281615--32672-0-13867-2504281910",
        "FlightNumber": "AI537",
        "Origin": {
          "AirportEntityId": "10957",
          "AirportName": "Indira Gandhi International",
          "CityName": "New Delhi",
          "DisplayCode": "DEL"
        },
        "Destination": {
          "AirportEntityId": "13867",
          "AirportName": "Chennai",
          "CityName": "Chennai",
          "DisplayCode": "MAA"
        },
        "MarketingCarrier": {
          "CarrierId": "-32672",
          "AlternatedId": null,
          "CarrierName": "Air India",
          "CarrierLogo": null,
          "DisplayCode": "AI",
          "DisplayCodeType": null,
          "BrandColour": null
        },
        "OperatingCarrier": {
          "CarrierId": "-32672",
          "AlternatedId": null,
          "CarrierName": "Air India",
          "CarrierLogo": null,
          "DisplayCode": "AI",
          "DisplayCodeType": null,
          "BrandColour": null
        },
        "Departure": "2025-04-28T16:15:00",
        "Arrival": "2025-04-28T19:10:00",
        "StopCount": 0,
        "StopDescription": "Non Stop",
        "Duration": "2h 55m",
        "DayChange": 0
      }
    ],
    "bookingAgents": [
      {
        "Id": "jGga-V5QKnmc",
        "AgentId": "stbf",
        "Name": "Sky-tours",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/stbf/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "72.00",
        "RatingValue": "2.2",
        "RatingCount": "1108"
      },
      {
        "Id": "fFaMp_8-Quch",
        "AgentId": "cust",
        "Name": "Trip.com",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/cust/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "74.40",
        "RatingValue": "4.5",
        "RatingCount": "7781"
      },
      {
        "Id": "ymJZcTuV8GrV",
        "AgentId": "oojo",
        "Name": "Oojo",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/oojo/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "74.90",
        "RatingValue": "4.5",
        "RatingCount": "3827"
      },
      {
        "Id": "obtrYVnAA1c9",
        "AgentId": "inda",
        "Name": "Air India",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/inda/1/10957.13867.2025-04-28/air/airli/flights?...",
        "Price": "79.38",
        "RatingValue": "3.9",
        "RatingCount": "227"
      },
      {
        "Id": "0XpJS7i_Lybw",
        "AgentId": "xpus",
        "Name": "Expedia",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/xpus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "80.24",
        "RatingValue": "4.6",
        "RatingCount": "6223"
      },
      {
        "Id": "U2QGzVIEtbRY",
        "AgentId": "skyp",
        "Name": "Kiwi.com",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/skyp/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "81.00",
        "RatingValue": "3.1",
        "RatingCount": "3207"
      },
      {
        "Id": "YzeJTO13cWLd",
        "AgentId": "suus",
        "Name": "StudentUniverse",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/suus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "82.49",
        "RatingValue": "2.1",
        "RatingCount": "447"
      },
      {
        "Id": "qPzm6n4V0Yv7",
        "AgentId": "pcln",
        "Name": "Priceline",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/pcln/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "83.58",
        "RatingValue": "3.8",
        "RatingCount": "1147"
      },
      {
        "Id": "TeXlEcEgEJFO",
        "AgentId": "edus",
        "Name": "eDreams",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/edus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "83.62",
        "RatingValue": "2.8",
        "RatingCount": "3466"
      },
      {
        "Id": "Ck0U3YWRC7Ly",
        "AgentId": "bcom",
        "Name": "Booking.com",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/bcom/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "83.99",
        "RatingValue": "4.8",
        "RatingCount": "9757"
      },
      {
        "Id": "82nCCKOB2sWj",
        "AgentId": "arus",
        "Name": "Mytrip",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/arus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "83.99",
        "RatingValue": "3.5",
        "RatingCount": "4146"
      },
      {
        "Id": "AVIQME66Ronv",
        "AgentId": "gtus",
        "Name": "Gotogate",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/gtus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "83.99",
        "RatingValue": "2.5",
        "RatingCount": "831"
      },
      {
        "Id": "nwrVLg0zwQLa",
        "AgentId": "chpo",
        "Name": "CheapOair",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/chpo/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "85.58",
        "RatingValue": "2.4",
        "RatingCount": "1272"
      },
      {
        "Id": "TyaC9JJkY9pE",
        "AgentId": "orbz",
        "Name": "Orbitz",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/orbz/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "85.71",
        "RatingValue": "1.6",
        "RatingCount": "434"
      },
      {
        "Id": "9OqIL84yHcAc",
        "AgentId": "fnus",
        "Name": "Flightnetwork",
        "BookingProposition": "PBOOK",
        "BookingUrl": "https://www.skyscanner.net/transport_deeplink/4.0/US/en-GB/USD/fnus/1/10957.13867.2025-04-28/air/trava/flights?...",
        "Price": "88.99",
        "RatingValue": "3.0",
        "RatingCount": "561"
      }
    ]
  }

displayFlights(Data)



