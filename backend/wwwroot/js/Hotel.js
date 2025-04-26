document.addEventListener('DOMContentLoaded', () => {
    const searchBox = document.getElementById('searchBox');
    const suggestionsBox = document.getElementById('suggestions');
    const popularList = document.getElementById('popular-list');
    const searchQuery = document.getElementById('searchQuery');

    let debounceTimer;
    let controller;
    
    searchBox.addEventListener('input', function () {
        const query = this.value.trim();
        searchQuery.textContent = query;
    
        clearTimeout(debounceTimer);
    
        if (query.length < 2) {
            suggestionsBox.style.display = 'none';
            popularList.innerHTML = '';
            return;
        }
    
        debounceTimer = setTimeout(async () => {
            if (controller) controller.abort(); // Cancel previous request
            controller = new AbortController();
    
            try {
                const response = await fetch(`/api/city/search?query=${encodeURIComponent(query)}`, {
                    signal: controller.signal
                });
    
                if (!response.ok) {
                    throw new Error(`Server responded with ${response.status}`);
                }
    
                const data = await response.json();
                popularList.innerHTML = Array.isArray(data) && data.length > 0 
                    ? data.map(city => `<div class="suggestion-item" data-city="${city.name}">${city.name}</div>`).join('') 
                    : '<div class="suggestion-item">No results found</div>';
    
                document.querySelectorAll('.suggestion-item').forEach(item => {
                    item.addEventListener('click', () => {
                        searchBox.value = item.getAttribute('data-city');
                        suggestionsBox.style.display = 'none';
                    });
                });
    
                suggestionsBox.style.display = 'block';
            } catch (error) {
                if (error.name !== 'AbortError') {
                    console.error('Error fetching cities:', error);
                }
            }
        }, 600);
    });
    

    // Hide suggestion box when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.input-group')) {
            suggestionsBox.style.display = 'none';
        }
    });
});
