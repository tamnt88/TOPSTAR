(() => {
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[character]);
    const formatDate = value => window.TopstarFormat.dateTime(value);
    const stars = rating => `<span class="koc-review-stars" aria-label="${Number(rating || 0)} trên 5 sao">${
        [1, 2, 3, 4, 5].map(value => `<i class="${value <= Number(rating || 0) ? 'fa-solid' : 'fa-regular'} fa-star"></i>`).join('')
    }</span>`;
    const token = document.querySelector('[data-review-token] input[name="__RequestVerificationToken"]')?.value || '';
    const modalElement = document.getElementById('reviewDetailModal');
    const modal = modalElement ? bootstrap.Modal.getOrCreateInstance(modalElement, { backdrop: 'static', keyboard: false }) : null;
    const detail = modalElement?.querySelector('[data-review-detail]');
    const context = modalElement?.querySelector('[data-review-context]');
    const rowCache = new Map();
    const tables = {};

    const statusBadge = (kind, row) => {
        if (kind === 'sent') {
            return row.isReviewed
                ? '<span class="koc-review-state is-success"><i class="fa-solid fa-circle-check"></i>Đã đánh giá</span>'
                : '<span class="koc-review-state is-warning"><i class="fa-regular fa-clock"></i>Chờ đánh giá</span>';
        }
        if (row.appealStatus === 'Open') return '<span class="koc-review-state is-warning"><i class="fa-solid fa-scale-balanced"></i>Đang xem xét</span>';
        if (row.appealStatus) return '<span class="koc-review-state is-muted"><i class="fa-solid fa-shield-halved"></i>Đã xử lý</span>';
        if (row.replyBody) return '<span class="koc-review-state is-success"><i class="fa-solid fa-reply"></i>Đã phản hồi</span>';
        return '<span class="koc-review-state is-info"><i class="fa-solid fa-comment"></i>Chưa phản hồi</span>';
    };

    const field = (name, placeholder, min, max, rows = 3) => `<div class="koc-review-modal-field">
        <textarea name="${name}" class="form-control" rows="${rows}" minlength="${min}" maxlength="${max}" placeholder="${escapeHtml(placeholder)}"></textarea>
        <p class="koc-field-error" data-field-error></p>
    </div>`;
    const form = (handler, id, body, button, buttonClass = 'btn-primary') => `<form method="post" action="?handler=${handler}" data-review-action novalidate>
        <input type="hidden" name="__RequestVerificationToken" value="${escapeHtml(token)}">
        <input type="hidden" name="id" value="${escapeHtml(id)}">${body}
        <button class="btn ${buttonClass} btn-sm" type="submit">${button}</button>
    </form>`;

    const receivedDetail = row => {
        const appealLabel = ({ Open: 'Đang xem xét', Accepted: 'Đã chấp nhận', Rejected: 'Đã từ chối', Withdrawn: 'Đã rút' })[row.appealStatus] || '';
        let interaction = '';
        if (row.replyBody) {
            interaction += `<section class="koc-review-modal-block is-reply"><h3><i class="fa-solid fa-reply"></i> Phản hồi của bạn</h3><p>${escapeHtml(row.replyBody)}</p><small>${formatDate(row.repliedAtUtc)}</small></section>`;
        } else {
            interaction += `<section class="koc-review-modal-action"><h3><i class="fa-solid fa-reply"></i> Phản hồi đánh giá</h3>${
                form('Reply', row.id, field('body', 'Nhập phản hồi từ 2 đến 2.000 ký tự', 2, 2000), '<i class="fa-solid fa-paper-plane me-1"></i>Gửi phản hồi')
            }</section>`;
        }
        if (row.appealStatus) {
            interaction += `<section class="koc-review-modal-block is-appeal"><div><h3><i class="fa-solid fa-scale-balanced"></i> Yêu cầu xem xét</h3><span>${appealLabel}</span></div><p>${escapeHtml(row.appealReason)}</p>${row.appealDecisionNote ? `<small><strong>Kết luận:</strong> ${escapeHtml(row.appealDecisionNote)}</small>` : ''}</section>`;
            if (row.appealStatus === 'Open') {
                interaction += `<div class="koc-review-modal-actions-grid">${
                    form('SupplementAppeal', row.id, field('body', 'Thông tin bổ sung từ 10 đến 2.000 ký tự', 10, 2000, 2), '<i class="fa-solid fa-plus me-1"></i>Gửi bổ sung', 'btn-outline-primary')
                }${form('WithdrawAppeal', row.id, field('reason', 'Lý do rút từ 10 đến 500 ký tự', 10, 500, 2), '<i class="fa-solid fa-ban me-1"></i>Rút yêu cầu', 'btn-outline-danger')}</div>`;
            }
        } else if (row.status !== 'Hidden') {
            interaction += `<section class="koc-review-modal-action"><h3><i class="fa-solid fa-shield-halved"></i> Yêu cầu Admin xem xét</h3>${
                form('Appeal', row.id, field('reason', 'Nêu rõ nội dung chưa chính xác và căn cứ (10–2.000 ký tự)', 10, 2000), '<i class="fa-solid fa-shield-halved me-1"></i>Gửi đến Admin', 'btn-outline-danger')
            }</section>`;
        }
        return `<div class="koc-review-modal-summary"><div>${stars(row.rating)}<strong>${escapeHtml(row.partnerName)}</strong><span>${escapeHtml(row.campaignTitle)}</span></div><time><i class="fa-regular fa-calendar"></i>${formatDate(row.createdAtUtc)}</time></div>
            <blockquote class="koc-review-modal-quote"><i class="fa-solid fa-quote-left"></i>${escapeHtml(row.comment || 'Brand không để lại nhận xét.')}</blockquote>${interaction}`;
    };

    const ratingOptions = (prefix, selected) => `<div class="koc-review-rating-input">${
        [1, 2, 3, 4, 5].map(value => `<label><input type="radio" name="rating" value="${value}" ${Number(selected) === value ? 'checked' : ''}><span>${value}<i class="fa-solid fa-star"></i></span></label>`).join('')
    }</div>`;
    const sentDetail = row => {
        if (!row.isReviewed) {
            return `<section class="koc-review-modal-action is-primary"><h3><i class="fa-solid fa-star"></i> Đánh giá Brand</h3>${
                form('Submit', row.participationId, `${ratingOptions(`submit-${row.participationId}`, 0)}${field('comment', 'Điểm tốt và điều Brand có thể cải thiện (không bắt buộc)', 0, 2000)}`, '<i class="fa-solid fa-paper-plane me-1"></i>Gửi đánh giá')
            }</section>`;
        }
        return `<div class="koc-review-modal-summary"><div>${stars(row.rating)}<strong>${escapeHtml(row.partnerName)}</strong><span>${escapeHtml(row.campaignTitle)}</span></div><time><i class="fa-regular fa-calendar-check"></i>${formatDate(row.reviewedAtUtc)}</time></div>
            <blockquote class="koc-review-modal-quote"><i class="fa-solid fa-quote-left"></i>${escapeHtml(row.comment || 'Bạn không để lại nhận xét.')}</blockquote>
            ${row.canEdit ? `<section class="koc-review-modal-action"><h3><i class="fa-solid fa-pen-to-square"></i> Chỉnh sửa trong 24 giờ</h3>${
                form('UpdateReview', row.participationId, `${ratingOptions(`edit-${row.participationId}`, row.rating)}${field('comment', 'Nội dung đánh giá', 0, 2000)}${field('reason', 'Lý do chỉnh sửa từ 10 đến 500 ký tự', 10, 500, 2)}`, '<i class="fa-solid fa-floppy-disk me-1"></i>Lưu thay đổi')
            }</section>` : '<p class="koc-review-modal-note"><i class="fa-solid fa-lock"></i>Đánh giá chỉ được sửa trong 24 giờ và trước khi có tương tác.</p>'}`;
    };

    document.querySelectorAll('.koc-review-data-table').forEach(element => {
        if (!window.jQuery?.fn?.DataTable) return;
        const kind = element.dataset.kind;
        const filter = document.querySelector(`[data-review-filter="${kind}"]`);
        const table = window.jQuery(element).DataTable(TopstarLists.adapt(window.jQuery(element), {
            processing: true,
            serverSide: true,
            searching: false,
            ordering: false,
            lengthChange: false,
            pageLength: 10,
            ajax: {
                url: element.dataset.ajaxUrl,
                type: 'GET',
                data: data => {
                    const values = new FormData(filter);
                    ['search', 'fromDate', 'toDate', 'rating', 'state'].forEach(key => data[key] = values.get(key) || '');
                },
                dataSrc: json => {
                    (json.data || []).forEach(row => rowCache.set(`${kind}:${row.id || row.participationId}`, row));
                    return json.data || [];
                },
                error: () => window.KocToast?.show('Không thể tải dữ liệu đánh giá. Vui lòng thử lại.', 'error')
            },
            columns: [
                { data: null, width: '54px', className: 'text-center', render: (_d, _t, _r, meta) => meta.settings._iDisplayStart + meta.row + 1 },
                { data: 'partnerName', render: value => `<strong class="koc-review-partner"><i class="fa-regular fa-building"></i>${escapeHtml(value)}</strong>` },
                { data: 'campaignTitle', render: value => `<span class="koc-review-campaign" title="${escapeHtml(value)}">${escapeHtml(value)}</span>` },
                { data: 'rating', width: '145px', render: value => value ? stars(value) : '<span class="text-muted">Chưa đánh giá</span>' },
                { data: null, width: '145px', render: (_d, _t, row) => `<span class="koc-review-date">${formatDate(row.createdAtUtc || row.reviewedAtUtc)}</span>` },
                { data: null, width: '145px', render: (_d, _t, row) => statusBadge(kind, row) },
                { data: null, width: '82px', className: 'text-center', render: (_d, _t, row) => `<button type="button" class="btn btn-outline-primary btn-sm js-review-detail" data-key="${kind}:${row.id || row.participationId}" data-kind="${kind}" title="Xem chi tiết"><i class="fa-regular fa-eye"></i></button>` }
            ],
            language: {
                processing: 'Đang tải dữ liệu...', emptyTable: 'Chưa có dữ liệu đánh giá.', zeroRecords: 'Không tìm thấy kết quả phù hợp.',
                info: 'Hiển thị _START_–_END_ trong _TOTAL_ kết quả', infoEmpty: 'Không có dữ liệu', infoFiltered: '',
                paginate: { first: 'Đầu', previous: '<i class="fa-solid fa-chevron-left"></i>', next: '<i class="fa-solid fa-chevron-right"></i>', last: 'Cuối' }
            },
            pagingType: 'full_numbers',
            layout: { topStart: null, topEnd: null, bottomStart: 'info', bottomEnd: 'paging' }
        }));
        tables[kind] = table;
        filter.addEventListener('submit', event => { event.preventDefault(); table.ajax.reload(null, true); });
        filter.querySelector('[name="pageSize"]').addEventListener('change', event => table.page.len(Number(event.target.value) || 10).draw());
    });

    document.addEventListener('click', event => {
        const button = event.target.closest('.js-review-detail');
        if (!button || !modal) return;
        const row = rowCache.get(button.dataset.key);
        if (!row) return;
        context.textContent = `${row.partnerName} · ${row.campaignTitle}`;
        detail.innerHTML = button.dataset.kind === 'received' ? receivedDetail(row) : sentDetail(row);
        const comment = detail.querySelector('textarea[name="comment"]');
        if (comment && row.comment) comment.value = row.comment;
        modal.show();
    });

    detail?.addEventListener('submit', async event => {
        const actionForm = event.target.closest('[data-review-action]');
        if (!actionForm) return;
        event.preventDefault();
        const error = actionForm.querySelector('[data-field-error]');
        actionForm.querySelectorAll('.is-invalid').forEach(item => item.classList.remove('is-invalid'));
        error?.classList.remove('is-visible');
        const rating = actionForm.querySelector('input[name="rating"]:checked');
        if (actionForm.querySelector('input[name="rating"]') && !rating) {
            if (error) { error.textContent = 'Vui lòng chọn số sao đánh giá.'; error.classList.add('is-visible'); }
            return;
        }
        const required = [...actionForm.querySelectorAll('textarea[minlength]')].find(input => input.value.trim().length < Number(input.minLength));
        if (required) {
            required.classList.add('is-invalid');
            if (error) { error.textContent = `Vui lòng nhập tối thiểu ${required.minLength} ký tự.`; error.classList.add('is-visible'); }
            required.focus();
            return;
        }
        const submit = actionForm.querySelector('button[type="submit"]');
        submit.disabled = true;
        try {
            const response = await fetch(actionForm.action, { method: 'POST', body: new FormData(actionForm), headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' } });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Không thể lưu thay đổi.');
            modal.hide();
            window.KocToast?.show(result.message, 'success');
            Object.values(tables).forEach(table => table.ajax.reload(null, false));
        } catch (exception) {
            if (error) { error.textContent = exception.message; error.classList.add('is-visible'); }
            else window.KocToast?.show(exception.message, 'error');
        } finally { submit.disabled = false; }
    });
})();
