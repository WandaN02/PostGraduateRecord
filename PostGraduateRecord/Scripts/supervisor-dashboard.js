document.addEventListener("DOMContentLoaded", function () {

    /* =========================================================
       SEARCH
    ========================================================= */

    const searchInput =
        document.getElementById("supervisorSearch");

    const clearButton =
        document.getElementById("clearSearch");

    const searchableItems =
        document.querySelectorAll(".searchable-item");


    if (searchInput) {

        searchInput.addEventListener("input", function () {

            const searchTerm =
                searchInput.value
                    .toLowerCase()
                    .trim();


            searchableItems.forEach(function (item) {

                const itemText =
                    item.textContent.toLowerCase();


                if (
                    searchTerm === "" ||
                    itemText.includes(searchTerm)
                ) {

                    item.classList.remove("search-hidden");

                } else {

                    item.classList.add("search-hidden");

                }

            });


            if (clearButton) {

                clearButton.style.display =
                    searchTerm.length > 0
                        ? "flex"
                        : "none";

            }

        });


        if (clearButton) {

            clearButton.addEventListener(
                "click",
                function () {

                    searchInput.value = "";

                    searchInput.dispatchEvent(
                        new Event("input")
                    );

                    searchInput.focus();

                }
            );

        }

    }


    /* =========================================================
       AI ASSISTANT ELEMENTS
    ========================================================= */

    const aiPanel =
        document.getElementById("supervisorAI");

    const openAI =
        document.getElementById("openSupervisorAI");

    const closeAI =
        document.getElementById("closeSupervisorAI");

    const aiInput =
        document.getElementById("supervisorAIInput");

    const aiSend =
        document.getElementById("supervisorAISend");

    const chatBody =
        document.getElementById("supervisorAIChat");


    /* =========================================================
       OPEN AI ASSISTANT
    ========================================================= */

    function openAssistant() {

        if (!aiPanel) {
            return;
        }

        aiPanel.classList.add("ai-open");

        setTimeout(function () {

            if (aiInput) {
                aiInput.focus();
            }

        }, 100);

    }


    /* =========================================================
       CLOSE AI ASSISTANT
    ========================================================= */

    function closeAssistant() {

        if (!aiPanel) {
            return;
        }

        aiPanel.classList.remove("ai-open");

    }


    /* =========================================================
       ADD USER MESSAGE
    ========================================================= */

    function addUserMessage(message) {

        if (!chatBody) {
            return;
        }


        const wrapper =
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


        /* =====================================================
           EDIT MESSAGE
        ===================================================== */

        const editButton =
            wrapper.querySelector(".ai-edit-button");


        if (editButton) {

            editButton.addEventListener(
                "click",
                function (event) {

                    event.preventDefault();
                    event.stopPropagation();


                    if (aiInput) {

                        aiInput.value = message;

                        aiInput.focus();


                        aiInput.classList.add(
                            "ai-editing"
                        );


                        setTimeout(function () {

                            aiInput.classList.remove(
                                "ai-editing"
                            );

                        }, 1200);

                    }


                    wrapper.remove();

                    scrollChat();

                }
            );

        }


        scrollChat();

    }


    /* =========================================================
       ADD AI MESSAGE
    ========================================================= */

    function addAIMessage(message) {

        if (!chatBody) {
            return;
        }


        const wrapper =
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


    /* =========================================================
       ESCAPE HTML
    ========================================================= */

    function escapeHtml(text) {

        const div =
            document.createElement("div");

        div.textContent =
            text || "";

        return div.innerHTML;

    }


    /* =========================================================
       SCROLL CHAT TO BOTTOM
    ========================================================= */

    function scrollChat() {

        if (!chatBody) {
            return;
        }

        chatBody.scrollTop =
            chatBody.scrollHeight;

    }


    /* =========================================================
       SEND QUESTION TO AI
    ========================================================= */

    async function sendQuestion() {

        if (!aiInput) {
            return;
        }


        const question =
            aiInput.value.trim();


        if (question === "") {
            return;
        }


        /* =====================================================
           SHOW USER MESSAGE
        ===================================================== */

        addUserMessage(question);

        aiInput.value = "";


        /* =====================================================
           DISABLE SEND BUTTON
        ===================================================== */

        if (aiSend) {
            aiSend.disabled = true;
        }


        /* =====================================================
           SHOW THINKING MESSAGE
        ===================================================== */

        let thinkingWrapper = null;


        if (chatBody) {

            thinkingWrapper =
                document.createElement("div");

            thinkingWrapper.className =
                "ai-message ai-message-bot";

            thinkingWrapper.id =
                "aiThinkingMessage";


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


        try {

            /* =================================================
               SEND REQUEST TO AI CONTROLLER
            ================================================= */

            const response =
                await fetch(
                    window.supervisorAIUrl,
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json; charset=utf-8"
                        },

                        body: JSON.stringify({

                            Message: question,

                            Role: "Supervisor"

                        })

                    }
                );


            /* =================================================
               READ RESPONSE
            ================================================= */

            const data =
                await response.json();


            /* =================================================
               REMOVE THINKING MESSAGE
            ================================================= */

            if (thinkingWrapper) {
                thinkingWrapper.remove();
            }


            /* =================================================
               DISPLAY RESPONSE
            ================================================= */

            if (data.Success) {

                addAIMessage(
                    data.Response
                );

            } else {

                addAIMessage(
                    "Sorry, I couldn't process your question.\n\n" +
                    (
                        data.Error ||
                        "An unknown error occurred."
                    )
                );

            }

        }
        catch (error) {

            console.error(
                "SPGS AI Error:",
                error
            );


            /* =================================================
               REMOVE THINKING MESSAGE
            ================================================= */

            if (thinkingWrapper) {
                thinkingWrapper.remove();
            }


            addAIMessage(
                "I couldn't connect to the AI service. " +
                "Please check your connection and try again."
            );

        }
        finally {

            /* =================================================
               RE-ENABLE SEND BUTTON
            ================================================= */

            if (aiSend) {
                aiSend.disabled = false;
            }


            if (aiInput) {
                aiInput.focus();
            }

        }

    }


    /* =========================================================
       OPEN AI BUTTON
    ========================================================= */

    if (openAI) {

        openAI.addEventListener(
            "click",
            openAssistant
        );

    }


    /* =========================================================
       CLOSE AI BUTTON
    ========================================================= */

    if (closeAI) {

        closeAI.addEventListener(
            "click",
            closeAssistant
        );

    }


    /* =========================================================
       SEND BUTTON
    ========================================================= */

    if (aiSend) {

        aiSend.addEventListener(
            "click",
            sendQuestion
        );

    }


    /* =========================================================
       ENTER KEY
    ========================================================= */

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


    /* =========================================================
       AI SUGGESTIONS
    ========================================================= */

    document
        .querySelectorAll(".ai-suggestion")
        .forEach(function (button) {

            button.addEventListener(
                "click",
                function () {

                    const question =
                        button.getAttribute(
                            "data-question"
                        );


                    if (!question || !aiInput) {
                        return;
                    }


                    aiInput.value =
                        question;


                    sendQuestion();

                }
            );

        });

});