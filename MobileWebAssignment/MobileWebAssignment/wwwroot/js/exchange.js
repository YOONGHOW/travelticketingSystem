document.getElementById('language-selector').addEventListener('change', function () {
    const selectedCurrency = this.value;

    // Fetch exchange rates from the server
    fetch(`/api/exchange-rate?currency=${selectedCurrency}`)
        .then(response => response.json())
        .then(data => {
            const rate = data.rate; // Exchange rate from API

            // Update the price for all tickets
            document.querySelectorAll('.ticket-price').forEach(priceElement => {
                const basePrice = parseFloat(priceElement.dataset.basePrice); // Get base price from data attribute
                const convertedPrice = (basePrice * rate).toFixed(2);
                priceElement.textContent = `${convertedPrice} ${selectedCurrency}`;
            });
        })
        .catch(error => console.error('Error fetching exchange rate:', error));
});

