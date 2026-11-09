(() => {
    'use strict';
    const announce = element => {
        const urgent = element.classList.contains('alert-danger') || element.classList.contains('alert-warning');
        element.setAttribute('role', urgent ? 'alert' : 'status');
        element.setAttribute('aria-live', urgent ? 'assertive' : 'polite');
        element.setAttribute('aria-atomic', 'true');
    };
    document.querySelectorAll('.dashboard-body-koc .alert').forEach(announce);
    document.querySelectorAll('.dashboard-body-koc [class*="empty"]').forEach(element => {
        if (!element.hasAttribute('role')) element.setAttribute('role', 'status');
    });
    document.querySelectorAll('.dashboard-body-koc .btn-close:not([aria-label])')
        .forEach(button => button.setAttribute('aria-label', 'Đóng'));
    const observer = new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => {
        if (!(node instanceof HTMLElement)) return;
        if (node.matches('.alert')) announce(node);
        node.querySelectorAll?.('.alert').forEach(announce);
    })));
    observer.observe(document.body, { childList: true, subtree: true });
})();
