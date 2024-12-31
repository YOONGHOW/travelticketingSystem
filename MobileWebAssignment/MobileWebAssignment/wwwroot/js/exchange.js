document.getElementById('language-selector').addEventListener('change', function () {
    const selectedCurrency = this.value;

    // Fetch exchange rates from the server
            document.cookie = `selectedCurrency=${selectedCurrency}; path=/; max-age=31536000`;

            fetch(`/api/exchange-rate?currency=${selectedCurrency}`)
                .then(response => response.json())
                .then(data => {
                    const rate = data.rate; 

            // Update the price for all tickets
            document.querySelectorAll('.ticket-price').forEach(priceElement => {
                const basePrice = parseFloat(priceElement.dataset.basePrice); // Get base price from data attribute
                const convertedPrice = (basePrice * rate).toFixed(2);
                priceElement.textContent = `${convertedPrice} ${selectedCurrency}`;
            });
        })
        .catch(error => console.error('Error fetching exchange rate:', error));
        });

    window.addEventListener('DOMContentLoaded', function () {
        const cookies = document.cookie.split('; ').reduce((acc, curr) => {
            const [key, value] = curr.split('=');
            acc[key] = value;
            return acc;
        }, {});

        if (cookies.selectedCurrency) {
            document.getElementById('language-selector').value = cookies.selectedCurrency;

            // Trigger a change event to load prices in the saved currency
            document.getElementById('language-selector').dispatchEvent(new Event('change'));
        }
    });

