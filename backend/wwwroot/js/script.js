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
                entityId : item.Value.EntityId ,
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
    const cabinClass = document.getElementById('cabinClass').value;

    // Validate required fields
    if (!fromInput.value || !toInput.value || !departureDate || (flightType === 'return' && !returnDate)) {
        alert('Please fill in all required fields.');
        return;
    }

    const requestData  = {
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
        fromEntityId : fromInput.dataset.airportEntityId || '',
        to: toInput.value,
        toId: toInput.dataset.airportId || '',
        toEntityId : toInput.dataset.airportEntityId || '',
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
        const price = parseFloat(flight.Price);
        const formattedPrice = new Intl.NumberFormat('en-IN', {
            style: 'currency',
            currency: 'INR',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(price);

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
                        <button class="details-btn" onclick="showFlightDetails('${flight.OutboundLeg.LegId}')">
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
                        <button class="details-btn" onclick="showFlightDetails('${flight.ReturnLeg.LegId}')">
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



// Placeholder function for showing flight details
function showFlightDetails(legId) {
    console.log("Showing details for leg:", legId);
    // Implement your flight details display logic here
    // This could open a modal or expand a section with more details
}

const Data =  [
        {
            "Id": "10957-2503301215--32672-0-13867-2503301510|13867-2504051600--32672-0-10957-2504051850",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503301215--32672-0-13867-2503301510",
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
                "Departure": "2025-03-30T12:15:00",
                "Arrival": "2025-03-30T15:10:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504051600--32672-0-10957-2504051850",
                "FlightNumber": "2484",
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
                "Departure": "2025-04-05T16:00:00",
                "Arrival": "2025-04-05T18:50:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "Price": "₹12,257",
            "PricingOptionId": "2AKd5WzSo4eA",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [],
            "Score": 0.999
        },
        {
            "Id": "10957-2503300955--32672-0-13867-2503301245|13867-2504051600--32672-0-10957-2504051850",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503300955--32672-0-13867-2503301245",
                "FlightNumber": "2467",
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
                "Departure": "2025-03-30T09:55:00",
                "Arrival": "2025-03-30T12:45:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504051600--32672-0-10957-2504051850",
                "FlightNumber": "2484",
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
                "Departure": "2025-04-05T16:00:00",
                "Arrival": "2025-04-05T18:50:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "Price": "₹12,514",
            "PricingOptionId": "3WueVB5Vb5RB",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "third_shortest"
            ],
            "Score": 0.974746
        },
        {
            "Id": "10957-2503300745--32672-0-13867-2503301035|13867-2504052105--32213-0-10957-2504060000",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503300745--32672-0-13867-2503301035",
                "FlightNumber": "2833",
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
                "Departure": "2025-03-30T07:45:00",
                "Arrival": "2025-03-30T10:35:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504052105--32213-0-10957-2504060000",
                "FlightNumber": "2166",
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
                "Departure": "2025-04-05T21:05:00",
                "Arrival": "2025-04-06T00:00:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 1
            },
            "Price": "₹11,417",
            "PricingOptionId": "nFbgWw91yewi",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "cheapest"
            ],
            "Score": 0.947697
        },
        {
            "Id": "10957-2503301215--32672-0-13867-2503301510|13867-2504052105--32213-0-10957-2504060000",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503301215--32672-0-13867-2503301510",
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
                "Departure": "2025-03-30T12:15:00",
                "Arrival": "2025-03-30T15:10:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504052105--32213-0-10957-2504060000",
                "FlightNumber": "2166",
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
                "Departure": "2025-04-05T21:05:00",
                "Arrival": "2025-04-06T00:00:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 1
            },
            "Price": "₹11,417",
            "PricingOptionId": "St6CMa1A8-gB",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "second_cheapest"
            ],
            "Score": 0.925476
        },
        {
            "Id": "10957-2503301340--32672-0-13867-2503301635|13867-2504051600--32672-0-10957-2504051850",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503301340--32672-0-13867-2503301635",
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
                "Departure": "2025-03-30T13:40:00",
                "Arrival": "2025-03-30T16:35:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504051600--32672-0-10957-2504051850",
                "FlightNumber": "2484",
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
                "Departure": "2025-04-05T16:00:00",
                "Arrival": "2025-04-05T18:50:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "Price": "₹12,514",
            "PricingOptionId": "K58om50NpZS4",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [],
            "Score": 0.925028
        },
        {
            "Id": "10957-2503300745--32672-0-13867-2503301035|13867-2504051600--32672-0-10957-2504051850",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503300745--32672-0-13867-2503301035",
                "FlightNumber": "2833",
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
                "Departure": "2025-03-30T07:45:00",
                "Arrival": "2025-03-30T10:35:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504051600--32672-0-10957-2504051850",
                "FlightNumber": "2484",
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
                "Departure": "2025-04-05T16:00:00",
                "Arrival": "2025-04-05T18:50:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "Price": "₹12,257",
            "PricingOptionId": "RKk3lOLSV-vS",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "second_shortest"
            ],
            "Score": 0.92471
        },
        {
            "Id": "10957-2503300955--32672-0-13867-2503301245|13867-2504052105--32213-0-10957-2504060000",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503300955--32672-0-13867-2503301245",
                "FlightNumber": "2467",
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
                "Departure": "2025-03-30T09:55:00",
                "Arrival": "2025-03-30T12:45:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504052105--32213-0-10957-2504060000",
                "FlightNumber": "2166",
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
                "Departure": "2025-04-05T21:05:00",
                "Arrival": "2025-04-06T00:00:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 1
            },
            "Price": "₹11,605",
            "PricingOptionId": "DQVGW3axv8H-",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "third_cheapest"
            ],
            "Score": 0.922736
        },
        {
            "Id": "10957-2503301215--32672-0-13867-2503301510|13867-2504050805--32672-0-10957-2504051050",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503301215--32672-0-13867-2503301510",
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
                "Departure": "2025-03-30T12:15:00",
                "Arrival": "2025-03-30T15:10:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504050805--32672-0-10957-2504051050",
                "FlightNumber": "892",
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
                "Departure": "2025-04-05T08:05:00",
                "Arrival": "2025-04-05T10:50:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 45m",
                "DayChange": 0
            },
            "Price": "₹12,257",
            "PricingOptionId": "yf0reRaEOGXt",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "second_shortest"
            ],
            "Score": 0.921045
        },
        {
            "Id": "10957-2503301215--32672-0-13867-2503301510|13867-2504052040--32672-0-10957-2504052325",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503301215--32672-0-13867-2503301510",
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
                "Departure": "2025-03-30T12:15:00",
                "Arrival": "2025-03-30T15:10:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 55m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504052040--32672-0-10957-2504052325",
                "FlightNumber": "538",
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
                "Departure": "2025-04-05T20:40:00",
                "Arrival": "2025-04-05T23:25:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 45m",
                "DayChange": 0
            },
            "Price": "₹12,257",
            "PricingOptionId": "gLrnxrVFikPz",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "second_shortest"
            ],
            "Score": 0.918359
        },
        {
            "Id": "10957-2503300955--32672-0-13867-2503301245|13867-2504052040--32672-0-10957-2504052325",
            "SessionId": "Cl0IARJZCk4KJDM5MGNmY2M3LTg0MWYtNGMyNi1iZDYyLTQzMDgxY2E5ODllOBACGiQ3ZDc3ZGI0Yy1jZGJmLTQxN2QtOThjOC1jNGNiM2UzOTcyMzQQ-77Emt0yGAESKHVzc19mYTBiYTMzYy02YWVhLTRhNzAtOTk3OS1iMjExM2Q1MTk4NGMiAlVT",
            "OutboundLeg": {
                "LegId": "10957-2503300955--32672-0-13867-2503301245",
                "FlightNumber": "2467",
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
                "Departure": "2025-03-30T09:55:00",
                "Arrival": "2025-03-30T12:45:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 50m",
                "DayChange": 0
            },
            "ReturnLeg": {
                "LegId": "13867-2504052040--32672-0-10957-2504052325",
                "FlightNumber": "538",
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
                "Departure": "2025-04-05T20:40:00",
                "Arrival": "2025-04-05T23:25:00",
                "StopCount": 0,
                "StopDescription": "Non Stop",
                "Duration": "2h 45m",
                "DayChange": 0
            },
            "Price": "₹12,514",
            "PricingOptionId": "Tv3oZKq4rrdj",
            "IsFreeCancellation": false,
            "IsChangeAllowed": false,
            "IsSelfTransferAllowed": false,
            "IsPartiallyRefundable": false,
            "Tags": [
                "shortest"
            ],
            "Score": 0.91724
        }
    ]

displayFlights(Data)