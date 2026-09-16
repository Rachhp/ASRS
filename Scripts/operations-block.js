let faultActive = false;

function showFaultModal(message) {
    document.getElementById("faultMessage").innerText = message;
    document.getElementById("faultModal").style.display = "block";
}

function hideFaultModal() {
    document.getElementById("faultModal").style.display = "none";
}

function checkOperations() {
    $.ajax({
        url: '/Home/CheckOperationsStatus',
        type: 'GET',
        success: function (faultStatus) {
            if (faultStatus !== "NoFault") {
                // Block operations units
                for (let i = 1; i <= 4; i++) {
                    let unit = $("#operationUnit" + i);
                    unit.prop('disabled', true);
                    unit.addClass('blocked');
                }

                if (!faultActive) {
                    faultActive = true;
                    showFaultModal("Operations blocked due to fault: " + faultStatus);
                }

            } else {
                // Fault resolved
                for (let i = 1; i <= 4; i++) {
                    let unit = $("#operationUnit" + i);
                    unit.prop('disabled', false);
                    unit.removeClass('blocked');
                }
                hideFaultModal();
                faultActive = false;
            }
        },
        error: function (xhr, status, error) {
            console.error("AJAX Error:", status, error);
        },
        complete: function () {
            setTimeout(checkOperations, 2000);
        }
    });
}

$(document).ready(function () {
    checkOperations();

    // OK button click -> redirect
    $("#faultOkBtn").click(function () {
        location.href = '/Home/Dashboard';
    });
});
