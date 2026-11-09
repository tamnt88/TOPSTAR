(() => {
    const modalElement = document.getElementById('bookingActionModal');
    const form = modalElement?.querySelector('[data-booking-action-form]');
    if (!modalElement || !form || !window.bootstrap) return;
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement, { backdrop: 'static', keyboard: false });
    const error = form.querySelector('[data-booking-error]');
    const rescheduleFields = form.querySelector('[data-reschedule-fields]');
    const cancelFields = form.querySelector('[data-cancel-fields]');
    const clearError = () => {
        error.textContent = '';
        error.classList.remove('is-visible');
        form.querySelectorAll('.is-invalid').forEach(x => x.classList.remove('is-invalid'));
    };
    const showError = (message, elements) => {
        error.textContent = message;
        error.classList.add('is-visible');
        elements.forEach(x => x?.classList.add('is-invalid'));
    };
    const open = (cancelling, button) => {
        form.reset();
        clearError();
        form.querySelector('[name="id"]').value = button.dataset.id || '';
        form.querySelector('[data-booking-modal-context]').textContent = button.dataset.title || '';
        form.action = `${location.pathname}?handler=${cancelling ? 'Cancel' : 'Reschedule'}`;
        rescheduleFields.hidden = cancelling;
        cancelFields.hidden = !cancelling;
        form.querySelector('[data-booking-modal-title]').textContent = cancelling ? 'Hủy lịch booking' : 'Đề xuất thời gian khác';
        form.querySelector('[data-booking-submit-label]').textContent = cancelling ? 'Xác nhận hủy' : 'Gửi đề xuất';
        form.querySelector('button[type="submit"]').className = `btn btn-sm ${cancelling ? 'btn-danger' : 'btn-primary'}`;
        modal.show();
    };
    document.addEventListener('click', event => {
        const reschedule = event.target.closest('[data-booking-reschedule]');
        const cancel = event.target.closest('[data-booking-cancel]');
        if (reschedule) open(false, reschedule);
        if (cancel) open(true, cancel);
        const focus = event.target.closest('[data-booking-focus]');
        if (focus) {
            const item = document.getElementById(`booking-${focus.dataset.bookingFocus}`);
            item?.scrollIntoView({ behavior: 'smooth', block: 'center' });
            item?.classList.add('is-highlighted');
            setTimeout(() => item?.classList.remove('is-highlighted'), 1800);
        }
    });
    form.addEventListener('submit', event => {
        clearError();
        if (!cancelFields.hidden) {
            const reason = form.querySelector('[name="reason"]');
            if (reason.value.trim().length < 10) {
                event.preventDefault();
                showError('Vui lòng nhập lý do hủy ít nhất 10 ký tự.', [reason]);
            }
            return;
        }
        const start = form.querySelector('[name="startsAtLocal"]');
        const end = form.querySelector('[name="endsAtLocal"]');
        const note = form.querySelector('[name="note"]');
        if (!start.value || !end.value) {
            event.preventDefault();
            showError('Vui lòng chọn đầy đủ thời gian bắt đầu và kết thúc.', [start, end]);
        } else if (new Date(end.value) <= new Date(start.value)) {
            event.preventDefault();
            showError('Thời gian kết thúc phải sau thời gian bắt đầu.', [start, end]);
        } else if (note.value.trim().length < 10) {
            event.preventDefault();
            showError('Vui lòng nhập lý do đề xuất ít nhất 10 ký tự.', [note]);
        }
    });
})();
