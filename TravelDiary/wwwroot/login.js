
const loginForm = document.getElementById("loginForm");
const loginSection = document.getElementById("loginSection");
const userSection = document.getElementById("userSection");
const message = document.getElementById("message");
const welcomeMessage = document.getElementById("welcomeMessage");
const logoutButton = document.getElementById("logoutButton");

// Pobiera token CSRF z naszego backendu.
async function getCsrfToken() {
    const response = await fetch("/api/Csrf/token", {
        method: "GET",
        credentials: "same-origin",
        cache: "no-store"
    });

    if (!response.ok) {
        throw new Error("Nie udało się pobrać tokenu CSRF.");
    }

    const data = await response.json();

    if (!data.token) {
        throw new Error("Serwer nie zwrócił tokenu CSRF.");
    }

    return data.token;
}

// Pokazuje formularz logowania.
function showLogin() {
    loginSection.hidden = false;
    userSection.hidden = true;
}

// Pokazuje powitanie zalogowanego użytkownika.
function showUser(user) {
    loginSection.hidden = true;
    userSection.hidden = false;

    welcomeMessage.textContent =
        `Witaj, ${user.displayName || user.userName}! 🌍`;
}

// Sprawdza, czy użytkownik jest zalogowany.
async function checkLogin() {
    try {
        const response = await fetch("/api/Auth/me", {
            credentials: "same-origin",
            cache: "no-store"
        });

        if (!response.ok) {
            showLogin();
            return;
        }

        const user = await response.json();
        showUser(user);

    } catch (error) {
        console.error("Błąd sprawdzania logowania:", error);
        showLogin();
    }
}

// Obsługa formularza logowania.
loginForm.addEventListener("submit", async function (event) {
    event.preventDefault();

    const email = document.getElementById("email").value;
    const password = document.getElementById("password").value;

    message.textContent = "Trwa logowanie...";

    try {
        // Pobieramy token przed wysłaniem formularza.
        const csrfToken = await getCsrfToken();

        const response = await fetch("/api/Auth/login", {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken
            },
            body: JSON.stringify({
                email: email,
                password: password
            })
        });

        if (!response.ok) {
            message.textContent =
                "Nie udało się zalogować. Sprawdź e-mail i hasło.";
            return;
        }

        loginForm.reset();
        message.textContent = "";

        await checkLogin();

    } catch (error) {
        console.error("Błąd logowania:", error);
        message.textContent =
            "Wystąpił błąd połączenia lub zabezpieczeń.";
    }
});

// Obsługa wylogowania.
logoutButton.addEventListener("click", async function () {
    logoutButton.disabled = true;

    try {
        // Pobieramy token CSRF.
        const csrfToken = await getCsrfToken();

        // Wysyłamy go w nagłówku żądania.
        const response = await fetch("/api/Auth/logout", {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "X-CSRF-TOKEN": csrfToken
            }
        });

        if (!response.ok) {
            alert("Nie udało się wylogować. Spróbuj ponownie.");
            return;
        }

        showLogin();
        message.textContent = "Wylogowano pomyślnie! 👋";

    } catch (error) {
        console.error("Błąd wylogowania:", error);
        alert("Nie można połączyć się z serwerem.");

    } finally {
        logoutButton.disabled = false;
    }
});

// Sprawdzamy sesję po otwarciu strony.
checkLogin();
