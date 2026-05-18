// ===== WEATHER BAR — VnExpress style =====
(function () {
    const DEFAULT_CITY = 'Hà Nội';
    let allCities = [];
    let selectedCity = localStorage.getItem('selectedCity') || DEFAULT_CITY;

    const ICON_MAP = {
        '☀️': 'sunny',
        '⛅': 'partly_cloudy_day',
        '☁️': 'cloud',
        '🌫️': 'foggy',
        '🌦️': 'rainy',
        '🌧️': 'rainy',
        '❄️': 'weather_snowy',
        '⛈️': 'thunderstorm',
        '🌡️': 'thermometer',
    };

    // ── Init ──────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', async () => {
        // Move dropdown & overlay to <body> so they escape any overflow:hidden ancestor
        const dropdown = document.getElementById('weatherDropdown');
        const overlay  = document.getElementById('weatherOverlay');
        if (dropdown && dropdown.parentElement !== document.body) {
            document.body.appendChild(dropdown);
        }
        if (overlay && overlay.parentElement !== document.body) {
            document.body.appendChild(overlay);
        }

        await loadCities();

        // Nếu saved city khác Hà Nội (đã SSR), cập nhật ngay
        if (selectedCity !== DEFAULT_CITY) {
            await loadWeather(selectedCity);
        }

        renderCityList(allCities);
    });

    // ── API calls ─────────────────────────────────────────────
    async function loadCities() {
        try {
            const res = await fetch('/api/weather/cities');
            allCities = await res.json();
        } catch {
            allCities = [];
        }
    }

    async function loadWeather(city) {
        try {
            const res = await fetch('/api/weather?city=' + encodeURIComponent(city));
            if (!res.ok) return;
            const data = await res.json();

            const cityEl  = document.getElementById('weatherCityName');
            const tempEl  = document.getElementById('weatherTemp');
            const iconEl  = document.getElementById('weatherIcon');

            if (cityEl)  cityEl.textContent  = data.city;
            if (tempEl)  tempEl.textContent  = data.temperature + '°';
            if (iconEl)  iconEl.textContent  = ICON_MAP[data.icon] || 'thermometer';

            selectedCity = data.city;
            localStorage.setItem('selectedCity', selectedCity);
        } catch (e) {
            console.error('[Weather] load error:', e);
        }
    }

    // ── Dropdown ──────────────────────────────────────────────
    window.toggleWeatherDropdown = function () {
        const dropdown = document.getElementById('weatherDropdown');
        const overlay  = document.getElementById('weatherOverlay');
        const chevron  = document.getElementById('weatherChevron');
        const trigger  = document.getElementById('weatherTrigger');
        const isOpen   = dropdown && dropdown.style.display !== 'none';

        if (isOpen) {
            closeWeatherDropdown();
        } else {
            // Position fixed dropdown below the trigger
            if (trigger && dropdown) {
                const rect    = trigger.getBoundingClientRect();
                const dropW   = 320;
                const viewW   = window.innerWidth;
                let   left    = rect.left;

                // Clamp to viewport right edge
                if (left + dropW > viewW - 8) left = viewW - dropW - 8;
                if (left < 8) left = 8;

                dropdown.style.top  = (rect.bottom + 6) + 'px';
                dropdown.style.left = left + 'px';
            }

            if (dropdown)  dropdown.style.display = 'block';
            if (overlay)   overlay.style.display  = 'block';
            if (chevron)   chevron.classList.add('open');

            const searchInput = document.getElementById('citySearchInput');
            if (searchInput) {
                searchInput.value = '';
                setTimeout(() => searchInput.focus(), 80);
            }
            renderCityList(allCities);
        }
    };

    window.closeWeatherDropdown = function () {
        const dropdown = document.getElementById('weatherDropdown');
        const overlay  = document.getElementById('weatherOverlay');
        const chevron  = document.getElementById('weatherChevron');

        if (dropdown) dropdown.style.display = 'none';
        if (overlay)  overlay.style.display  = 'none';
        if (chevron)  chevron.classList.remove('open');
    };

    window.filterCities = function (query) {
        const q        = query.trim().toLowerCase();
        const filtered = q === ''
            ? allCities
            : allCities.filter(c => c.name.toLowerCase().includes(q));
        renderCityList(filtered);
    };

    function renderCityList(cities) {
        const list = document.getElementById('cityList');
        if (!list) return;
        list.innerHTML = '';

        cities.forEach(city => {
            const li = document.createElement('li');
            li.className = city.name === selectedCity ? 'active' : '';

            const nameSpan = document.createElement('span');
            nameSpan.textContent = city.name;
            li.appendChild(nameSpan);

            if (city.name === DEFAULT_CITY) {
                const tag = document.createElement('span');
                tag.className = 'vne-default-tag';
                tag.innerHTML = 'Mặc định <span class="material-symbols-outlined">my_location</span>';
                li.appendChild(tag);
            } else if (city.name === selectedCity) {
                const chk = document.createElement('span');
                chk.className = 'material-symbols-outlined';
                chk.textContent = 'check';
                li.appendChild(chk);
            }

            li.onclick = async () => {
                await loadWeather(city.name);
                renderCityList(allCities);
                closeWeatherDropdown();
            };
            list.appendChild(li);
        });
    }

    // ── Geolocation ───────────────────────────────────────────
    window.toggleAutoLocate = function (checkbox) {
        if (!checkbox.checked) return;
        if (!navigator.geolocation) { checkbox.checked = false; return; }

        navigator.geolocation.getCurrentPosition(async (pos) => {
            const { latitude, longitude } = pos.coords;
            let nearest = allCities[0] || { name: DEFAULT_CITY };
            let minDist = Infinity;

            allCities.forEach(c => {
                const d = Math.pow(c.lat - latitude, 2) + Math.pow(c.lon - longitude, 2);
                if (d < minDist) { minDist = d; nearest = c; }
            });

            await loadWeather(nearest.name);
            renderCityList(allCities);
            closeWeatherDropdown();
            checkbox.checked = false;
        }, () => { checkbox.checked = false; });
    };
})();
