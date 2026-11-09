(() => {
    const tableElement = document.getElementById('kocTaskTable');
    const tableWrap = tableElement?.closest('.koc-task-table-wrap');
    const emptyState = document.createElement('div');
    emptyState.className = 'koc-task-empty-state';
    emptyState.hidden = true;
    emptyState.innerHTML = '<i class="fa-regular fa-folder-open" aria-hidden="true"></i><strong>Chưa có đầu việc</strong><p>Đầu việc sẽ xuất hiện khi bạn được Brand tuyển chọn và giao nội dung.</p><a href="/Koc/Jobs">Tìm chiến dịch phù hợp <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>';
    tableWrap?.appendChild(emptyState);
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[char]);
    const formatDate = value => window.TopstarFormat.dateTime(value);
    const formatDay = value => value
        ? new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(`${value}T00:00:00`))
        : 'Chưa cập nhật';
    const formatMoney = value => `${new Intl.NumberFormat('vi-VN').format(Number(value) || 0)} đ`;
    const richCampaignContent = value => {
        const content = String(value || '').trim();
        if (!content) return '<p class="koc-campaign-rich-empty">Brand chưa cung cấp nội dung chiến dịch.</p>';
        return /<[a-z][\s\S]*>/i.test(content)
            ? content
            : `<p>${escapeHtml(content).replace(/\r?\n/g, '<br>')}</p>`;
    };
    const validHttpUrl = value => {
        if (!value) return true;
        try {
            const url = new URL(value);
            return url.protocol === 'http:' || url.protocol === 'https:';
        } catch { return false; }
    };
    const readJsonResponse = async response => {
        if (!(response.headers.get('content-type') || '').includes('application/json')) {
            await response.text();
            throw new Error('Máy chủ chưa thể xử lý yêu cầu. Vui lòng tải lại trang và thử lại.');
        }
        return response.json();
    };
    const statusMap = {
        NotSubmitted: ['Cần nộp', 'is-pending'],
        NeedRevision: ['Cần chỉnh sửa', 'is-revision'],
        Submitted: ['Chờ Brand duyệt', 'is-review'],
        Approved: ['Đã duyệt', 'is-approved'],
        Rejected: ['Đã từ chối', 'is-rejected']
    };
    const statusText = status => (statusMap[status] || [status, ''])[0];
    const statusClass = status => (statusMap[status] || [status, ''])[1];
    const statusBadge = (status, row) => {
        if (row && !row.fundingReady) return '<span class="koc-task-status is-review"><i class="fa-solid fa-wallet"></i>Chờ Brand ký quỹ</span>';
        if (row && row.campaignStatus === 'Published') return '<span class="koc-task-status is-review"><i class="fa-regular fa-clock"></i>Chờ triển khai</span>';
        return `<span class="koc-task-status ${statusClass(status)}"><i class="fa-solid fa-circle"></i>${escapeHtml(statusText(status))}</span>`;
    };
    const campaignState = row => {
        if (row.campaignStatus === 'Completed') return ['Hoàn tất', 'is-completed', 'fa-circle-check'];
        if (row.campaignEndDate && new Date(`${row.campaignEndDate}T23:59:59`) < new Date()) return ['Hết hạn', 'is-expired', 'fa-clock'];
        if (row.campaignStatus === 'Published') return [row.fundingReady ? 'Chờ triển khai' : 'Đã được chọn', 'is-pending', 'fa-clock'];
        return ['Đang chạy', 'is-running', 'fa-play'];
    };
    const eventText = type => ({
        Replaced: 'KOC thay thế bài nộp',
        Submitted: 'KOC gửi bài',
        Reviewed: 'Brand phản hồi',
        SensitiveDataRedacted: 'Đã ẩn dữ liệu nhạy cảm'
    })[type] || 'Cập nhật đầu việc';
    const csrfToken = () => document.querySelector('[data-task-form] input[name="__RequestVerificationToken"]')?.value || '';

    const detailHtml = row => {
        const overdue = row.dueDateUtc && new Date(row.dueDateUtc) < new Date() && row.canSubmit;
        const links = [
            row.submissionUrl ? `<a href="${escapeHtml(row.submissionUrl)}" target="_blank" rel="noopener noreferrer" class="btn btn-outline-primary btn-sm"><i class="fa-solid fa-arrow-up-right-from-square me-1"></i>Mở nội dung</a>` : '',
            row.attachmentUrl ? `<a href="${escapeHtml(row.attachmentUrl)}" target="_blank" rel="noopener noreferrer" class="btn btn-outline-secondary btn-sm"><i class="fa-solid fa-paperclip me-1"></i>${escapeHtml(row.attachmentName || 'Tệp đính kèm')}</a>` : '',
            row.history?.length ? `<button type="button" class="btn btn-outline-secondary btn-sm js-history-open" data-id="${row.submissionId}"><i class="fa-solid fa-clock-rotate-left me-1"></i>Lịch sử <span>${row.history.length}</span></button>` : ''
        ].join('');
        const action = row.canSubmit
            ? `<button type="button" class="btn btn-primary btn-sm js-task-open" data-id="${row.submissionId}"><i class="fa-solid ${row.status === 'Submitted' ? 'fa-pen-to-square' : 'fa-cloud-arrow-up'} me-1"></i>${row.status === 'Submitted' ? 'Thay thế bài nộp' : row.status === 'NeedRevision' ? 'Nộp lại' : 'Nộp nội dung'}</button>`
            : '';
        return `<div class="koc-task-row-detail">
            <div class="koc-task-meta">
                <span class="${overdue ? 'is-overdue' : ''}"><i class="fa-regular fa-calendar"></i><small>Hạn nộp</small><strong>${formatDate(row.dueDateUtc)}</strong></span>
                <span><i class="fa-solid fa-rotate"></i><small>Phiên bản</small><strong>${Number(row.revisionCount) + 1}</strong></span>
                <span><i class="fa-regular fa-paper-plane"></i><small>Gửi gần nhất</small><strong>${row.submittedAtUtc ? formatDate(row.submittedAtUtc) : 'Chưa gửi'}</strong></span>
            </div>
            ${row.reviewComment ? `<div class="koc-task-feedback"><i class="fa-solid fa-comment-dots"></i><div><strong>Phản hồi từ Brand</strong><p>${escapeHtml(row.reviewComment)}</p></div></div>` : ''}
            <div class="koc-task-footer"><div>${links}</div>${action}</div>
        </div>`;
    };

    const campaignHtml = row => {
        const campaign = campaignState(row);
        const requirements = [
            row.campaignRequiresBooking ? '<span><i class="fa-regular fa-calendar-check"></i>Có lịch booking</span>' : '',
            row.campaignRequiresShipment ? '<span><i class="fa-solid fa-truck-fast"></i>Có gửi sản phẩm</span>' : '',
            !row.campaignRequiresBooking && !row.campaignRequiresShipment ? '<span><i class="fa-solid fa-globe"></i>Làm việc trực tuyến</span>' : ''
        ].join('');
        return `<div class="koc-campaign-detail">
            <header class="koc-campaign-detail-hero">
                <div class="koc-campaign-brand-mark"><i class="fa-solid fa-building"></i></div>
                <div>
                    <small>Chiến dịch của ${escapeHtml(row.brandName)}</small>
                    <h3>${escapeHtml(row.campaignTitle)}</h3>
                    <span class="koc-campaign-state ${campaign[1]}"><i class="fa-solid ${campaign[2]}"></i>${campaign[0]}</span>
                </div>
            </header>
            <section class="koc-campaign-brief">
                <header>
                    <span><i class="fa-solid fa-file-lines"></i></span>
                    <div><strong>Nội dung chiến dịch</strong><small>Brief và yêu cầu do Brand cung cấp</small></div>
                </header>
                <div class="koc-campaign-rich-content">${richCampaignContent(row.campaignObjective)}</div>
            </section>
            <div class="koc-campaign-detail-grid">
                <section>
                    <span><i class="fa-solid fa-wallet"></i></span>
                    <div><small>Tổng ngân sách dự kiến</small><strong>${formatMoney(row.campaignBudgetAmount)}</strong><p>Ngân sách toàn chiến dịch, không phải mức thù lao mặc định của một KOC.</p></div>
                </section>
                <section>
                    <span><i class="fa-solid fa-calendar-days"></i></span>
                    <div><small>Thời gian chiến dịch</small><strong>${formatDay(row.campaignStartDate)} <i class="fa-solid fa-arrow-right"></i> ${formatDay(row.campaignEndDate)}</strong></div>
                </section>
                <section>
                    <span><i class="fa-solid fa-box-open"></i></span>
                    <div><small>Gói & hình thức phối hợp</small><strong>${escapeHtml(row.campaignPackageCode || 'Chưa phân loại')}</strong><div class="koc-campaign-requirements">${requirements}</div></div>
                </section>
            </div>
            <section class="koc-campaign-assignment">
                <div><i class="fa-solid fa-list-check"></i><span><small>Đầu việc được giao</small><strong>${escapeHtml(row.deliverableType)}</strong></span></div>
                <div><i class="fa-solid fa-boxes-stacked"></i><span><small>Số lượng</small><strong>${Number(row.quantity) || 0}</strong></span></div>
                <div><i class="fa-regular fa-clock"></i><span><small>Hạn nộp</small><strong>${formatDate(row.dueDateUtc)}</strong></span></div>
            </section>
        </div>`;
    };

    const redactionFormHtml = history => {
        if (history.eventType === 'SensitiveDataRedacted' || (!history.submissionUrl && !history.submissionNote && !history.attachmentUrl)) return '';
        return `<form method="post" action="${escapeHtml(tableElement.dataset.redactUrl)}" class="koc-history-redact-form" data-redact-form novalidate>
            <input type="hidden" name="__RequestVerificationToken" value="${escapeHtml(csrfToken())}">
            <input type="hidden" name="historyId" value="${escapeHtml(history.id)}">
            <button type="button" class="btn btn-outline-danger btn-sm" data-redact-open><i class="fa-solid fa-shield-halved me-1"></i>Ẩn dữ liệu nhạy cảm</button>
            <div class="koc-history-redact-panel" data-redact-panel>
                <strong><i class="fa-solid fa-user-shield"></i>Chọn dữ liệu cần ẩn vĩnh viễn</strong>
                <div class="koc-history-redact-options">
                    ${history.submissionUrl ? '<label><input type="checkbox" name="redactLink" value="true"> Link nội dung</label>' : ''}
                    ${history.submissionNote ? '<label><input type="checkbox" name="redactNote" value="true"> Ghi chú của KOC</label>' : ''}
                    ${history.attachmentUrl ? '<label><input type="checkbox" name="redactAttachment" value="true"> Tệp đính kèm</label>' : ''}
                </div>
                <p class="koc-field-error" data-redact-selection-error></p>
                <label>Lý do xử lý <span>(bắt buộc)</span></label>
                <textarea name="reason" class="form-control form-control-sm" rows="2" maxlength="500" placeholder="Nhập tối thiểu 10 ký tự; không nhập lại dữ liệu nhạy cảm vào đây"></textarea>
                <p class="koc-field-error" data-redact-reason-error></p>
                <p class="koc-field-error" data-redact-server-error></p>
                <small>Thao tác không thể hoàn tác. Dữ liệu đã chọn sẽ bị thu hồi khỏi bài nộp và lịch sử.</small>
                <div><button type="button" class="btn btn-light btn-sm" data-redact-cancel><i class="fa-solid fa-xmark me-1"></i>Hủy</button><button type="submit" class="btn btn-danger btn-sm"><i class="fa-solid fa-shield-halved me-1"></i>Xác nhận ẩn dữ liệu</button></div>
            </div>
        </form>`;
    };

    const historyHtml = row => {
        const histories = [...(row.history || [])].sort((a, b) => new Date(b.createdAtUtc) - new Date(a.createdAtUtc));
        const timeline = histories.map((history, index) => {
            const resources = `${history.submissionUrl ? `<a href="${escapeHtml(history.submissionUrl)}" target="_blank" rel="noopener noreferrer" class="btn btn-outline-primary btn-sm"><i class="fa-solid fa-arrow-up-right-from-square me-1"></i>Mở nội dung</a>` : ''}${history.attachmentUrl ? `<a href="${escapeHtml(history.attachmentUrl)}" target="_blank" rel="noopener noreferrer" class="btn btn-outline-secondary btn-sm"><i class="fa-solid fa-paperclip me-1"></i>${escapeHtml(history.attachmentName || 'Tệp đính kèm')}</a>` : ''}`;
            const empty = !history.submissionUrl && !history.attachmentUrl && !history.submissionNote && !history.reviewComment
                ? '<p class="koc-history-empty"><i class="fa-regular fa-folder-open"></i>Không có nội dung bổ sung trong sự kiện này.</p>' : '';
            return `<article class="koc-history-event ${statusClass(history.status)}">
                <div class="koc-history-marker"><i class="fa-solid fa-paper-plane"></i></div>
                <div class="koc-history-event-card">
                    <header><div><strong>Phiên bản ${Number(history.revisionNo) + 1} · ${escapeHtml(eventText(history.eventType))}</strong>${index === 0 ? '<span>Mới nhất</span>' : ''}</div><time><i class="fa-regular fa-clock"></i>${formatDate(history.createdAtUtc)}</time></header>
                    <div class="koc-history-status-row">${statusBadge(history.status)}</div>
                    ${history.submissionNote ? `<section class="koc-history-message is-koc"><strong><i class="fa-regular fa-note-sticky"></i>Ghi chú của KOC</strong><p>${escapeHtml(history.submissionNote)}</p></section>` : ''}
                    ${history.reviewComment ? `<section class="koc-history-message is-brand"><strong><i class="fa-solid fa-comment-dots"></i>Phản hồi từ Brand</strong><p>${escapeHtml(history.reviewComment)}</p></section>` : ''}
                    ${resources ? `<div class="koc-history-resources">${resources}</div>` : ''}${redactionFormHtml(history)}${empty}
                </div>
            </article>`;
        }).join('');
        const revisions = new Set(histories.map(item => item.revisionNo)).size;
        return `<div class="koc-history-summary">
            <div><span><i class="fa-solid fa-code-branch"></i></span><small>Tổng phiên bản</small><strong>${revisions}</strong></div>
            <div><span><i class="fa-solid fa-pen-to-square"></i></span><small>Lần chỉnh sửa</small><strong>${row.revisionCount}</strong></div>
            <div><span><i class="fa-solid fa-circle-check"></i></span><small>Trạng thái hiện tại</small><strong class="koc-task-status ${statusClass(row.status)}">${escapeHtml(statusText(row.status))}</strong></div>
        </div><div class="koc-history-timeline">${timeline}</div>`;
    };

    const detailModalElement = document.getElementById('taskDetailModal');
    const detailModal = detailModalElement
        ? bootstrap.Modal.getOrCreateInstance(detailModalElement, { backdrop: 'static', keyboard: false })
        : null;
    let bindRedaction = () => {};
    let prepareSubmission = () => {};

    let taskTable;
    const rowCache = new Map();
    if (tableElement && window.jQuery?.fn?.DataTable) {
        const $table = window.jQuery(tableElement);
        const focusId = new URLSearchParams(window.location.search).get('focus');
        let focusHandled = false;
        taskTable = $table.DataTable(TopstarLists.configure(tableElement, {
            processing: true,
            serverSide: true,
            searching: false,
            ordering: false,
            pageLength: Number(document.getElementById('PageSize')?.value) || 20,
            lengthChange: false,
            ajax: {
                url: tableElement.dataset.ajaxUrl,
                type: 'GET',
                data: data => {
                    data.query = document.getElementById('Search')?.value || '';
                    data.status = document.getElementById('Status')?.value || 'all';
                    data.due = document.getElementById('Due')?.value || 'all';
                    data.sort = document.getElementById('Sort')?.value || 'due';
                },
                dataSrc: json => {
                    const summary = json.summary || {};
                    const noResults = !(json.data || []).length;
                    tableWrap?.classList.toggle('is-empty', noResults);
                    emptyState.hidden = !noResults;
                    emptyState.querySelector('strong').textContent = json.recordsTotal > 0 ? 'Không có đầu việc phù hợp' : 'Chưa có đầu việc';
                    emptyState.querySelector('p').textContent = json.recordsTotal > 0 ? 'Thử thay đổi trạng thái hoặc xóa bộ lọc để xem các đầu việc khác.' : 'Đầu việc sẽ xuất hiện khi bạn được Brand tuyển chọn và giao nội dung.';
                    document.querySelectorAll("[data-task-count]").forEach(node => { node.textContent = Number(summary[node.dataset.taskCount] || 0).toLocaleString("vi-VN"); });
                    const overdue = document.getElementById("taskOverdue");
                    if (overdue) overdue.hidden = !summary.overdueCount;
                    rowCache.clear();
                    (json.data || []).forEach(row => rowCache.set(row.submissionId, row));
                    return json.data || [];
                },
                error: () => window.KocToast?.show('Không thể tải danh sách đầu việc. Vui lòng thử lại.', 'error')
            },
            columns: [
                { data: null, width: '54px', className: 'text-center', render: (_data, _type, _row, meta) => meta.settings._iDisplayStart + meta.row + 1 },
                { data: null, render: (_data, _type, row) => `<div class="koc-table-campaign"><strong title="${escapeHtml(row.campaignTitle)}">${escapeHtml(row.campaignTitle)}</strong><small>${escapeHtml(row.brandName)}</small></div>` },
                { data: null, render: (_data, _type, row) => `<div class="koc-table-deliverable"><strong>${escapeHtml(row.deliverableType)}</strong><small><i class="fa-solid fa-boxes-stacked"></i>Số lượng: ${row.quantity}</small></div>` },
                { data: 'dueDateUtc', render: value => `<span class="koc-table-date"><i class="fa-regular fa-calendar"></i>${value ? formatDate(value) : 'Chưa đặt hạn'}</span>` },
                { data: 'revisionCount', width: '96px', className: 'text-center', render: value => `<span class="koc-table-version">${Number(value) + 1}</span>` },
                { data: null, render: (_data, _type, row) => { const campaign = campaignState(row); return `<div class="koc-table-states"><span class="koc-campaign-state ${campaign[1]}"><i class="fa-solid ${campaign[2]}"></i>${campaign[0]}</span>${statusBadge(row.status,row)}</div>`; } },
                { data: null, width: '92px', className: 'text-center', render: (_data, _type, row) => `<button type="button" class="koc-task-collapse-toggle js-task-detail" data-id="${row.submissionId}" aria-label="Xem chi tiết đầu việc" title="Xem chi tiết"><i class="fa-solid fa-eye"></i></button>` }
            ],
            language: {
                processing: 'Đang tải dữ liệu...', emptyTable: 'Chưa có đầu việc.', zeroRecords: 'Không tìm thấy đầu việc phù hợp.',
                info: 'Hiển thị _START_–_END_ trong _TOTAL_ đầu việc', infoEmpty: 'Không có dữ liệu', infoFiltered: '',
                paginate: { first: 'Đầu', previous: '<i class="fa-solid fa-chevron-left"></i>', next: '<i class="fa-solid fa-chevron-right"></i>', last: 'Cuối' }
            },
            layout: { topStart: null, topEnd: null, bottomStart: 'info', bottomEnd: 'paging' }
        }, { Search: '#Search', Status: '#Status', Due: '#Due', Sort: '#Sort' }, '#PageSize'));
        document.querySelector('.koc-task-filters')?.addEventListener('submit', event => {
            event.preventDefault();
            taskTable.ajax.reload(null, true);
        });
        document.querySelectorAll('#Status, #Due, #Sort').forEach(input => input.addEventListener('change', () => taskTable.ajax.reload(null, true)));
        document.getElementById('PageSize')?.addEventListener('change', event => taskTable.page.len(Number(event.target.value) || 20).draw());
        $table.on('draw.dt', () => {
            if (!focusId || focusHandled) return;
            taskTable.rows().every(function () {
                const row = this.data();
                if (focusHandled || String(row?.submissionId || '').toLowerCase() !== focusId.toLowerCase()) return;
                focusHandled = true;
                const node = this.node();
                const detailButton = node?.querySelector('.js-task-detail');
                detailButton?.click();
                node?.scrollIntoView({ behavior: 'smooth', block: 'center' });
                node?.classList.add('is-focused');
                setTimeout(() => {
                    node?.classList.remove('is-focused');
                }, 350);
            });
        });
    }

    document.addEventListener('click', event => {
        const detailButton = event.target.closest('.js-task-detail');
        if (!detailButton || !taskTable || !detailModal) return;
        const row = rowCache.get(detailButton.dataset.id) || taskTable.row(detailButton.closest('tr')).data();
        if (!row) return;
        detailModalElement.querySelector('[data-task-detail-context]').textContent = `${row.campaignTitle} · ${row.deliverableType}`;
        detailModalElement.querySelector('[data-task-detail-content]').innerHTML = detailHtml(row);
        detailModalElement.querySelector('[data-task-campaign-content]').innerHTML = campaignHtml(row);
        const historyContent = detailModalElement.querySelector('[data-history-content]');
        historyContent.innerHTML = historyHtml(row);
        bindRedaction(historyContent);
        detailModalElement.querySelector('[data-history-count]').textContent = row.history?.length || 0;
        prepareSubmission(row);
        bootstrap.Tab.getOrCreateInstance(detailModalElement.querySelector('[data-task-tab="overview"]')).show();
        detailModal.show();
    });
    detailModalElement?.addEventListener('hidden.bs.modal', () => {
        detailModalElement.querySelector('[data-task-detail-content]')?.replaceChildren();
        detailModalElement.querySelector('[data-task-campaign-content]')?.replaceChildren();
        detailModalElement.querySelector('[data-history-content]')?.replaceChildren();
    });

    bindRedaction = container => {
        container.querySelectorAll('[data-redact-form]').forEach(redactForm => {
            const panel = redactForm.querySelector('[data-redact-panel]');
            const reason = redactForm.querySelector('textarea[name="reason"]');
            const options = redactForm.querySelector('.koc-history-redact-options');
            const selectionError = redactForm.querySelector('[data-redact-selection-error]');
            const reasonError = redactForm.querySelector('[data-redact-reason-error]');
            const serverError = redactForm.querySelector('[data-redact-server-error]');
            const clearRedactErrors = () => {
                options.classList.remove('is-invalid');
                reason.classList.remove('is-invalid');
                reason.removeAttribute('aria-invalid');
                [selectionError, reasonError, serverError].forEach(message => {
                    message.textContent = '';
                    message.classList.remove('is-visible');
                });
            };
            redactForm.querySelector('[data-redact-open]').addEventListener('click', () => { panel.classList.add('is-open'); reason.focus(); });
            redactForm.querySelector('[data-redact-cancel]').addEventListener('click', () => { panel.classList.remove('is-open'); clearRedactErrors(); });
            options.addEventListener('change', () => {
                if (redactForm.querySelector('input[type="checkbox"]:checked')) {
                    options.classList.remove('is-invalid');
                    selectionError.textContent = '';
                    selectionError.classList.remove('is-visible');
                }
            });
            reason.addEventListener('input', () => {
                if (reason.value.trim().length >= 10 && reason.value.trim().length <= 500) {
                    reason.classList.remove('is-invalid');
                    reason.removeAttribute('aria-invalid');
                    reasonError.textContent = '';
                    reasonError.classList.remove('is-visible');
                }
            });
            redactForm.addEventListener('submit', async event => {
                event.preventDefault();
                clearRedactErrors();
                let firstInvalid = null;
                if (!redactForm.querySelector('input[type="checkbox"]:checked')) {
                    options.classList.add('is-invalid');
                    selectionError.textContent = 'Vui lòng chọn ít nhất một dữ liệu cần ẩn.';
                    selectionError.classList.add('is-visible');
                    firstInvalid = options.querySelector('input[type="checkbox"]');
                }
                if (reason.value.trim().length < 10 || reason.value.trim().length > 500) {
                    reason.classList.add('is-invalid');
                    reason.setAttribute('aria-invalid', 'true');
                    reasonError.textContent = 'Vui lòng nhập lý do từ 10 đến 500 ký tự.';
                    reasonError.classList.add('is-visible');
                    firstInvalid ??= reason;
                }
                if (firstInvalid) {
                    firstInvalid.focus();
                    return;
                }
                const submit = redactForm.querySelector('button[type="submit"]');
                submit.disabled = true;
                try {
                    const response = await fetch(redactForm.action, { method: 'POST', body: new FormData(redactForm), headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' } });
                    const result = await readJsonResponse(response);
                    if (!response.ok || !result.success) throw new Error(result.message || 'Không thể ẩn dữ liệu.');
                    detailModal?.hide();
                    window.KocToast?.show(result.message, 'success');
                    taskTable?.ajax.reload(null, false);
                } catch (exception) { serverError.textContent = exception.message; serverError.classList.add('is-visible'); }
                finally { submit.disabled = false; }
            });
        });
    };

    document.addEventListener('click', event => {
        const button = event.target.closest('.js-history-open');
        if (!button || !detailModal) return;
        bootstrap.Tab.getOrCreateInstance(detailModalElement.querySelector('[data-task-tab="history"]')).show();
    });

    const modalElement = detailModalElement;
    if (!modalElement) return;
    const modal = detailModal;
    const form = modalElement.querySelector('[data-task-form]');
    const idInput = form.querySelector('input[name="id"]');
    const urlInput = form.querySelector('input[name="submissionUrl"]');
    const fileInput = form.querySelector('input[name="attachmentFile"]');
    const noteInput = form.querySelector('textarea[name="note"]');
    const noteCount = form.querySelector('[data-note-count]');
    const urlError = form.querySelector('[data-task-url-error]');
    const fileError = form.querySelector('[data-task-file-error]');
    const noteError = form.querySelector('[data-task-note-error]');
    const error = form.querySelector('[data-task-error]');
    const formTitle = form.querySelector('[data-task-form-title]');
    const submitLabel = form.querySelector('[data-task-submit-label]');
    const allowedExtensions = ['jpg', 'jpeg', 'png', 'webp', 'pdf', 'doc', 'docx', 'mp4', 'mov'];
    const clearErrors = () => {
        [urlInput, fileInput, noteInput].forEach(field => {
            field.classList.remove('is-invalid');
            field.removeAttribute('aria-invalid');
        });
        [error, urlError, fileError, noteError].forEach(message => {
            message.textContent = '';
            message.classList.remove('is-visible');
        });
    };
    const showError = (message, field, target = error, focus = true) => {
        target.textContent = message;
        target.classList.add('is-visible');
        field?.classList.add('is-invalid');
        field?.setAttribute('aria-invalid', 'true');
        if (focus) field?.focus();
    };
    const clearFieldError = (field, target) => {
        field.classList.remove('is-invalid');
        field.removeAttribute('aria-invalid');
        target.textContent = '';
        target.classList.remove('is-visible');
    };
    urlInput.addEventListener('input', () => clearFieldError(urlInput, urlError));
    fileInput.addEventListener('change', () => clearFieldError(fileInput, fileError));
    noteInput.addEventListener('input', () => {
        noteCount.textContent = noteInput.value.length.toLocaleString('vi-VN');
        if (noteInput.value.trim()) {
            noteInput.classList.remove('is-invalid');
            noteInput.removeAttribute('aria-invalid');
            noteError.textContent = '';
            noteError.classList.remove('is-visible');
        }
    });
    prepareSubmission = row => {
        clearErrors();
        form.reset();
        idInput.value = row.submissionId;
        urlInput.value = row.submissionUrl || '';
        noteInput.value = row.submissionNote || '';
        noteCount.textContent = noteInput.value.length.toLocaleString('vi-VN');
        formTitle.textContent = row.status === 'Submitted' ? 'Thay thế bài nộp' : row.status === 'NeedRevision' ? 'Nộp lại nội dung' : 'Nộp nội dung';
        submitLabel.textContent = row.status === 'Submitted' ? 'Thay thế và gửi lại' : 'Gửi Brand duyệt';
        form.closest('.tab-pane').hidden = !row.canSubmit;
        const submitTab = modalElement.querySelector('[data-task-tab="submit"]');
        submitTab.hidden = !row.canSubmit;
    };
    document.addEventListener('click', event => {
        const button = event.target.closest('.js-task-open');
        if (!button) return;
        const row = rowCache.get(button.dataset.id);
        if (!row) return;
        prepareSubmission(row);
        bootstrap.Tab.getOrCreateInstance(modalElement.querySelector('[data-task-tab="submit"]')).show();
    });
    form.addEventListener('submit', async event => {
        event.preventDefault(); clearErrors();
        const url = urlInput.value.trim(), file = fileInput.files[0], note = noteInput.value.trim();
        let firstInvalid = null;
        const invalidate = (message, field, target) => {
            showError(message, field, target, false);
            firstInvalid ??= field;
        };
        if (!url && !file) invalidate('Vui lòng nhập link nội dung hoặc chọn một tệp để tải lên.', urlInput, urlError);
        else if (!validHttpUrl(url)) invalidate('Link phải là địa chỉ hợp lệ bắt đầu bằng http:// hoặc https://.', urlInput, urlError);
        if (file) {
            if (!allowedExtensions.includes(file.name.split('.').pop()?.toLowerCase())) invalidate('Định dạng tệp chưa được hỗ trợ.', fileInput, fileError);
            else if (file.size > 25 * 1024 * 1024) invalidate('Dung lượng tệp không được vượt quá 25 MB.', fileInput, fileError);
        }
        if (!note) invalidate('Ghi chú cho Brand là bắt buộc.', noteInput, noteError);
        else if (note.length > 2000) invalidate('Ghi chú không được vượt quá 2.000 ký tự.', noteInput, noteError);
        if (firstInvalid) {
            firstInvalid.focus();
            return;
        }
        const submit = form.querySelector('button[type="submit"]'); submit.disabled = true;
        try {
            const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json' } });
            const result = await readJsonResponse(response);
            if (!response.ok || !result.success) throw new Error(result.message || 'Không thể gửi nội dung.');
            modal.hide(); window.KocToast?.show(result.message, 'success'); taskTable?.ajax.reload(null, false);
        } catch (exception) { window.KocToast?.show(exception.message || 'Không thể kết nối máy chủ. Vui lòng thử lại.', 'error'); }
        finally { submit.disabled = false; }
    });
})();
