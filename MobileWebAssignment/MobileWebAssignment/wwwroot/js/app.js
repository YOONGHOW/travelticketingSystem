// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.target.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});

// Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$('[type=reset]').on('click', e => {
    e.preventDefault();
    location = location;
});

// Check all checkboxes
$('[data-check]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a')) return;

    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview
$('.upload input').on('change', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;

    if (f && f.type.startsWith('image/')) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    // Trigger input validation
    $(e.target).valid();
});

// Drag and drop functionality
$('label.upload').on('dragenter dragover', e => {
    e.preventDefault();  // Prevent default behavior, which is open the file in browser
    e.stopPropagation();
    $('#drag').css('border', '5px dotted #9b59b6');
});

$('label.upload').on('dragleave', e => {
    e.preventDefault();
    e.stopPropagation();
    $('#drag').css('border', '2px solid black');
});

$('label.upload').on('drop', e => {
    e.preventDefault();
    e.stopPropagation();

    const dt = e.originalEvent.dataTransfer;
    const f = dt.files[0];
    const img = $(e.currentTarget).find('img')[0]; // Use currentTarget to ensure we reference the label element
    const input = $(e.currentTarget).find('input[type=file]')[0];

    if (!img) return;

    img.dataset.src ??= img.src;

    if (f?.type.startsWith('image/')) {
        img.src = URL.createObjectURL(f);
        input.files = dt.files; //set the file to input[type=file]
    } else {
        img.src = img.dataset.src;
    }
});

// Sync start time when button is clicked
$("#syncStartTimeButton").on("click", function () {
    const firstStartTime = $(".start-time").first().val();

    if (firstStartTime) {
        $(".start-time").each(function () {
            $(this).val(firstStartTime);
        });
    } else {
        alert("Please select a start time first.");
    }
});

// Sync end time when button is clicked
$("#syncEndTimeButton").on("click", function () {
    const firstEndTime = $(".end-time").first().val();

    if (firstEndTime) {
        $(".end-time").each(function () {
            $(this).val(firstEndTime);
        });
    } else {
        alert("Please select an end time first.");
    }
});

$(".day-status").on("change", function () {
    const isClosed = $(this).val() === "closed";
    const row = $(this).closest(".day-row");
    row.find(".start-time, .end-time").prop("disabled", isClosed);
});

$(".day-status").each(function () {
    const isClosed = $(this).val() === "closed";
    const row = $(this).closest(".day-row");
    row.find(".start-time, .end-time").prop("disabled", isClosed);
});



