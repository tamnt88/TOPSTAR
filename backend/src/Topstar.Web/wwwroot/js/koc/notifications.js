(() => {
    const tableElement = document.getElementById('kocNotificationTable');
    const dataElement = document.getElementById('notificationData');
    if (!tableElement || !dataElement || !window.jQuery?.fn?.DataTable) return;

    const items = JSON.parse(dataElement.textContent || '[]');
    const byId = new Map(items.map(item => [String(item.id), item]));
    const token = document.querySelector('#notificationAntiForgery input[name="__RequestVerificationToken"]')?.value || '';
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[character]);
    const formatDate = value => window.TopstarFormat.dateTime(value);
    const typeMap = {
        Participation: ['Ứng tuyển', 'fa-paper-plane'],
        CampaignCompleted: ['Chiến dịch', 'fa-bullhorn'],
        SubmissionReview: ['Duyệt nội dung', 'fa-file-circle-check'],
        SubmissionOverdue: ['Quá hạn', 'fa-clock'],
        Shipment: ['Giao nhận', 'fa-box'],
        Schedule: ['Lịch booking', 'fa-calendar-check'],
        Dispute: ['Tranh chấp', 'fa-scale-balanced'],
        DisputeMessage: ['Phản hồi tranh chấp', 'fa-comments'],
        CampaignCancelled: ['Chiến dịch hủy', 'fa-ban'],
        Wallet: ['Ví thu nhập', 'fa-wallet'],
        WalletRelease: ['Giải ngân', 'fa-money-bill-transfer'],
        Payout: ['Rút tiền', 'fa-building-columns'],
        CampaignReview: ['Đánh giá', 'fa-star'],
        ReviewModeration: ['Kiểm duyệt đánh giá', 'fa-shield-halved'],
        Violation: ['Cảnh báo tài khoản', 'fa-triangle-exclamation']
    };
    const eventMap = {
        Created: ['Đã tạo', 'fa-circle-plus'],
        Read: ['Đã đọc', 'fa-check'],
        Opened: ['Đã mở công việc', 'fa-arrow-up-right-from-square'],
        Hidden: ['Đã lưu trữ', 'fa-box-archive'],
        Restored: ['Đã khôi phục', 'fa-rotate-left']
    };
    const typeInfo = value => typeMap[value] || ['Cập nhật hệ thống', 'fa-bell'];

    const table = window.jQuery(tableElement).DataTable(TopstarLists.configure(tableElement, {
        serverSide: true,
        ajax: { url: location.pathname + "?handler=Data", data: d => { d.query = document.getElementById("notificationSearch").value; d.filter = document.getElementById("notificationFilter").value; d.category = document.getElementById("notificationCategory")?.value || ""; }, dataSrc: json => { byId.clear(); (json.data || []).forEach(item => byId.set(String(item.id), item)); const summary = json.summary || {}; document.querySelectorAll("[data-notification-count]").forEach(node => node.textContent = Number(summary[node.dataset.notificationCount] || 0).toLocaleString("vi-VN")); document.getElementById("markAllNotificationsRead").hidden = !summary.unreadCount; return json.data || []; } },
        searching: false,
        ordering: false,
        pageLength: 10,
        lengthChange: false,
        autoWidth: false,
        order: [[1, 'desc']],
        columns: [
            {
                data: 'title',
                render: (value, _type, row) => {
                    const info = typeInfo(row.type);
                    return `<div class="koc-notification-summary ${row.isRead ? '' : 'is-unread'}">
                        <span><i class="fa-solid ${info[1]}"></i></span>
                        <div><strong>${escapeHtml(value)}</strong><small>${escapeHtml(info[0])}</small><p>${escapeHtml(row.body)}</p></div>
                    </div>`;
                }
            },
            {
                data: 'createdAtUtc',
                render: value => `<span class="koc-notification-time"><i class="fa-regular fa-clock"></i>${formatDate(value)}</span>`
            },
            {
                data: null,
                render: (_value, _type, row) => row.isVisible
                    ? row.isRead
                        ? '<span class="koc-notification-state is-read"><i class="fa-solid fa-check"></i> Đã đọc</span>'
                        : '<span class="koc-notification-state is-new"><i class="fa-solid fa-circle"></i> Mới</span>'
                    : '<span class="koc-notification-state is-archived"><i class="fa-solid fa-box-archive"></i> Lưu trữ</span>'
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: (_value, _type, row) => `<div class="koc-notification-actions">
                    ${row.targetUrl && row.isVisible ? `<button class="btn btn-primary btn-sm" type="button" data-notification-action="open" data-id="${row.id}" title="Mở công việc"><i class="fa-solid fa-arrow-right"></i><span>Xử lý</span></button>` : ''}
                    <button class="btn btn-outline-primary btn-sm" type="button" data-notification-detail="${row.id}" title="Xem chi tiết"><i class="fa-regular fa-eye"></i><span>Xem</span></button>
                    ${row.isVisible
                        ? `<button class="btn btn-light btn-sm" type="button" data-notification-action="archive" data-id="${row.id}" title="Lưu trữ"><i class="fa-solid fa-box-archive"></i></button>`
                        : `<button class="btn btn-light btn-sm" type="button" data-notification-action="restore" data-id="${row.id}" title="Khôi phục"><i class="fa-solid fa-rotate-left"></i></button>`}
                </div>`
            }
        ],
        language: {
            emptyTable: `<div class="koc-notification-empty"><span><i class="fa-regular fa-bell-slash"></i></span><strong>Không có thông báo phù hợp</strong><p>Bạn đã xử lý hết các cập nhật trong nhóm này. Thông báo mới sẽ xuất hiện tại đây.</p></div>`,
            zeroRecords: `<div class="koc-notification-empty"><span><i class="fa-solid fa-magnifying-glass"></i></span><strong>Không tìm thấy kết quả</strong><p>Hãy thử từ khóa ngắn hơn hoặc kiểm tra nhóm thông báo khác.</p></div>`,
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ thông báo',
            infoEmpty: 'Chưa có thông báo',
            paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' }
        },
        dom: 'rt<"koc-notification-table-footer"ip>',
        drawCallback: () => bindActions()
    }, { query: '#notificationSearch', filter: '#notificationFilter' }, '#notificationPageLength'));

    document.getElementById('notificationSearch')?.addEventListener('keydown', event => { if(event.key === 'Enter') { event.preventDefault(); table.ajax.reload(null, true); } });
    document.getElementById('notificationPageLength')?.addEventListener('change', event => table.page.len(Number(event.target.value)).draw());
    document.getElementById('notificationCategory')?.addEventListener('change', () => table.ajax.reload(null, true));
    document.getElementById('notificationApply')?.addEventListener('click', () => table.ajax.reload(null, true));

    const modalElement = document.getElementById('notificationDetailModal');
    const modal = modalElement ? bootstrap.Modal.getOrCreateInstance(modalElement) : null;
    let selectedId = null;

    function bindActions() {
        tableElement.querySelectorAll('[data-notification-detail]').forEach(button => {
            button.onclick = () => showDetail(button.dataset.notificationDetail);
        });
        tableElement.querySelectorAll('[data-notification-action]').forEach(button => {
            button.onclick = () => runAction(button.dataset.notificationAction, button.dataset.id, button);
        });
    }

    function showDetail(id) {
        const item = byId.get(String(id));
        if (!item || !modal) return;
        selectedId = String(id);
        const info = typeInfo(item.type);
        document.getElementById('notificationDetailTitle').textContent = item.title;
        document.getElementById('notificationDetailMeta').textContent = `${info[0]} · ${formatDate(item.createdAtUtc)}`;
        document.getElementById('notificationDetailBody').textContent = item.body;
        document.getElementById('notificationDetailHistory').innerHTML = item.history?.length
            ? item.history.map(entry => {
                const event = eventMap[entry.eventType] || [entry.eventType, 'fa-circle'];
                return `<div><span><i class="fa-solid ${event[1]}"></i></span><p><strong>${escapeHtml(event[0])}</strong><small>${formatDate(entry.createdAtUtc)}${entry.actorRoleSnapshot ? ` · ${escapeHtml(entry.actorRoleSnapshot)}` : ''}</small></p></div>`;
            }).join('')
            : '<p class="koc-notification-history-empty">Chưa có thay đổi trạng thái.</p>';
        const openButton = document.getElementById('notificationOpenTask');
        openButton.hidden = !item.targetUrl || !item.isVisible;
        modal.show();
        if (!item.isRead && item.isVisible) {
            runAction('read', id, null, false);
        }
    }

    document.getElementById('notificationOpenTask')?.addEventListener('click', () => {
        if (selectedId) runAction('open', selectedId, document.getElementById('notificationOpenTask'));
    });
    document.getElementById('markAllNotificationsRead')?.addEventListener('click', event => runAction('all', '', event.currentTarget));

    async function runAction(action, id, button, reload = true) {
        const handlers = { read: 'ReadAjax', all: 'AllAjax', archive: 'ArchiveAjax', restore: 'RestoreAjax', open: 'OpenAjax' };
        if (!handlers[action]) return;
        if (button) button.disabled = true;
        try {
            const body = new URLSearchParams();
            if (id) body.set('id', id);
            const response = await fetch(`${location.pathname}?handler=${handlers[action]}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
                body
            });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Không thể cập nhật thông báo.');
            window.KocToast?.show(result.message, 'success');
            if (action === 'open' && result.targetUrl) {
                window.location.href = result.targetUrl;
                return;
            }
            if (reload) modal?.hide();
            table.ajax.reload(null, false);
            if (button) button.disabled = false;
        } catch (error) {
            window.KocToast?.show(error.message || 'Không thể kết nối máy chủ.', 'error');
            if (button) button.disabled = false;
        }
    }
})();
