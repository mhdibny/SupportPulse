(function () {
    document.addEventListener('mouseover', function (e) {
        const target = e.target.closest('[data-tooltip]');
        if (!target) return;

        if (target.querySelector('.glass-tooltip')) return;

        const tooltip = document.createElement('div');
        tooltip.className = 'glass-tooltip';
        tooltip.textContent = target.dataset.tooltip;
        target.classList.add('has-glass-tooltip');
        target.appendChild(tooltip);
    });

    document.addEventListener('mouseout', function (e) {
        const target = e.target.closest('[data-tooltip]');
        if (!target) return;

        const tooltip = target.querySelector('.glass-tooltip');
        if (tooltip) {
            tooltip.remove();
            target.classList.remove('has-glass-tooltip');
        }
    });
})();