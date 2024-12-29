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
                    console.log(
                        "Capture result",
                        orderData,
                        JSON.stringify(orderData, null, 2)
                    );

                    // Show a success message within this page, for example:
                    const element = document.getElementById("paypal-button-container");
                    element.innerHTML = "";
                    element.innerHTML = "<h3>Thank you for your payment!</h3>";
                    console.log("JavaScript file is successfully payment!");
                    // Or go to another URL:  actions.redirect('thank_you.html');
                });
            },

            onError: function (err) {
                console.log(err);
            },
        })
        .render("#paypal-button-container");
}
initPayPalButton();