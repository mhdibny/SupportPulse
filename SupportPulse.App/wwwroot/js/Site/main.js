/**
 * theme.js
 * Theme toggling, toast notifications, and animated particle background.
 */
(function () {
    'use strict';

    // ============================================================
    // Theme Management
    // ============================================================
    const html = document.documentElement;
    const themeToggle = document.getElementById('themeToggle');
    const themeIcon = themeToggle?.querySelector('i');

    /**
     * Applies the given theme and persists the choice.
     * @param {string} theme - 'dark' or 'light'
     */
    function setTheme(theme) {
        html.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        updateThemeIcon(theme);
    }

    /**
     * Updates the toggle button icon based on the current theme.
     * @param {string} theme - 'dark' or 'light'
     */
    function updateThemeIcon(theme) {
        if (!themeIcon) return;
        themeIcon.className = theme === 'dark' ? 'fas fa-moon' : 'fas fa-sun';
    }

    // Load saved theme, default to dark
    const savedTheme = localStorage.getItem('theme') || 'dark';
    setTheme(savedTheme);

    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const currentTheme = html.getAttribute('data-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }

    // ============================================================
    // Toast Notification System
    // ============================================================
    /**
     * Displays a glass‑morphism toast notification.
     * @param {string} message - The notification text.
     * @param {string} [type='info'] - One of 'success', 'error', 'warning', 'info'.
     * @param {string} [title=''] - Optional title; auto‑filled based on type if omitted.
     * @param {number} [duration=4000] - Display duration in milliseconds.
     */
    window.showToast = function (message, type, title, duration) {
        type = type || 'info';
        duration = duration || 4000;

        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = 'custom-toast ' + type;

        var iconHtml = '';
        switch (type) {
            case 'success':
                iconHtml = '<i class="fas fa-check-circle"></i>';
                title = title || 'موفقیت';
                break;
            case 'error':
                iconHtml = '<i class="fas fa-times-circle"></i>';
                title = title || 'خطا';
                break;
            case 'warning':
                iconHtml = '<i class="fas fa-exclamation-triangle"></i>';
                title = title || 'هشدار';
                break;
            default:
                iconHtml = '<i class="fas fa-info-circle"></i>';
                title = title || 'اطلاعات';
        }

        toast.innerHTML = `
            <div class="toast-icon">${iconHtml}</div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <button class="toast-close"><i class="fas fa-times"></i></button>
        `;

        container.appendChild(toast);

        // Close button
        toast.querySelector('.toast-close').addEventListener('click', function () {
            toast.classList.add('hide');
            setTimeout(function () { toast.remove(); }, 300);
        });

        // Auto dismiss
        setTimeout(function () {
            if (toast.parentNode) {
                toast.classList.add('hide');
                setTimeout(function () { toast.remove(); }, 300);
            }
        }, duration);
    };

    // ============================================================
    // Animated Particle Background (Canvas)
    // ============================================================
    const canvas = document.getElementById('particleCanvas');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let width, height;
        let particles = [];

        /**
         * Initialises canvas dimensions and particle array.
         */
        function initCanvas() {
            width = window.innerWidth;
            height = window.innerHeight;
            canvas.width = width;
            canvas.height = height;

            particles = [];
            const particleCount = Math.floor((width * height) / 12000);
            for (let i = 0; i < particleCount; i++) {
                particles.push({
                    x: Math.random() * width,
                    y: Math.random() * height,
                    radius: Math.random() * 2 + 0.8,
                    vx: (Math.random() - 0.5) * 0.15,
                    vy: (Math.random() - 0.5) * 0.15
                });
            }
        }

        /**
         * Draws particles and connecting lines.
         */
        function drawParticles() {
            ctx.clearRect(0, 0, width, height);
            const theme = html.getAttribute('data-theme');
            const particleColor = theme === 'dark' ? '255, 255, 255' : '15, 23, 42';
            const particleOpacity = theme === 'dark' ? 0.2 : 0.15;
            const lineOpacity = theme === 'dark' ? 0.04 : 0.08;

            // Draw particles
            particles.forEach(function (p) {
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
                ctx.fillStyle = 'rgba(' + particleColor + ', ' + particleOpacity + ')';
                ctx.fill();

                p.x += p.vx;
                p.y += p.vy;

                if (p.x < 0 || p.x > width) p.vx *= -1;
                if (p.y < 0 || p.y > height) p.vy *= -1;
            });

            // Draw connecting lines between nearby particles
            ctx.strokeStyle = 'rgba(' + particleColor + ', ' + lineOpacity + ')';
            ctx.lineWidth = 0.6;
            for (let i = 0; i < particles.length; i++) {
                for (let j = i + 1; j < particles.length; j++) {
                    const dx = particles[i].x - particles[j].x;
                    const dy = particles[i].y - particles[j].y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    if (distance < 120) {
                        ctx.beginPath();
                        ctx.moveTo(particles[i].x, particles[i].y);
                        ctx.lineTo(particles[j].x, particles[j].y);
                        ctx.stroke();
                    }
                }
            }

            requestAnimationFrame(drawParticles);
        }

        window.addEventListener('resize', initCanvas);
        initCanvas();
        drawParticles();
    }

})();