document.addEventListener("DOMContentLoaded", function () {

    var ratingInputs =
        document.querySelectorAll("input.rating-radio");

    var totalRatingElement =
        document.getElementById("totalRating");

    var finalMarkElement =
        document.getElementById("finalMark");

    var overallScoreElement =
        document.getElementById("overallScore");

    var criteriaMaximum =
        parseFloat(
            document.getElementById("criteriaMaximum").value
        );

    var criteriaCount =
        parseInt(
            document.getElementById("criteriaCount").value,
            10
        );


    /* =====================================================
       GET SELECTED RATING
    ===================================================== */

    function getSelectedRating(index) {

        var selected = document.querySelector(
            'input.rating-radio[data-index="' +
            index +
            '"]:checked'
        );

        if (!selected) {
            return null;
        }

        var rating =
            parseInt(selected.value, 10);

        if (isNaN(rating)) {
            return null;
        }

        return rating;
    }


    /* =====================================================
       UPDATE COMMENT REQUIREMENT
    ===================================================== */

    function updateCommentRequirement(index) {

        var rating =
            getSelectedRating(index);

        var label =
            document.querySelector(
                '[data-comment-label="' +
                index +
                '"]'
            );

        var help =
            document.querySelector(
                '[data-comment-help="' +
                index +
                '"]'
            );

        var input =
            document.querySelector(
                '[data-comment-input="' +
                index +
                '"]'
            );

        var error =
            document.querySelector(
                '[data-comment-error="' +
                index +
                '"]'
            );


        if (!label || !help || !input) {
            return;
        }


        if (rating !== null && rating <= 3) {

            label.textContent =
                "Required for rating " +
                rating;

            label.classList.add("required");

            help.textContent =
                "Please explain the reason for awarding a rating of " +
                rating +
                ". A comment is required.";

            input.classList.add(
                "comment-required"
            );

        }
        else {

            label.textContent =
                "Optional for ratings 4–5";

            label.classList.remove(
                "required"
            );

            help.textContent =
                "Add a short explanation supporting the mark awarded.";

            input.classList.remove(
                "comment-required"
            );

        }


        if (error) {
            error.textContent = "";
        }
    }


    /* =====================================================
       UPDATE INDIVIDUAL CRITERION SCORE
    ===================================================== */

    function updateCriterionDisplay(index) {

        var rating =
            getSelectedRating(index);

        var scoreElement =
            document.querySelector(
                '.criterion-score-display[data-index="' +
                index +
                '"]'
            );


        if (!scoreElement) {
            return;
        }


        if (rating === null) {

            scoreElement.textContent = "—";

        }
        else {

            scoreElement.textContent =
                rating.toString();

        }
    }


    /* =====================================================
       CALCULATE TOTAL SCORE
    ===================================================== */

    function calculateScores() {

        var totalMarks = 0;


        ratingInputs.forEach(function (element) {

            var ratingInput =
                element;


            if (!ratingInput.checked) {
                return;
            }


            var mark =
                parseInt(
                    ratingInput.value,
                    10
                );


            if (!isNaN(mark)) {

                totalMarks += mark;

            }

        });


        var markOutOf10 = 0;

        var percentage = 0;


        if (criteriaMaximum > 0) {

            markOutOf10 =
                (totalMarks /
                    criteriaMaximum) *
                10;

            percentage =
                (totalMarks /
                    criteriaMaximum) *
                100;

        }


        if (totalRatingElement) {

            totalRatingElement.textContent =
                totalMarks.toFixed(0);

        }


        if (finalMarkElement) {

            finalMarkElement.textContent =
                markOutOf10.toFixed(2);

        }


        if (overallScoreElement) {

            overallScoreElement.textContent =
                percentage.toFixed(2);

        }


        for (
            var i = 0;
            i < criteriaCount;
            i++
        ) {

            updateCriterionDisplay(i);

            updateCommentRequirement(i);

        }

    }


    /* =====================================================
       RATING CHANGE
    ===================================================== */

    ratingInputs.forEach(function (element) {

        element.addEventListener(
            "change",
            calculateScores
        );

    });


    /* =====================================================
       FORM VALIDATION
    ===================================================== */

    var evaluationForm =
        document.getElementById(
            "evaluationForm"
        );


    if (evaluationForm) {

        evaluationForm.addEventListener(
            "submit",
            function (event) {

                var invalidComments = [];


                for (
                    var i = 0;
                    i < criteriaCount;
                    i++
                ) {

                    var rating =
                        getSelectedRating(i);


                    var commentInput =
                        document.querySelector(
                            '[data-comment-input="' +
                            i +
                            '"]'
                        );


                    var error =
                        document.querySelector(
                            '[data-comment-error="' +
                            i +
                            '"]'
                        );


                    if (error) {

                        error.textContent = "";

                    }


                    /*
                     * Ratings 1–3 require a comment.
                     */

                    if (
                        rating !== null &&
                        rating <= 3 &&
                        commentInput &&
                        commentInput.value.trim() === ""
                    ) {

                        invalidComments.push(i);


                        commentInput.classList.add(
                            "comment-required"
                        );


                        if (error) {

                            error.textContent =
                                "A comment is required for this rating.";

                        }

                    }

                }


                if (
                    invalidComments.length > 0
                ) {

                    event.preventDefault();


                    var firstInvalid =
                        invalidComments[0];


                    var firstInvalidInput =
                        document.querySelector(
                            '[data-comment-input="' +
                            firstInvalid +
                            '"]'
                        );


                    if (firstInvalidInput) {

                        firstInvalidInput.focus();


                        firstInvalidInput.scrollIntoView({
                            behavior: "smooth",
                            block: "center"
                        });

                    }


                    return false;

                }

            }
        );

    }


    /* =====================================================
       INITIAL CALCULATION
    ===================================================== */

    calculateScores();

});