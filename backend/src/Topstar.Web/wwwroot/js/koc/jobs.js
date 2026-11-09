(() => {
    const element = document.getElementById('withdrawConfirmModal');
    if (!element) return;
    const modal = bootstrap.Modal.getOrCreateInstance(element);
    const confirm = element.querySelector('[data-withdraw-confirm]');
    let pendingForm = null;
    document.querySelectorAll('[data-withdraw-form]').forEach(form => {
        form.addEventListener('submit', event => {
            if (form.dataset.confirmed === 'true') return;
            event.preventDefault();
            pendingForm = form;
            element.querySelector('[data-withdraw-title]').textContent = form.dataset.campaignTitle;
            confirm.disabled = false;
            confirm.textContent = 'Xác nhận rút';
            modal.show();
        });
    });
    confirm.addEventListener('click', () => {
        if (!pendingForm || confirm.disabled) return;
        confirm.disabled = true;
        confirm.textContent = 'Đang xử lý…';
        pendingForm.dataset.confirmed = 'true';
        pendingForm.requestSubmit();
    });
    element.addEventListener('hidden.bs.modal', () => { pendingForm = null; });
})();
