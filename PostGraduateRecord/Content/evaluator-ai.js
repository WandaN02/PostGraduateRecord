document.addEventListener("DOMContentLoaded", function () {

    // =====================================================
    // GET ELEMENTS
    // =====================================================

    var aiPanel = document.getElementById("evaluatorAI");
    var openAI = document.getElementById("openEvaluatorAI");
    var closeAI = document.getElementById("closeEvaluatorAI");
    var aiInput = document.getElementById("evaluatorAIInput");
    var aiSend = document.getElementById("evaluatorAISend");
    var chatBody = document.getElementById("evaluatorAIChat");


    // =====================================================
    // OPEN AI ASSISTANT
    // =====================================================

    function openAssistant() {

        if (!aiPanel) {
            return;
        }

        aiPanel.classList.add("ai-open");

        if (aiInput) {
            aiInput.focus();
        }
    }


    // =====================================================
    // CLOSE AI ASSISTANT
    // =====================================================

    function closeAssistant() {

        if (!aiPanel) {
            return;
        }

        aiPanel.classList.remove("ai-open");
    }


    // =====================================================
    // ESCAPE HTML
    // =====================================================

    function escapeHtml(text) {

        if (text === null || text === undefined) {
            return "";
        }

        var div = document.createElement("div");

        div.textContent = String(text);

        return div.innerHTML;
    }


    // =====================================================
    // SCROLL CHAT TO BOTTOM
    // =====================================================

    function scrollChat() {

        if (!chatBody) {
            return;
        }

        chatBody.scrollTop = chatBody.scrollHeight;
    }


    // =====================================================
    // ADD USER MESSAGE
    // =====================================================

    function addUserMessage(message) {

        if (!chatBody) {
            return;
        }

        var wrapper =
            document.createElement("div");

        wrapper.className =
            "ai-message ai-user-message";


        wrapper.innerHTML =
            '<div class="ai-message-content">' +

            '<strong>You</strong>' +

            '<p class="ai-user-text">' +
            escapeHtml(message) +
            '</p>' +

            '<button type="button" ' +
            'class="ai-edit-button" ' +
            'title="Edit message" ' +
            'aria-label="Edit message">' +

            '<i class="fa-solid fa-pen"></i>' +

            '</button>' +

            '</div>';


        chatBody.appendChild(wrapper);


        // =================================================
        // EDIT USER MESSAGE
        // =================================================

        var editButton =
            wrapper.querySelector(".ai-edit-button");


        if (editButton) {

            editButton.addEventListener(
                "click",
                function (event) {

                    event.preventDefault();
                    event.stopPropagation();


                    if (aiInput) {

                        aiInput.value =
                            message;

                        aiInput.focus();

                        aiInput.classList.add(
                            "ai-editing"
                        );


                        setTimeout(
                            function () {

                                aiInput.classList.remove(
                                    "ai-editing"
                                );

                            },
                            1200
                        );
                    }


                    wrapper.remove();

                    scrollChat();
                }
            );
        }


        scrollChat();
    }


    // =====================================================
    // ADD AI MESSAGE
    // =====================================================

    function addAIMessage(message) {

        if (!chatBody) {
            return;
        }

        var wrapper =
            document.createElement("div");

        wrapper.className =
            "ai-message ai-message-bot";


        wrapper.innerHTML =
            '<div class="ai-message-icon">✦</div>' +

            '<div class="ai-message-content">' +

            '<strong>SPGS AI</strong>' +

            '<p>' +
            escapeHtml(message)
                .replace(/\n/g, "<br>") +
            '</p>' +

            '</div>';


        chatBody.appendChild(wrapper);

        scrollChat();
    }


    // =====================================================
    // SEND QUESTION
    // =====================================================

    function sendQuestion() {

        if (!aiInput) {
            return;
        }


        var question =
            aiInput.value.trim();


        // Don't send an empty question
        if (question === "") {
            return;
        }


        // =================================================
        // SHOW USER QUESTION
        // =================================================

        addUserMessage(question);


        // Clear input
        aiInput.value = "";


        // =================================================
        // DISABLE SEND BUTTON
        // =================================================

        if (aiSend) {
            aiSend.disabled = true;
        }


        // =================================================
        // THINKING MESSAGE
        // =================================================

        var thinkingWrapper = null;


        if (chatBody) {

            thinkingWrapper =
                document.createElement("div");

            thinkingWrapper.className =
                "ai-message ai-message-bot";


            thinkingWrapper.innerHTML =
                '<div class="ai-message-icon">✦</div>' +

                '<div class="ai-message-content">' +

                '<strong>SPGS AI</strong>' +

                '<p>Thinking...</p>' +

                '</div>';


            chatBody.appendChild(
                thinkingWrapper
            );

            scrollChat();
        }


        // =================================================
        // CHECK AI URL
        // =================================================

        if (
            typeof evaluatorAIUrl === "undefined" ||
            !evaluatorAIUrl
        ) {

            if (thinkingWrapper) {
                thinkingWrapper.remove();
            }

            addAIMessage(
                "The AI service URL is not configured."
            );

            if (aiSend) {
                aiSend.disabled = false;
            }

            return;
        }


        // =================================================
        // SEND REQUEST TO AI CONTROLLER
        // =================================================

        fetch(
            evaluatorAIUrl,
            {
                method: "POST",

                headers: {
                    "Content-Type":
                        "application/json; charset=utf-8"
                },

                body: JSON.stringify({

                    Message: question,

                    Role: "Evaluator"

                })
            }
        )


            // =================================================
            // CHECK RESPONSE
            // =================================================

            .then(function (response) {

                if (!response.ok) {

                    throw new Error(
                        "HTTP error: " +
                        response.status
                    );
                }

                return response.json();
            })


            // =================================================
            // PROCESS AI RESPONSE
            // =================================================

            .then(function (data) {

                // Remove Thinking...
                if (thinkingWrapper) {

                    thinkingWrapper.remove();
                }


                // Successful response
                if (data && data.Success) {

                    addAIMessage(
                        data.Response || "No response received."
                    );

                }

                // Error returned by controller
                else {

                    addAIMessage(

                        "Sorry, I couldn't process " +
                        "your question.\n\n" +

                        (
                            data && data.Error
                                ? data.Error
                                : "An unknown error occurred."
                        )
                    );
                }
            })


            // =================================================
            // CONNECTION ERROR
            // =================================================

            .catch(function (error) {

                console.error(
                    "SPGS Evaluator AI Error:",
                    error
                );


                if (thinkingWrapper) {

                    thinkingWrapper.remove();
                }


                addAIMessage(
                    "I couldn't connect to the AI service. " +
                    "Please check your connection and try again."
                );
            })


            // =================================================
            // ENABLE SEND BUTTON AGAIN
            // =================================================

            .finally(function () {

                if (aiSend) {

                    aiSend.disabled = false;
                }


                if (aiInput) {

                    aiInput.focus();
                }
            });
    }


    // =====================================================
    // OPEN BUTTON
    // =====================================================

    if (openAI) {

        openAI.addEventListener(
            "click",
            openAssistant
        );
    }


    // =====================================================
    // CLOSE BUTTON
    // =====================================================

    if (closeAI) {

        closeAI.addEventListener(
            "click",
            closeAssistant
        );
    }


    // =====================================================
    // SEND BUTTON
    // =====================================================

    if (aiSend) {

        aiSend.addEventListener(
            "click",
            sendQuestion
        );
    }


    // =====================================================
    // ENTER KEY
    // =====================================================

    if (aiInput) {

        aiInput.addEventListener(
            "keydown",
            function (event) {

                if (event.key === "Enter") {

                    event.preventDefault();

                    sendQuestion();
                }
            }
        );
    }


    // =====================================================
    // SUGGESTION BUTTONS
    // =====================================================

    var suggestionButtons =
        document.querySelectorAll(
            "#evaluatorAI .ai-suggestion"
        );


    suggestionButtons.forEach(
        function (button) {

            button.addEventListener(
                "click",
                function () {

                    var question =
                        button.getAttribute(
                            "data-question"
                        );


                    if (
                        !question ||
                        !aiInput
                    ) {

                        return;
                    }


                    aiInput.value =
                        question;


                    sendQuestion();
                }
            );
        }
    );

});