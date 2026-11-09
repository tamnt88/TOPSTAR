(() => {
    'use strict';
    const bindModal = (buttonSelector, modalId, applicationId, codeId, reasonId, formSelector) => {
        const modalElement = document.getElementById(modalId);
        if (!modalElement) return;
        document.querySelectorAll(buttonSelector).forEach(button => button.addEventListener('click', () => {
            document.getElementById(applicationId).value = button.dataset.applicationId;
            document.getElementById(codeId).textContent = button.dataset.code;
            const reason = document.getElementById(reasonId);
            reason.value = '';
            reason.classList.remove('is-invalid');
            bootstrap.Modal.getOrCreateInstance(modalElement).show();
        }));
        document.querySelector(formSelector)?.addEventListener('submit', event => {
            const reason = document.getElementById(reasonId);
            const length = reason.value.trim().length;
            const valid = length >= 10 && length <= 500;
            reason.classList.toggle('is-invalid', !valid);
            if (!valid) event.preventDefault();
        });
    };
    bindModal('.js-remove-koc', 'removeKocModal', 'removeKocApplicationId', 'removeKocName', 'removalReason', '.js-remove-koc-form');
    bindModal('.js-reject-proposed-koc', 'rejectProposedKocModal', 'rejectProposedKocApplicationId', 'rejectProposedKocName', 'rejectionReason', '.js-reject-proposed-koc-form');
})();
