/**
 * identity.js – Authentication via HTTP API (no SignalR hub).
 */
window.accessToken = sessionStorage.getItem('accessToken') || null;

// ========================================================================
// Glass loader overlay
// ========================================================================

/**
 * Displays the full‑screen glass loader.
 */
function showGlassLoader() {
    createGlassLoader();
    document.getElementById('glassLoader')?.classList.add('active');
}

/**
 * Hides the glass loader with a fade‑out transition.
 */
function hideGlassLoader() {
    const ov = document.getElementById('glassLoader');
    if (ov) {
        ov.classList.remove('active');
        setTimeout(() => ov.remove(), 300);
    }
}

/**
 * Creates the loader overlay if it doesn't already exist.
 */
function createGlassLoader() {
    if (document.getElementById('glassLoader')) return;
    const overlay = document.createElement('div');
    overlay.id = 'glassLoader';
    overlay.className = 'register-glass-overlay';
    overlay.innerHTML = `
        <div class="glass-loader-card">
            <div class="glow-circles">
                <div class="glow-circle"></div>
                <div class="glow-circle"></div>
                <div class="glow-circle"></div>
            </div>
            <div class="loader-title">در حال ثبت‌نام شما هستیم!</div>
            <div class="loader-subtitle">لطفاً چند لحظه منتظر بمانید. ما برای امنیت شما از الگوریتم‌های رمزنگاری پیشرفته استفاده می‌کنیم که ممکن است کمی زمان ببرد.</div>
        </div>
    `;
    document.body.appendChild(overlay);
}

// ========================================================================
// Login form
// ========================================================================

/**
 * Attaches the AJAX login handler to the login form.
 */
function setupLoginForm() {
    const loginForm = document.querySelector('form[action*="Login"]');
    if (!loginForm) return;

    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        showGlassLoader();

        const formData = new FormData(loginForm);
        const username = formData.get('UserName')?.trim() || '';
        const password = formData.get('Password') || '';

        try {
            const res = await fetch('/api/identity/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userName: username, password: password })
            });

            if (res.ok) {
                window.location.href = "/";
            } else {
                const contentType = res.headers.get("content-type");
                if (contentType && contentType.includes("application/json")) {
                    const data = await res.json();
                    window.showToast?.(data.message || "خطا در ورود.", "error");
                } else {
                    window.showToast?.("خطا در برقراری ارتباط.", "error");
                }
                hideGlassLoader();
            }
        } catch (err) {
            console.error(err);
            hideGlassLoader();
            window.showToast?.("خطا در برقراری ارتباط.", "error");
        }
    });
}

// ========================================================================
// Registration form
// ========================================================================

/**
 * Attaches the AJAX registration handler to the sign‑up form.
 */
function setupRegisterForm() {
    const registerForm = document.getElementById('registerForm');
    if (!registerForm) return;

    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        showGlassLoader();

        const formData = new FormData(registerForm);

        try {
            const res = await fetch('/api/identity/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    firstName: formData.get('FirstName')?.trim() || '',
                    lastName: formData.get('LastName')?.trim() || '',
                    userName: formData.get('UserName')?.trim() || '',
                    password: formData.get('Password') || '',
                    rePassword: formData.get('RePassword') || ''
                })
            });

            if (res.ok) {
                window.location.href = "/";
            } else {
                const contentType = res.headers.get("content-type");
                if (contentType && contentType.includes("application/json")) {
                    const data = await res.json();
                    window.showToast?.(data.message || "خطا در ثبت‌نام.", "error");
                } else {
                    window.showToast?.("خطا در برقراری ارتباط.", "error");
                }
                hideGlassLoader();
            }
        } catch (err) {
            console.error(err);
            hideGlassLoader();
            window.showToast?.("خطا در برقراری ارتباط.", "error");
        }
    });
}

// ========================================================================
// Initialisation
// ========================================================================
document.addEventListener('DOMContentLoaded', () => {
    // Start the main chat connection if a token already exists
    if (window.accessToken && typeof window.startMainConnection === 'function') {
        window.startMainConnection(window.accessToken);
    }

    setupRegisterForm();
    setupLoginForm();
});