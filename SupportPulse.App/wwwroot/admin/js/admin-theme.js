(function () {
    const html = document.documentElement;
    const btn = document.getElementById('themeToggle');
    const icon = btn?.querySelector('i');
    const themes = ['dark', 'light'];
    const saved = localStorage.getItem('admin-theme') || 'dark';
    function setTheme(t) {
        html.setAttribute('data-theme', t);
        localStorage.setItem('admin-theme', t);
        if (icon) icon.className = t === 'dark' ? 'fas fa-moon' : 'fas fa-sun';
    }
    setTheme(saved);
    btn?.addEventListener('click', () => {
        const next = html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        setTheme(next);
    });
})();