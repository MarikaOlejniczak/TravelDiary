
const registerForm = document.getElementById("registerForm");
const message = document.getElementById("message");

// Reagujemy na kliknięcie przycisku "Zarejestruj się"
registerForm.addEventListener("submit", async function (event) {

    // Zapobiegamy przeładowaniu strony
    event.preventDefault();

    // Pobieramy dane wpisane przez użytkownika
    const userName = document.getElementById("userName").value.trim();
    const displayName = document.getElementById("displayName").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;

    // Sprawdzamy, czy hasła są identyczne
    if (password !== confirmPassword) {
        message.textContent = "Hasła muszą być identyczne!";
        return;
    }

    const submitButton = registerForm.querySelector('button[type="submit"]');

    submitButton.disabled = true;
    message.textContent = "Trwa tworzenie konta...";

    try {
        // Wysyłamy dane do backendu ASP.NET Core
        const response = await fetch("/api/Auth/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                userName: userName,
                displayName: displayName,
                email: email,
                password: password
            })
        });

        // Odczytujemy odpowiedź serwera
        const data = await response.json();

        if (!response.ok) {

            // ASP.NET Core Identity może zwrócić listę błędów
            if (Array.isArray(data)) {
                message.textContent = data
                    .map(error => error.description)
                    .join(" ");
            } else {
                message.textContent =
                    data.message || "Nie udało się utworzyć konta.";
            }

            return;
        }

        // Rejestracja zakończona sukcesem
        message.textContent =
            "Konto zostało utworzone! Możesz się teraz zalogować. 🎉";

        registerForm.reset();

    } catch (error) {
        console.error("Błąd rejestracji:", error);

        message.textContent =
            "Nie można połączyć się z serwerem.";

    } finally {
        submitButton.disabled = false;
    }
});
