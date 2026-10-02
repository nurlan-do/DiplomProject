async function detectAndSaveUserCity() {
    if (!navigator.geolocation) {
        console.log('Геолокация не поддерживается вашим браузером');
        return;
    }

    navigator.geolocation.getCurrentPosition(async (position) => {
        const lat = position.coords.latitude;
        const lon = position.coords.longitude;

        try {
            // Запрос к бесплатному API для получения названия города по координатам
            const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}&accept-language=ru`);
            const data = await response.json();

            // Извлекаем название города (в зависимости от региона это может быть city, town или village)
            const cityName = data.address.city || data.address.town || data.address.village || 'Неизвестно';

            // Автоматически отправляем на бэкенд, который вы только что настроили
            await saveCityToBackend(cityName);

        } catch (error) {
            console.error('Не удалось определить город по координатам:', error);
        }
    }, (error) => {
        console.warn('Пользователь запретил доступ к геолокации или произошла ошибка:', error.message);
    });
}


document.addEventListener('DOMContentLoaded', async () => {
    // showPage('homePage');
    // ==========================================
    // 1. ПЕРЕКЛЮЧЕНИЕ СТРАНИЦ (Главная, Поиск, Профиль)
    // ==========================================

    // Проверяем пользователя
    try {
        const res = await fetch('/api/auth/user');
        const data = await res.json();

        if (data.isAuthenticated) {
            // если пользователь вошел, обновляем имя/почту в профиле
            document.getElementById('userName').textContent = data.name;
        }
    } catch (e) {
        console.error('Ошибка проверки авторизации', e);
    }

    // показываем главную страницу
    const homePage = document.getElementById('homePage');
    if (homePage) {
        homePage.style.display = 'block';
    }

    const navButtons = document.querySelectorAll('.nav-link');
    const views = document.querySelectorAll('.view');

    navButtons.forEach(button => {
        button.addEventListener('click', () => {
            const targetViewId = button.getAttribute('data-view');

            navButtons.forEach(btn => btn.classList.remove('active'));
            views.forEach(view => view.classList.add('hidden'));

            button.classList.add('active');
            const targetView = document.getElementById(targetViewId);
            if (targetView) {
                targetView.classList.remove('hidden');
            }
        });
    });

    // ==========================================
    // 2. ПЕРЕКЛЮЧЕНИЕ ТЕМ (День / Ночь)
    // ==========================================
    const themeToggleBtn = document.getElementById('themeToggle');
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            document.body.classList.toggle('light-theme');
            if (document.body.classList.contains('light-theme')) {
                themeToggleBtn.textContent = '☀️';
            } else {
                themeToggleBtn.textContent = '🌙';
            }
        });
    }

    // ==========================================
    // 3. GOOGLE IDENTITY SERVICES (Бесшовный вход)
    // ==========================================
    if (window.google) {
        google.accounts.id.initialize({
            client_id: "886486873482-3bgiib67v060b4n5nqf4smnopg91h63v.apps.googleusercontent.com",
            callback: handleCredentialResponse
        });

        const googleAuthBtn = document.getElementById('googleAuthBtn');
        if (googleAuthBtn) {
            google.accounts.id.renderButton(
                googleAuthBtn,
                { theme: "outline", size: "large", text: "signin_with" }
            );
        }
    }

    // Проверяем статус авторизации при старте страницы
    checkAuthStatus();

    // ==========================================
    // 4. ЗАГРУЗКА ПРОФЕССИЙ И ПОИСК
    // ==========================================
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearBtn');
    const loader = document.getElementById('loader');
    const errorMessage = document.getElementById('errorMessage');
    const resultsList = document.getElementById('resultsList');

    let allProfessions = [];

    async function loadProfessions() {
        try {
            if (loader) loader.classList.remove('hidden');
            if (errorMessage) errorMessage.classList.add('hidden');

            const response = await fetch('/api/professions');
            if (!response.ok) throw new Error('Ошибка сервера при получении данных');

            allProfessions = await response.json();
            renderProfessions(allProfessions);
        } catch (error) {
            console.error('Не удалось загрузить профессии:', error);
            if (errorMessage) {
                errorMessage.textContent = 'Не удалось загрузить список профессий. Проверьте, запущен ли бэкенд.';
                errorMessage.classList.remove('hidden');
            }
        } finally {
            if (loader) loader.classList.add('hidden');
        }
    }

    function renderProfessions(professions) {
        if (!resultsList) return;

        if (professions.length === 0) {
            resultsList.innerHTML = '<p>Профессии не найдены.</p>';
            return;
        }

        resultsList.innerHTML = professions.map(p => `
        <div class="profession-card" style="border: 1px solid #e0e0e0; padding: 15px; margin-bottom: 12px; border-radius: 8px;">
            <h3>${p.title}</h3>
            <p>${p.description}</p>
            <span class="category-badge">${p.category}</span>
            <!-- Кнопка добавления в профиль -->
            <button onclick="saveProfession(${p.id})" style="background: #0284c7; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer; margin-left: 10px;">
                Добавить в профиль
            </button>
        </div>
    `).join('');
    }

    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase().trim();
            if (query.length > 0) {
                if (clearBtn) clearBtn.classList.remove('hidden');
            } else {
                if (clearBtn) clearBtn.classList.add('hidden');
            }

            const filtered = allProfessions.filter(p =>
                (p.title && p.title.toLowerCase().includes(query)) ||
                (p.name && p.name.toLowerCase().includes(query)) ||
                (p.description && p.description.toLowerCase().includes(query)) ||
                (p.category && p.category.toLowerCase().includes(query))
            );
            renderProfessions(filtered);
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            searchInput.value = '';
            clearBtn.classList.add('hidden');
            renderProfessions(allProfessions);
            searchInput.focus();
        });
    }

    loadProfessions();

    startQuiz();

});

// ==========================================
// ВСПОМОГАТЕЛЬНЫЕ ГЛОБАЛЬНЫЕ ФУНКЦИИ АВТОРИЗАЦИИ
// ==========================================

// Обработка успешного входа через всплывающее окно Google
async function handleCredentialResponse(response) {
    try {
        const res = await fetch('/api/auth/google-token', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token: response.credential })
        });

        if (res.ok) {
            const data = await res.json();
            console.log('Успешный вход!', data);
            checkAuthStatus(); // Обновляем шапку
        } else {
            console.error('Ошибка авторизации на бэкенде');
        }
    } catch (error) {
        console.error('Ошибка сети:', error);
    }
}

// Проверка текущего пользователя и обновление интерфейса шапки
async function checkAuthStatus() {
    try {
        const response = await fetch('/api/auth/user');
        const data = await response.json();

        const googleAuthBtn = document.getElementById('googleAuthBtn');
        const userInfo = document.getElementById('userInfo');
        const userName = document.getElementById('userName');

        const profileLoading = document.getElementById('profileLoading');
        const profileData = document.getElementById('profileData');
        const profileGuest = document.getElementById('profileGuest');
        const profileName = document.getElementById('profileName');
        const profileEmail = document.getElementById('profileEmail');

        if (data.isAuthenticated) {
            // Если вошел — скрываем кнопку входа, показываем имя
            if (googleAuthBtn) googleAuthBtn.style.display = 'none';
            if (userInfo) userInfo.classList.remove('hidden');
            if (userName) userName.textContent = data.name;
            if (profileLoading) profileLoading.classList.add('hidden');
            if (profileData) profileData.classList.remove('hidden');
            if (profileGuest) profileGuest.classList.add('hidden');

            if (profileName) profileName.textContent = data.name;
            if (profileEmail) profileEmail.textContent = data.email || 'Не указан';

            const profileCity = document.getElementById('profileCity');

            if (profileCity) {
                profileCity.textContent = data.city || 'Не указан';
            }

            const saveCityBtn = document.getElementById('saveCityBtn');

            if (saveCityBtn) {
                saveCityBtn.addEventListener('click', async () => {
                    const cityInput = document.getElementById('cityInput');
                    const city = cityInput.value.trim();

                    if (!city) {
                        alert('Введите город!');
                        return;
                    }

                    try {
                        // ---> ВСТАВЛЯТЬ СЮДА <---
                        const token = localStorage.getItem('token'); // или откуда вы берете токен

                        const res = await fetch('/api/user/city', {
                            method: 'POST',
                            credentials: 'include', // Используем куки вместо Bearer токена
                            headers: {
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({ city })
                        });

                        if (res.ok) {
                            const data = await res.json();
                            document.getElementById('profileCity').textContent = data.city;
                            alert('Город успешно сохранен!');
                            cityInput.value = '';
                        } else {
                            alert('Не удалось сохранить город.');
                        }
                    } catch (e) {
                        console.error('Ошибка сохранения города', e);
                    }
                });
            }

            console.log('Пользователь вошел в систему как:', data.name);
        } else {
            // Если не вошел — показываем кнопку Google, скрываем блок пользователя
            if (googleAuthBtn) googleAuthBtn.style.display = 'block';
            if (userInfo) userInfo.classList.add('hidden');
            if (profileLoading) profileLoading.classList.add('hidden');
            if (profileData) profileData.classList.add('hidden');
            if (profileGuest) profileGuest.classList.remove('hidden');
        }
    } catch (e) {
        console.error('Ошибка проверки статуса', e);
    }
}

async function saveProfession(professionId) {
    try {
        const response = await fetch('/api/user/professions', {
            method: 'POST',
            credentials: 'include', // Обязательно для передачи куки сессии!
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ professionId: professionId })
        });

        if (response.ok) {
            alert('Профессия успешно сохранена!');
            // Сразу обновляем список профессий на экране профиля
            loadSavedProfessions();
        } else if (response.status === 401) {
            alert('Пожалуйста, войдите в систему через Google.');
        } else {
            alert('Не удалось сохранить профессию.');
        }
    } catch (error) {
        console.error('Ошибка при сохранении профессии:', error);
    }
}

// Загрузка сохраненных профессий в личный кабинет / профиль
async function loadUserProfileProfessions() {
    const container = document.getElementById('savedProfessionsList');
    if (!container) return;

    try {
        const response = await fetch('/api/user/professions');
        if (response.ok) {
            const professions = await response.json();

            if (professions.length === 0) {
                container.innerHTML = '<p>У вас пока нет сохраненных профессий.</p>';
                return;
            }

            container.innerHTML = professions.map(p => `
                <div class="saved-card" style="border: 1px solid #ccc; padding: 10px; margin-bottom: 10px; border-radius: 6px;">
                    <h4>${p.title}</h4>
                    <p>${p.description}</p>
                    <span style="font-size: 12px; color: gray;">Категория: ${p.category}</span>
                </div>
            `).join('');
        } else {
            container.innerHTML = '<p>Войдите в аккаунт, чтобы увидеть сохраненные профессии.</p>';
        }
    } catch (error) {
        console.error('Не удалось загрузить профессии', error);
        container.innerHTML = '<p>Ошибка загрузки.</p>';
    }
}

const logoutBtn = document.getElementById('logoutBtn');
if (logoutBtn) {
    logoutBtn.addEventListener('click', async () => {
        try {
            const response = await fetch('/api/auth/logout', {
                method: 'POST'
            });

            if (response.ok) {
                console.log('Успешный выход из системы');
                // Сбрасываем UI: скрываем профиль/имя, показываем кнопку входа Google
                const googleAuthBtn = document.getElementById('googleAuthBtn');
                const userInfo = document.getElementById('userInfo');

                if (googleAuthBtn) googleAuthBtn.style.display = 'block';
                if (userInfo) userInfo.classList.add('hidden');

                // Опционально можно перезагрузить страницу или перекинуть на главную
                switchView('homeView');
            } else {
                console.error('Ошибка при выходе');
            }
        } catch (error) {
            console.error('Ошибка сети при попытке выйти:', error);
        }
    });
}

async function loadSavedProfessions() {
    const container = document.getElementById('savedProfessionsContainer');
    if (!container) return;

    try {
        const response = await fetch('/api/user/professions', {
            method: 'GET',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const professions = await response.json();

            // Если список пустой
            if (professions.length === 0) {
                container.innerHTML = '<p>У вас пока нет сохраненных профессий.</p>';
                return;
            }

            // Генерируем HTML для каждой профессии
            container.innerHTML = professions.map(p => `
                <div class="profession-card-item" style="border: 1px solid #ddd; padding: 12px; margin-bottom: 10px; border-radius: 6px; background: #fff;">
                    <h4 style="margin: 0 0 5px 0; color: #333;">${p.title}</h4>
                    <p style="margin: 0 0 8px 0; color: #666; font-size: 14px;">${p.description}</p>
                    <span style="font-size: 12px; background: #e0f2fe; color: #0369a1; padding: 2px 6px; border-radius: 4px;">${p.category}</span>
                </div>
            `).join('');

            container.innerHTML = professions.map(p => `
    <div class="saved-card" style="border: 1px solid #ccc; padding: 12px; margin-bottom: 10px; border-radius: 6px; background: #fff;">
        <h4 style="margin: 0 0 5px 0; color: #333;">${p.title}</h4>
        <p style="margin: 0 0 8px 0; color: #666; font-size: 14px;">${p.description}</p>
        <span style="font-size: 12px; background: #e8f2fe; color: #0369a1; padding: 2px 6px; border-radius: 4px;">${p.category}</span>
        <button onclick="removeProfession(${p.id})" style="float: right; background: #ff4d4d; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer;">Удалить</button>
    </div>
`).join('');

        } else if (response.status === 401) {
            container.innerHTML = '<p>Пожалуйста, войдите в систему, чтобы увидеть сохраненные профессии.</p>';
        } else {
            container.innerHTML = '<p>Не удалось загрузить профессии.</p>';
        }
    } catch (error) {
        console.error('Ошибка при загрузке сохраненных профессий:', error);
        container.innerHTML = '<p>Ошибка соединения с сервером.</p>';
    }
}

async function saveCityToBackend(cityName) {
    try {
        const response = await fetch('/api/user/city', {
            method: 'POST',
            credentials: 'include', // Обязательно для передачи кук авторизации
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ city: cityName })
        });

        if (response.ok) {
            const result = await response.json();
            console.log('Город успешно сохранен:', result.city);

            // Если на странице есть элемент для отображения города — обновляем его
            const citySpan = document.getElementById('userCity');
            if (citySpan) citySpan.textContent = result.city;
        }
    } catch (error) {
        console.error('Ошибка при сохранении города на бэкенд:', error);
    }
}

async function removeProfession(professionId) {
    try {
        const response = await fetch(`/api/user/professions/${professionId}`, {
            method: 'DELETE',
            credentials: 'include', // Обязательно для передачи куки сессии
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('Профессия удалена из профиля.');
            loadSavedProfessions(); // Перезагружаем список в профиле
        } else if (response.status === 401) {
            alert('Пожалуйста, войдите в систему.');
        } else {
            alert('Не удалось удалить профессию.');
        }
    } catch (error) {
        console.error('Ошибка при удалении профессии:', error);
    }
}

const quizData = [
    {
        question: "Какое направление вам ближе всего?",
        options: [
            { text: "Разработка и технологии", category: "IT" },
            { text: "Дизайн и создание контента", category: "Творчество" },
            { text: "Работа с цифрами и анализ", category: "Аналитика" }
        ]
    }
    // Можно добавить второй и третий вопрос по аналогии
];

let currentQuestionIndex = 0;
let userSelections = [];

function startQuiz() {
    currentQuestionIndex = 0;
    userSelections = [];
    showQuestion();
}

function showQuestion() {
    const q = quizData[currentQuestionIndex];
    document.getElementById('question-title').textContent = q.question;

    const container = document.getElementById('options-container');
    container.innerHTML = q.options.map((opt, index) => `
        <button onclick="selectOption('${opt.category}')" style="padding: 10px; text-align: left; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 6px; cursor: pointer;">
            ${opt.text}
        </button>
    `).join('');
}

function selectOption(category) {
    userSelections.push(category);
    currentQuestionIndex++;

    if (currentQuestionIndex < quizData.length) {
        showQuestion();
    } else {
        submitQuizResults();
    }
}

async function submitQuizResults() {
    document.getElementById('question-block').classList.add('hidden');
    document.getElementById('result-block').classList.remove('hidden');

    try {
        const response = await fetch('/api/recommendations/calculate', {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ SelectedCategories: userSelections })
        });

        if (!response.ok) throw new Error('Ошибка расчета рекомендаций');

        const professions = await response.json();
        const resultsContainer = document.getElementById('recommended-results');

        if (professions.length === 0) {
            resultsContainer.innerHTML = '<p>Не удалось подобрать профессии под ваши ответы.</p>';
            return;
        }

        // Рендерим карточки с уже знакомой нам кнопкой «Добавить в профиль»!
        resultsContainer.innerHTML = professions.map(p => `
            <div style="border: 1px solid #e0e0e0; padding: 12px; margin-bottom: 10px; border-radius: 6px;">
                <h4>${p.title}</h4>
                <p>${p.description}</p>
                <button onclick="saveProfession(${p.id})" style="background: #0284c7; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer; margin-top: 8px;">
                    Добавить в профиль
                </button>
            </div>
        `).join('');

    } catch (error) {
        console.error('Ошибка:', error);
    }
}

document.getElementById('homePage').style.display = 'block';

document.getElementById('homePage').style.display = 'none';

document.getElementById('detectCityBtn')?.addEventListener('click', detectAndSaveUserCity);
