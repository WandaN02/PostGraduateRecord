document.addEventListener("DOMContentLoaded", function () {

    var passwordInput =
        document.getElementById("password");

    var passwordToggle =
        document.getElementById("passwordToggle");

    var eyeIcon =
        document.getElementById("passwordEye");


    if (!passwordInput ||
        !passwordToggle ||
        !eyeIcon) {
        return;
    }


    passwordToggle.addEventListener("click", function () {

        if (passwordInput.type === "password") {

            passwordInput.type = "text";

            eyeIcon.classList.remove("fa-eye");
            eyeIcon.classList.add("fa-eye-slash");

        }
        else {

            passwordInput.type = "password";

            eyeIcon.classList.remove("fa-eye-slash");
            eyeIcon.classList.add("fa-eye");

        }

    });

});