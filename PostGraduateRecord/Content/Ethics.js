document.addEventListener("DOMContentLoaded", function () {

    var yesButton = document.getElementById("ethicsYes");
    var noButton = document.getElementById("ethicsNo");
    var numberSection = document.getElementById("ethicsNumberSection");

    function updateEthicsNumber() {

        if (yesButton.checked) {

            numberSection.style.display = "block";

        } else {

            numberSection.style.display = "none";
        }
    }

    if (yesButton && noButton && numberSection) {

        yesButton.addEventListener("change", updateEthicsNumber);

        noButton.addEventListener("change", updateEthicsNumber);

        updateEthicsNumber();
    }

});