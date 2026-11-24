

        document.addEventListener("DOMContentLoaded", function () {

            const searchInput =
                document.getElementById("submissionSearch");

            const clearButton =
                document.getElementById("clearSubmissionSearch");

            const submissions =
                document.querySelectorAll(".searchable-submission");

            const noResults =
                document.getElementById("noSearchResults");


            if (!searchInput) {
                return;
            }


            searchInput.addEventListener("input", function () {

                const searchTerm =
                    searchInput.value
                        .toLowerCase()
                        .trim();

                let visibleCount = 0;


                submissions.forEach(function (submission) {

                    const submissionText =
                        submission.textContent
                            .toLowerCase();


                    if (
                        searchTerm === "" ||
                        submissionText.includes(searchTerm)
                    ) {

                        submission.classList.remove(
                            "search-hidden"
                        );

                        visibleCount++;

                    }
                    else {

                        submission.classList.add(
                            "search-hidden"
                        );

                    }

                });


                if (searchTerm.length > 0) {

                    clearButton.style.display = "flex";

                }
                else {

                    clearButton.style.display = "none";

                }


                if (
                    searchTerm.length > 0 &&
                    visibleCount === 0
                ) {

                    noResults.style.display = "block";

                }
                else {

                    noResults.style.display = "none";

                }

            });


            clearButton.addEventListener("click", function () {

                searchInput.value = "";

                searchInput.dispatchEvent(
                    new Event("input")
                );

                searchInput.focus();

            });

        });

