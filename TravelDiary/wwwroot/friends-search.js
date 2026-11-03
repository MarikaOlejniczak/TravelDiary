
async function sendFriendRequest(receiverId, button) {
    button.disabled = true;
    button.textContent = "Wysyłanie...";

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
            "/api/Friends/request/" +
            encodeURIComponent(receiverId),
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
                result.message || "Nie udało się wysłać zaproszenia."
            );
        }

        button.textContent = "✅ Zaproszenie wysłane";
        alert(result.message);

    } catch (error) {
        alert(error.message);
        console.error(error);

        button.disabled = false;
        button.textContent = "💌 Zaproś do znajomych";
    }
}

async function searchUsers() {
    const query = document
        .getElementById("userSearch")
        .value
        .trim();

    const status = document.getElementById("searchStatus");
    const results = document.getElementById("searchResults");

    results.replaceChildren();

    if (query.length < 2) {
        status.textContent = "Wpisz przynajmniej 2 znaki.";
        return;
    }

    status.textContent = "Szukam podróżników... 🔎";

    try {
        const response = await fetch(
            "/api/Users/search?query=" +
            encodeURIComponent(query),
            {
                credentials: "same-origin",
                cache: "no-store"
            }
        );

        if (!response.ok) {
            throw new Error(
                "Błąd wyszukiwania: " + response.status
            );
        }

        const users = await response.json();

        if (users.length === 0) {
            status.textContent = "Nie znaleziono użytkowników.";
            return;
        }

        status.textContent =
            `Znaleziono użytkowników: ${users.length}`;

        for (const user of users) {
            const card = document.createElement("div");

            const name = document.createElement("h3");
            name.textContent = user.displayName;

            const username = document.createElement("p");
            username.textContent = "@" + user.userName;

            const link = document.createElement("a");
            link.href = "/profile.html?id=" +
                encodeURIComponent(user.id);
            link.textContent = "👤 Zobacz profil";

            const inviteButton = document.createElement("button");
            inviteButton.type = "button";
            inviteButton.textContent = "💌 Zaproś do znajomych";

            inviteButton.addEventListener("click", () => {
                sendFriendRequest(user.id, inviteButton);
            });

            card.append(name, username, link, inviteButton);
            results.appendChild(card);
        }

    } catch (error) {
        status.textContent =
            "Nie udało się wyszukać użytkowników.";
        console.error(error);
    }
}

document.getElementById("searchButton")
    .addEventListener("click", searchUsers);

document.getElementById("userSearch")
    .addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            searchUsers();
        }
    });
