document.addEventListener("DOMContentLoaded", function () {

    // =========================================
    // VIEW TIMELINE BUTTONS
    // =========================================

    document.querySelectorAll(".timeline-view-button").forEach(function (button) {

        button.addEventListener("click", function () {

            showStudentTimeline(button);

        });

    });


    // =========================================
    // FILTER STUDENTS
    // =========================================

    document.querySelectorAll(".timeline-filter").forEach(function (button) {

        button.addEventListener("click", function () {

            var filter =
                button.getAttribute("data-filter");


            // Remove active state

            document.querySelectorAll(".timeline-filter").forEach(function (btn) {

                btn.classList.remove("active");

            });


            // Add active state

            button.classList.add("active");


            // Filter student cards

            document.querySelectorAll(".student-timeline-card").forEach(function (card) {

                var status =
                    card.getAttribute("data-student-status");


                if (filter === "all" || status === filter) {

                    card.style.display = "";

                }
                else {

                    card.style.display = "none";

                }

            });

        });

    });


    // =========================================
    // CLOSE MODAL WHEN CLICKING OUTSIDE
    // =========================================

    var timelineModal =
        document.getElementById("timelineModal");


    if (timelineModal) {

        timelineModal.addEventListener(
            "click",
            function (event) {

                if (event.target === timelineModal) {

                    closeTimelineDetails();

                }

            }
        );

    }


    // =========================================
    // CLOSE WITH ESCAPE KEY
    // =========================================

    document.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Escape") {

                closeTimelineDetails();

            }

        }
    );

});


// =========================================
// SHOW STUDENT TIMELINE
// =========================================

function showStudentTimeline(button) {

    var card =
        button.closest(".student-timeline-card");


    if (!card) {

        return;

    }


    var student =
        button.getAttribute("data-student");


    var title =
        button.getAttribute("data-title");


    var progress =
        button.getAttribute("data-progress");


    // =========================================
    // STUDENT INFORMATION
    // =========================================

    document.getElementById("modalStudent").textContent =
        student;


    document.getElementById("modalStudentName").textContent =
        student;


    document.getElementById("modalTitle").textContent =
        title;


    document.getElementById("modalResearchTitle").textContent =
        title;


    document.getElementById("modalProgress").textContent =
        progress + "%";


    document.getElementById("modalSubtitle").textContent =
        "Postgraduate research progress";


    // =========================================
    // GET STAGES FROM CARD
    // =========================================

    var stages =
        card.querySelectorAll(".timeline-stage");


    var modalStages =
        document.getElementById("modalTimelineStages");


    modalStages.innerHTML = "";


    var completed = 0;


    stages.forEach(function (stage) {

        var stageNameElement =
            stage.querySelector("h4");


        var descriptionElement =
            stage.querySelector("p");


        var statusElement =
            stage.querySelector(".timeline-status");


        var dateElement =
            stage.querySelector(".timeline-stage-date");


        var stageName =
            stageNameElement
                ? stageNameElement.textContent.trim()
                : "";


        var description =
            descriptionElement
                ? descriptionElement.textContent.trim()
                : "";


        var status =
            statusElement
                ? statusElement.textContent.trim()
                : "Pending";


        var date =
            dateElement
                ? dateElement.textContent.trim()
                : "No date";


        if (status.includes("Completed")) {

            completed++;

        }


        var stageHtml = "";


        if (status.includes("Completed")) {

            stageHtml =

                '<div class="modal-timeline-stage completed">' +

                '<div class="modal-timeline-marker">' +

                '<i class="fa-solid fa-check"></i>' +

                '</div>' +

                '<div class="modal-timeline-content">' +

                '<strong>' +
                stageName +
                '</strong>' +

                '<p>' +
                description +
                '</p>' +

                '<span>' +
                date +
                '</span>' +

                '</div>' +

                '</div>';

        }
        else if (status.includes("In Progress")) {

            stageHtml =

                '<div class="modal-timeline-stage in-progress">' +

                '<div class="modal-timeline-marker">' +

                '<i class="fa-solid fa-spinner"></i>' +

                '</div>' +

                '<div class="modal-timeline-content">' +

                '<strong>' +
                stageName +
                '</strong>' +

                '<p>' +
                description +
                '</p>' +

                '<span>' +
                date +
                '</span>' +

                '</div>' +

                '</div>';

        }
        else {

            stageHtml =

                '<div class="modal-timeline-stage pending">' +

                '<div class="modal-timeline-marker">' +

                '<i class="fa-solid fa-clock"></i>' +

                '</div>' +

                '<div class="modal-timeline-content">' +

                '<strong>' +
                stageName +
                '</strong>' +

                '<p>' +
                description +
                '</p>' +

                '<span>' +
                date +
                '</span>' +

                '</div>' +

                '</div>';

        }


        modalStages.insertAdjacentHTML(
            "beforeend",
            stageHtml
        );

    });


    // =========================================
    // MODAL SUMMARY
    // =========================================

    document.getElementById("modalCompletedStages").textContent =
        completed;


    document.getElementById("modalTotalStages").textContent =
        stages.length;


    // =========================================
    // OPEN MODAL
    // =========================================

    document.getElementById("timelineModal")
        .classList.add("show");

}


// =========================================
// CLOSE MODAL
// =========================================

function closeTimelineDetails() {

    var modal =
        document.getElementById("timelineModal");


    if (modal) {

        modal.classList.remove("show");

    }

}