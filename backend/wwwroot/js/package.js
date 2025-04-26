document.querySelector('.search-btn').addEventListener('click', function() {
    alert('Search functionality will be implemented soon!');
});

console.log("hii package")
const destinations = [
    { img: "~/imagesP/kerala.jpg", name: "Kerala", price: "₹20,000", rating: "5.0" },
    { img: "~/imagesP/shimla.jpg", name: "Shimla", price: "₹15,000", rating: "4.9" },
    { img: "~/imagesP/goa.jpg", name: "Goa", price: "₹30,000", rating: "4.9" },
    { img: "~/imagesP/manali.jpg", name: "Manali", price: "₹11,000", rating: "4.2" },
    { img: "~/imagesP/himachal.jpg", name: "Himachal", price: "₹17,000", rating: "4.9" },
    { img: "~/imagesP/kashmir.jpg", name: "Kashmir", price: "₹35,000", rating: "4.5" },
    { img: "~/imagesP/rajasthan.jpg", name: "Rajasthan", price: "₹16,000", rating: "4.9" },
    { img: "~/imagesP/udaipur.jpg", name: "Udaipur", price: "₹25,000", rating: "4.6" },
    { img: "~/imagesP/ladakh.jpg", name: "Ladakh", price: "₹15,000", rating: "4.7" },
    { img: "~/imagesP/mysore.jpg", name: "Mysore", price: "₹18,000", rating: "4.8" },
    { img: "~/imagesP/ooty.jpg", name: "Ooty", price: "₹22,000", rating: "4.7" },
    { img: "~/imagesP/darjeeling.jpg", name: "Darjeeling", price: "₹19,000", rating: "4.6" }
];

const itemsPerPage = 6;
let currentPage = 1;

function renderDestinations() {
    const grid = document.getElementById("destinationGrid");
    grid.innerHTML = "";
    
    let start = (currentPage - 1) * itemsPerPage;
    let end = start + itemsPerPage;
    
    let paginatedItems = destinations.slice(start, end);

    paginatedItems.forEach(destination => {
        const card = document.createElement("div");
        card.classList.add("card");

        card.innerHTML = `
            <img src="${destination.img}" alt="${destination.name}">
            <div class="card-content">
                <h3>${destination.name}</h3>
                <p>Starting At ${destination.price} Per Person</p>
                <div class="rating">
                    <span>⭐ ${destination.rating}</span>
                </div>
            </div>
        `;

        grid.appendChild(card);
    });

    updatePagination();
}

function updatePagination() {
    const pageNumbers = document.getElementById("pageNumbers");
    pageNumbers.innerHTML = "";
    
    const totalPages = Math.ceil(destinations.length / itemsPerPage);
    
    for (let i = 1; i <= totalPages; i++) {
        let pageBtn = document.createElement("button");
        pageBtn.textContent = i;
        pageBtn.classList.add("page-btn");
        
        if (i === currentPage) {
            pageBtn.style.fontWeight = "bold";
            pageBtn.style.color = "red";
        }
        
        pageBtn.onclick = () => {
            currentPage = i;
            renderDestinations();
        };

        pageNumbers.appendChild(pageBtn);
    }
}

function changePage(step) {
    const totalPages = Math.ceil(destinations.length / itemsPerPage);
    
    if (currentPage + step > 0 && currentPage + step <= totalPages) {
        currentPage += step;
        renderDestinations();
    }
}

document.addEventListener("DOMContentLoaded", () => {
    renderDestinations();
});
