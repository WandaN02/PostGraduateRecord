document.addEventListener("DOMContentLoaded", function () {

    var documentInput =
        document.getElementById("SupportingDocument");

    var fileError =
        document.getElementById("applicationFileError");

    var submitButton =
        document.getElementById("applicationSubmitButton");


    var allowedExtensions = [
        ".pdf",
        ".doc",
        ".docx"
    ];


    documentInput.addEventListener("change", function () {

        // Reset validation
        fileError.style.display = "none";

        submitButton.disabled = false;


        // No file selected
        if (!documentInput.files ||
            documentInput.files.length === 0) {

            return;
        }


        // Get selected file name
        var fileName =
            documentInput.files[0].name.toLowerCase();


        // Get extension
        var extension =
            fileName.substring(
                fileName.lastIndexOf(".")
            );


        // Check extension
        if (!allowedExtensions.includes(extension)) {

            // Show error
            fileError.style.display = "block";


            // Disable submit
            submitButton.disabled = true;


            // Remove invalid file
            documentInput.value = "";


            return;
        }

    });

});