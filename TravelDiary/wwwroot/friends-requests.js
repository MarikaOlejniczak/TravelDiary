
async function respondToFriendRequest(requestId, action, button) {
    const isAccept = action === "accept";

    if (!confirm(
        isAccept
            ? "Zaakceptować zaproszenie do znajomych?"
            : "Odrzucić zaproszenie do znajomych?"
    )) {
        return;
    }

    button.disabled = true;
    button.textContent = "Przetwarzanie...";

    try {
        const csrfResponse = await fetch("/api/Csrf/token", {
            credentials: "same-origin",
            cache: "no-store"
        });

        if (!csrfResponse.ok) {
            throw new Error("Nie udało się pobrać tokenu CSRF.");
        }

        const csrf = await csrfResponse.json();

        const response = await fetch(
            `/api/Friends/requests/${requestId}/${action}`,
            {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "X-CSRF-TOKEN": csrf.token
                }
            }
        );

        const result = await response.json();

        if (!response.ok) {
            throw new Error(
                result.message || "Nie udało się odpowiedzieć na zaproszenie."
            );
        }

        alert(result.message);

        await Promise.all([
            loadFriends(),
            loadRequests()
        ]);

    } catch (error) {
        alert(error.message);
        console.error(error);

        button.disabled = false;
        button.textContent = isAccept
            ? "✅ Akceptuj"
            : "❌ Odrzuć";
    }
}

function acceptFriendRequest(requestId) {
    const button = document.querySelector(
        `[data-accept-request="${requestId}"]`
    );

    if (button) {
        respondToFriendRequest(requestId, "accept", button);
    }
}

function rejectFriendRequest(requestId) {
    const button = document.querySelector(
        `[data-reject-request="${requestId}"]`
    );

    if (button) {
        respondToFriendRequest(requestId, "reject", button);
    }
}
