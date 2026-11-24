document.addEventListener("DOMContentLoaded", function () {

    var popup =
        document.getElementById("studentPopup");


    if (popup) {

        setTimeout(function () {

            popup.classList.add("hide");

        }, 3000);


        setTimeout(function () {

            popup.remove();

        }, 3400);

    }

});