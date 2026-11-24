document.addEventListener("DOMContentLoaded", function () {

  var documentInput = document.getElementById("document");
var fileError = document.getElementById("fileError");
var submitButton = document.getElementById("submitButton");

var allowedExtensions = [
    ".pdf",
    ".doc",
    ".docx"
];


documentInput.addEventListener("change", function () {

    fileError.style.display = "none";
    submitButton.disabled = false;


    if (!documentInput.files ||
        documentInput.files.length === 0) {

        return;
    }


    var fileName =
        documentInput.files[0].name.toLowerCase();


    var extension =
        fileName.substring(
            fileName.lastIndexOf(".")
        );


    if (!allowedExtensions.includes(extension)) {

        fileError.style.display = "block";

        submitButton.disabled = true;

        documentInput.value = "";

        return;
    }

});


});
