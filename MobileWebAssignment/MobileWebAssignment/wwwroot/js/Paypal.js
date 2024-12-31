console.log("JavaScript file is successfully connected!");
function initPayPalButton() {
    paypal
        .Buttons({
            style: {
                shape: "rect",
                color: "gold",
                layout: "vertical",
                label: "paypal",
            },

          
            createOrder: function (data, actions) {
                const userInput = document.getElementById("totalcal").value.replace(/[^\d.-]/g, ""); // Removes RM and other non-numeric characters
                const paypalAmount = parseFloat(userInput).toFixed(2);
                return actions.order.create({
                    purchase_units: [
                        { amount: { currency_code: "USD", value: paypalAmount } },
                    ],
                });
            },

            onApprove: function (data, actions) {
                return actions.order.capture().then(function (orderData) {
                    // Full available details
                    console.log("Capture result", orderData, JSON.stringify(orderData, null, 2));

                    // Extract payment details
                    const transaction = orderData.purchase_units[0].payments.captures[0];
                    
                    // Send data to the server
                    fetch('/Client/PurchasePaypal', { // Adjust the endpoint to match your server
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            orderId: orderData.id,
                            transactionId: transaction.id,
                            amount: transaction.amount.value,
                            currency: transaction.amount.currency_code,
                            paymentStatus: transaction.status,
                            purchaseDetails: "Include any additional purchase details here",
                        })
                    })
                        .then(response => response.json())
                        .then(result => {
                            console.log("Server response:", result);
                            // Show a success message within this page
                            if (result.success) { 
                                window.location.href = '/Client/ClientPaymentHIS?Unpaid=unpaid&message=Payment Unsuccessful!';
                            } 
                        })
                        .catch(error => {
                            console.error("Error recording the purchase:", error);
                        });
                });
            },

            onError: function (err) {
                console.log(err);
            },
        })
        .render("#paypal-button-container");
}
initPayPalButton();