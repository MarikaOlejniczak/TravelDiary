
'use strict';

const $ = (id) => document.getElementById(id);
const params = new URLSearchParams(window.location.search);

let profileMap = null;
let profileMarkers = null;
let travelPin = null;

async function getJson(path) {
    const response = await fetch(path, {
        credentials: 'same-origin',
        cache: 'no-store'
    });

    if (!response.ok) {
        throw new Error(`Błąd HTTP ${response.status}`);
    }

    return response.json();
}

async function saveProfile(data) {
    const csrf = await getJson('/api/Csrf/token');

    const response = await fetch('/api/Users/me', {
        method: 'PUT',
        credentials: 'same-origin',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': csrf.token
        },
        body: JSON.stringify(data)
    });

    if (!response.ok) {
        throw new Error(`Nie udało się zapisać profilu (${response.status}).`);
    }

    return response.json();
}

function validCoordinates(post) {
    const lat = Number(post.latitude);
    const lng = Number(post.longitude);

    return post.latitude != null &&
        post.longitude != null &&
        Number.isFinite(lat) &&
        Number.isFinite(lng) &&
        lat >= -90 &&
        lat <= 90 &&
        lng >= -180 &&
        lng <= 180;
}

function initializeMap() {
    if (typeof L === 'undefined') {
        $('mapHint').textContent =
            'Nie udało się wczytać biblioteki mapy.';
        return;
    }


    travelPin = L.icon({
        iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
        iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
        shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    });


    profileMap = L.map('profileMap', {
        scrollWheelZoom: false
    }).setView([51.9194, 19.1451], 5);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(profileMap);

    profileMarkers = L.layerGroup().addTo(profileMap);
}

function renderMap(posts) {
    if (!profileMap) return;

    profileMarkers.clearLayers();

    const points = [];

    for (const post of posts) {
        if (!validCoordinates(post)) continue;

        const coords = [
            Number(post.latitude),
            Number(post.longitude)
        ];

        points.push(coords);

        const popup = document.createElement('div');

        const title = document.createElement('strong');
        title.textContent =
            post.title || 'Wspomnienie z podróży';

        const link = document.createElement('a');
        link.href = `#post-${post.id}`;
        link.textContent = '📖 Zobacz ten wpis';
        link.style.display = 'block';
        link.style.marginTop = '8px';

        link.addEventListener('click', (event) => {
            event.preventDefault();

            const postElement = document.getElementById(`post-${post.id}`);

            if (postElement) {
                postElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'center'
                });

                postElement.style.transition = 'box-shadow 0.3s';
                postElement.style.boxShadow = '0 0 0 3px #16B8B2';

                setTimeout(() => {
                    postElement.style.boxShadow = '';
                }, 1800);
            }
        });

        popup.append(title, link);

        L.marker(coords, { icon: travelPin })
            .addTo(profileMarkers)
            .bindPopup(popup);
    }

    $('placesCount').textContent =
        `${points.length} ${points.length === 1 ? 'miejsce' : 'miejsc'}`;

    $('mapHint').textContent = points.length
        ? 'Kliknij pinezkę, aby zobaczyć wspomnienie.'
        : 'Ten podróżnik nie dodał jeszcze wpisów z lokalizacją.';

    if (points.length === 1) {
        profileMap.setView(points[0], 9);
    } else if (points.length > 1) {
        profileMap.fitBounds(points, {
            padding: [35, 35],
            maxZoom: 10
        });
    }

    setTimeout(() => {
        if (profileMap) {
            profileMap.invalidateSize();
        }
    }, 200);
}

function renderPosts(posts) {
    const grid = $('postsGrid');
    grid.replaceChildren();

    $('postsCount').textContent =
        `${posts.length} ${posts.length === 1 ? 'wpis' : 'wpisów'}`;

    $('postsStatus').textContent = posts.length
        ? ''
        : 'Ten podróżnik nie dodał jeszcze żadnych wpisów. ✈️';

    for (const post of posts) {
        const article = document.createElement('article');
        article.className = 'post-card';
        article.id = `post-${post.id}`;

        const media = document.createElement('div');
        media.className = 'post-media';

        if (
            Array.isArray(post.images) &&
            post.images.length &&
            typeof post.images[0] === 'string' &&
            post.images[0].startsWith('/')
        ) {
            const img = document.createElement('img');
            img.src = post.images[0];
            img.alt = `Zdjęcie z podróży: ${post.title || 'wspomnienie'}`;
            img.loading = 'lazy';
            media.append(img);
        } else {
            media.classList.add('post-media-empty');
            media.textContent = '🌴';
        }

        const content = document.createElement('div');
        content.className = 'post-body';

        const date = document.createElement('p');
        date.className = 'post-date';

        const d = new Date(post.createdAt);

        date.textContent = Number.isNaN(d.getTime())
            ? 'Wspomnienie z podróży'
            : d.toLocaleDateString('pl-PL', {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });

        const title = document.createElement('h3');
        title.textContent =
            post.title || 'Wspomnienie z podróży';

        const description = document.createElement('p');
        description.className = 'post-description';
        description.textContent =
            post.content || 'Podróż zapisana na mapie.';

        const link = document.createElement('a');
        link.className = 'post-link';
        link.href = `#post-${post.id}`;
        link.textContent = '📖 Otwórz wspomnienie →';

        link.addEventListener('click', (event) => {
            event.preventDefault();

            const dialog = $('postDialog');
            const dialogTitle = $('postDialogTitle');
            const dialogContent = $('postDialogContent');

            dialogTitle.textContent = post.title || 'Wspomnienie z podróży';
            dialogContent.replaceChildren();

            const description = document.createElement('p');
            description.textContent = post.content || '';
            dialogContent.append(description);

            if (Array.isArray(post.images)) {
                for (const imageUrl of post.images) {
                    if (
                        typeof imageUrl !== 'string' ||
                        !/^\/api\/post-images\/\d+$/.test(imageUrl)
                    ) {
                        continue;
                    }

                    const image = document.createElement('img');
                    image.src = imageUrl;
                    image.alt = post.title || 'Zdjęcie z podróży';
                    image.loading = 'lazy';
                    image.style.maxWidth = '100%';
                    image.style.height = 'auto';
                    image.style.display = 'block';
                    image.style.marginTop = '16px';

                    dialogContent.append(image);
                }
            }

            dialog.showModal();
        });

        content.append(date, title, description, link);
        article.append(media, content);
        grid.append(article);
    }
}

async function loadProfile() {
    try {
        const me = await getJson('/api/Auth/me');
        const requestedId = params.get('id');
        const profileId = requestedId || me.id;

        const user = await getJson(
            `/api/Users/${encodeURIComponent(profileId)}`
        );

        $('displayName').textContent =
            user.displayName || user.userName || 'Podróżnik';

        $('userName').textContent =
            user.userName ? '@' + user.userName : '';

        $('bio').textContent =
            user.bio ||
            'Jeszcze bez opisu — przygody mówią same za siebie!';

        const joinedDate = new Date(user.createdAt);

        $('joined').textContent =
            Number.isNaN(joinedDate.getTime())
                ? ''
                : '📅 W TravelDiary od ' +
                joinedDate.toLocaleDateString('pl-PL');

        $('avatar').textContent =
            (user.displayName || user.userName || '🌍')
                .trim()
                .charAt(0)
                .toUpperCase();

        $('profile').hidden = false;
        $('status').hidden = true;

        if (String(user.id) === String(me.id)) {
            $('editToggle').hidden = false;
            $('editDisplayName').value =
                user.displayName || '';
            $('editBio').value = user.bio || '';
        }

        document.title =
            `${user.displayName || user.userName || 'Profil'} — TravelDiary`;

        try {
            const posts = await getJson(
                `/api/Posts/user/${encodeURIComponent(profileId)}`
            );

            if (!Array.isArray(posts)) {
                throw new Error('API nie zwróciło listy wpisów.');
            }

            posts.sort(
                (a, b) =>
                    new Date(b.createdAt) -
                    new Date(a.createdAt)
            );

            renderPosts(posts);
            renderMap(posts);
        } catch (error) {
            $('postsStatus').textContent =
                'Nie udało się pobrać wpisów: ' + error.message;

            $('mapHint').textContent =
                'Nie udało się pobrać miejsc podróży.';

            console.error(error);
        }
    } catch (error) {
        $('status').textContent =
            'Nie udało się wczytać profilu. Sprawdź, czy jesteś zalogowana.';

        $('postsStatus').textContent =
            'Nie można wczytać wpisów bez profilu.';

        $('mapHint').textContent =
            'Mapa będzie dostępna po wczytaniu profilu.';

        console.error(error);
    }
}

$('editToggle').addEventListener('click', () => {
    $('editSection').hidden = false;

    $('editSection').scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
});

$('closeEdit').addEventListener('click', () => {
    $('editSection').hidden = true;
});

$('editForm').addEventListener('submit', async (event) => {
    event.preventDefault();

    const button = event.currentTarget.querySelector(
        'button[type="submit"]'
    );

    button.disabled = true;
    $('saveStatus').textContent = 'Zapisywanie...';

    try {
        const result = await saveProfile({
            displayName: $('editDisplayName').value.trim(),
            bio: $('editBio').value.trim()
        });

        $('displayName').textContent = result.displayName;

        $('bio').textContent =
            result.bio ||
            'Jeszcze bez opisu — przygody mówią same za siebie!';

        $('avatar').textContent =
            (result.displayName || '🌍')
                .trim()
                .charAt(0)
                .toUpperCase();

        $('saveStatus').textContent =
            'Profil został zapisany! 💚';
    } catch (error) {
        $('saveStatus').textContent = error.message;
        console.error(error);
    } finally {
        button.disabled = false;
    }
});

$('closePostDialog').addEventListener('click', () => {
    $('postDialog').close();
});

initializeMap();
loadProfile();
