document.addEventListener("DOMContentLoaded", function () {

    var popup = document.getElementById("validationPopup");
    var message = document.getElementById("validationMessage");
    var okButton = document.getElementById("validationOkButton");

    var currentField = null;

    function showPopup(text, field) {

        if (!popup || !message) {
            return;
        }

        message.textContent = text;
        currentField = field;

        popup.classList.add("show");
    }

    if (okButton) {

        okButton.addEventListener("click", function () {

            popup.classList.remove("show");

            if (currentField) {
                currentField.focus();
            }
        });
    }


    /* =========================
       STUDENT NUMBER
       NUMBERS ONLY
       ========================= */

    var studentNumber =
        document.getElementById("StudentNumber");

    if (studentNumber) {

        studentNumber.addEventListener("input", function () {

            var value = this.value;

            if (/[^0-9]/.test(value)) {

                var invalidCharacters =
                    value.match(/[^0-9]/g);

                showPopup(
                    "You entered letters or invalid characters: "
                    + invalidCharacters.join(", ")
                    + ". Student Number must contain numbers only.",
                    this
                );
            }
        });
    }


    /* =========================
       FIRST NAME
       LETTERS AND SPACES ONLY
       ========================= */

    var firstName =
        document.getElementById("FirstName");

    if (firstName) {

        firstName.addEventListener("input", function () {

            var value = this.value;

            if (/[^a-zA-ZÀ-ÿ\s'-]/.test(value)) {

                var invalidCharacters =
                    value.match(/[^a-zA-ZÀ-ÿ\s'-]/g);

                showPopup(
                    "You entered a number or invalid character: "
                    + invalidCharacters.join(", ")
                    + ". First Name should contain letters only.",
                    this
                );
            }
        });
    }


    /* =========================
       LAST NAME
       LETTERS AND SPACES ONLY
       ========================= */

    var lastName =
        document.getElementById("LastName");

    if (lastName) {

        lastName.addEventListener("input", function () {

            var value = this.value;

            if (/[^a-zA-ZÀ-ÿ\s'-]/.test(value)) {

                var invalidCharacters =
                    value.match(/[^a-zA-ZÀ-ÿ\s'-]/g);

                showPopup(
                    "You entered a number or invalid character: "
                    + invalidCharacters.join(", ")
                    + ". Last Name should contain letters only.",
                    this
                );
            }
        });
    }


    /* =========================
       EMAIL
       ========================= */

    var email =
        document.getElementById("Email");

    if (email) {

        email.addEventListener("input", function () {

            var value = this.value;

            if (value.includes(" ")) {

                showPopup(
                    "You entered a space in the email address. Please enter a valid email address.",
                    this
                );
            }
        });
    }


    /* =========================
       PHONE NUMBER
       NUMBERS ONLY
       ========================= */

    var phoneNumber =
        document.getElementById("PhoneNumber");

    if (phoneNumber) {

        phoneNumber.addEventListener("input", function () {

            var value = this.value;

            if (/[^0-9]/.test(value)) {

                var invalidCharacters =
                    value.match(/[^0-9]/g);

                showPopup(
                    "You entered letters or invalid characters: "
                    + invalidCharacters.join(", ")
                    + ". Phone Number must contain numbers only.",
                    this
                );
            }
        });
    }


    /* =========================
       QUALIFICATION
       LETTERS, NUMBERS AND
       NORMAL PUNCTUATION
       ========================= */

    var qualification =
        document.getElementById("Qualification");

    if (qualification) {

        qualification.addEventListener("input", function () {

            var value = this.value;

            if (/[^a-zA-Z0-9À-ÿ\s&().,'/-]/.test(value)) {

                var invalidCharacters =
                    value.match(
                        /[^a-zA-Z0-9À-ÿ\s&().,'/-]/g
                    );

                showPopup(
                    "You entered an invalid character: "
                    + invalidCharacters.join(", ")
                    + ". Please check the Qualification field.",
                    this
                );
            }
        });
    }


    /* =========================
       REGISTRATION YEAR
       NUMBERS ONLY
       ========================= */

    var registrationYear =
        document.getElementById("RegistrationYear");

    if (registrationYear) {

        registrationYear.addEventListener("input", function () {

            var value = this.value;

            if (/[^0-9]/.test(value)) {

                var invalidCharacters =
                    value.match(/[^0-9]/g);

                showPopup(
                    "You entered letters or invalid characters: "
                    + invalidCharacters.join(", ")
                    + ". Registration Year must contain numbers only.",
                    this
                );
            }
        });
    }


    /* =========================
       FORM SUBMISSION
       ========================= */

    var form =
        document.getElementById("registerStudentForm");

    if (form) {

        form.addEventListener("submit", function (event) {

            var studentNumberValue =
                document.getElementById("StudentNumber").value;

            var firstNameValue =
                document.getElementById("FirstName").value;

            var lastNameValue =
                document.getElementById("LastName").value;

            var phoneValue =
                document.getElementById("PhoneNumber").value;

            var yearValue =
                document.getElementById("RegistrationYear").value;


            /* Student Number */

            if (!/^[0-9]{9}$/.test(studentNumberValue)) {

                event.preventDefault();

                showPopup(
                    "Student Number must contain exactly 9 numbers.",
                    document.getElementById("StudentNumber")
                );

                return;
            }


            /* First Name */

            if (!/^[a-zA-ZÀ-ÿ\s'-]+$/.test(firstNameValue)) {

                event.preventDefault();

                showPopup(
                    "First Name should contain letters only.",
                    document.getElementById("FirstName")
                );

                return;
            }


            /* Last Name */

            if (!/^[a-zA-ZÀ-ÿ\s'-]+$/.test(lastNameValue)) {

                event.preventDefault();

                showPopup(
                    "Last Name should contain letters only.",
                    document.getElementById("LastName")
                );

                return;
            }


            /* Phone */

            if (!/^[0-9]{10}$/.test(phoneValue)) {

                event.preventDefault();

                showPopup(
                    "Phone Number must contain exactly 10 numbers.",
                    document.getElementById("PhoneNumber")
                );

                return;
            }


            /* Registration Year */

            if (!/^[0-9]{4}$/.test(yearValue)) {

                event.preventDefault();

                showPopup(
                    "Registration Year must contain exactly 4 numbers.",
                    document.getElementById("RegistrationYear")
                );

                return;
            }

        });

    }

});