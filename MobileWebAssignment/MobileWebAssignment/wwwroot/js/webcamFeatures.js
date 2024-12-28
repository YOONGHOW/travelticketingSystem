// Webcam function
$(function () {
    let webcamOpen = false;

    // Initialize webcam properties
    Webcam.set({
        width: 360,
        height: 260,
        image_format: 'jpeg',
        jpeg_quality: 90
    });

    // Toggle webcam
    $('#openwebcam').click(function () {
        if (!webcamOpen) {
            Webcam.attach('#idwebcam');
            $(this).val('Close Webcam');
            $('#idwebcam').show(); // Show the webcam container
            webcamOpen = true;
        } else {
            Webcam.reset();
            $(this).val('Open Webcam');
            $('#idwebcam').hide(); // Hide the webcam container
            webcamOpen = false;
        }
    });

    // Capture image
    $('#btncapture').click(function () {
        Webcam.snap(function (data_uri) {
            $("#drag")[0].src = data_uri;
            $("#PhotoBase64").val(data_uri);
        });
    });

    // Preview uploaded file
    $('input[asp-for="Photo"]').change(function (e) {
        const reader = new FileReader();
        reader.onload = function (event) {
            $("#drag").attr('src', event.target.result);
        };
        reader.readAsDataURL(e.target.files[0]);
    });
});