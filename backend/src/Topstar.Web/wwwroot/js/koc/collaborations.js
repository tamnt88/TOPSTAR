(() => {
    const tableElement = document.getElementById('kocCollaborationTable');
    if (!tableElement || !window.jQuery?.fn?.DataTable) return;

    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[character]);

    const formatDate = value => window.TopstarFormat.dateTime(value);

    const statusMap = {
        Confirmed: ['Đang hợp tác', 'is-active', 'fa-arrows-rotate'],
        InProgress: ['Đang thực hiện', 'is-active', 'fa-play'],
        Completed: ['Hoàn tất', 'is-completed', 'fa-circle-check'],
        Cancelled: ['Đã hủy', 'is-cancelled', 'fa-ban'],
        Rejected: ['Đã từ chối', 'is-cancelled', 'fa-circle-xmark'],
        Pending: ['Chờ xác nhận', 'is-pending', 'fa-clock']
    };
    const statusMeta = value => statusMap[value] || [value || 'Chưa cập nhật', 'is-neutral', 'fa-circle-info'];
    const statusBadge = value => {
        const status = statusMeta(value);
        return `<span class="koc-collaboration-status ${status[1]}"><i class="fa-solid ${status[2]}"></i>${escapeHtml(status[0])}</span>`;
    };

    const eventStatusText = value => ({
        Confirmed: 'Đã xác nhận', Completed: 'Hoàn tất', Approved: 'Đã duyệt',
        Submitted: 'Đã gửi', NeedRevision: 'Cần chỉnh sửa', Rejected: 'Đã từ chối',
        Cancelled: 'Đã hủy', Created: 'Đã tạo', InTransit: 'Đang giao',
        Delivered: 'Đã nhận', Pending: 'Đang chờ', Funded: 'Đã ký quỹ',
        Active: 'Đang đối soát', Accepted: 'Đã nghiệm thu', PartiallyAccepted: 'Nghiệm thu một phần'
    })[value] || value || 'Đã cập nhật';

    const timelineHtml = row => {
        if (!row.events?.length) {
            return `<div class="koc-collaboration-empty">
                <i class="fa-regular fa-folder-open"></i>
                <div><strong>Chưa có sự kiện lịch sử</strong><span>Các thay đổi phát sinh sẽ được lưu tại đây.</span></div>
            </div>`;
        }
        const events = row.events.map(event => `<article class="koc-collaboration-event">
            <span class="koc-collaboration-marker"></span>
            <div class="koc-collaboration-event-card">
                <div class="koc-collaboration-event-main">
                    <div class="koc-collaboration-event-state">
                        <span class="koc-collaboration-domain">${escapeHtml(event.domain)}</span>
                        <strong>${escapeHtml(eventStatusText(event.status))}</strong>
                    </div>
                    ${event.note
                        ? `<p>${escapeHtml(event.note)}</p>`
                        : '<p class="is-empty">Không có ghi chú bổ sung.</p>'}
                </div>
                <div class="koc-collaboration-event-meta">
                    <time><i class="fa-regular fa-clock"></i>${formatDate(event.occurredAtUtc)}</time>
                    ${event.actorRole ? `<span><i class="fa-regular fa-user"></i>${escapeHtml(event.actorRole)}</span>` : ''}
                </div>
            </div>
        </article>`).join('');
        return `<div class="koc-collaboration-detail">
            <div class="koc-collaboration-detail-title">
                <div><i class="fa-solid fa-timeline"></i><span><strong>Timeline hợp tác</strong><small>${escapeHtml(row.campaignTitle)} · ${escapeHtml(row.brandName)}</small></span></div>
                <span>${row.eventCount} sự kiện</span>
            </div>
            <div class="koc-operation-summary">
                <div><small>Bước hiện tại</small><strong>${escapeHtml(row.currentStep)}</strong></div>
                <div><small>Đang chờ</small><strong>${escapeHtml(row.waitingFor)}</strong></div>
                <p><i class="fa-solid fa-arrow-right"></i>${escapeHtml(row.nextAction)}</p>
            </div>
            <div class="koc-collaboration-timeline">${events}</div>
        </div>`;
    };

    const $table = window.jQuery(tableElement);
    const dataTable = $table.DataTable(TopstarLists.adapt($table, {
        processing: true,
        serverSide: true,
        searching: false,
        ordering: false,
        pageLength: 10,
        lengthChange: false,
        autoWidth: false,
        ajax: {
            url: tableElement.dataset.ajaxUrl,
            type: 'GET',
            data: request => {
                request.searchValue = document.getElementById('collaborationSearch')?.value.trim() || '';
                request.status = document.getElementById('collaborationStatus')?.value || '';
            },
            dataSrc: json => {
                document.querySelector('[data-collaboration-count]').innerHTML =
                    `<i class="fa-solid fa-handshake-angle"></i> ${Number(json.recordsFiltered) || 0} lần hợp tác`;
                return json.data || [];
            },
            error: () => window.KocToast?.show('Không thể tải lịch sử hợp tác. Vui lòng thử lại.', 'error')
        },
        columns: [
            {
                data: null, width: '54px', orderable: false,
                render: (_data, _type, _row, meta) => meta.settings._iDisplayStart + meta.row + 1
            },
            {
                data: 'campaignTitle',
                render: value => `<div class="koc-collaboration-primary"><span><i class="fa-solid fa-bullhorn"></i></span><strong title="${escapeHtml(value)}">${escapeHtml(value)}</strong></div>`
            },
            {
                data: 'brandName',
                render: value => `<div class="koc-collaboration-brand"><strong title="${escapeHtml(value)}">${escapeHtml(value)}</strong><small><i class="fa-regular fa-building"></i> Đối tác chiến dịch</small></div>`
            },
            {
                data: 'confirmedAtUtc',
                render: (value, type) => type === 'sort' ? value : `<span class="koc-collaboration-date"><i class="fa-regular fa-calendar-check"></i>${formatDate(value)}</span>`
            },
            { data: 'status', render: value => statusBadge(value) },
            {
                data: 'eventCount', width: '92px',
                render: value => `<span class="koc-collaboration-events"><i class="fa-solid fa-clock-rotate-left"></i>${Number(value)} sự kiện</span>`
            },
            {
                data: null, width: '64px', orderable: false,
                render: (_data, _type, row) => `<button type="button" class="koc-collaboration-toggle" data-id="${row.participationId}" aria-expanded="false" title="Xem lịch sử"><i class="fa-solid fa-chevron-down"></i></button>`
            }
        ],
        language: {
            processing: 'Đang tải dữ liệu...',
            emptyTable: 'Chưa có lần hợp tác nào.',
            zeroRecords: 'Không tìm thấy lịch sử phù hợp.',
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ lần hợp tác',
            infoEmpty: 'Không có dữ liệu',
            infoFiltered: '',
            paginate: {
                first: 'Đầu',
                previous: '<i class="fa-solid fa-chevron-left"></i>',
                next: '<i class="fa-solid fa-chevron-right"></i>',
                last: 'Cuối'
            }
        },
        layout: { topStart: null, topEnd: null, bottomStart: 'info', bottomEnd: 'paging' }
    }));

    document.getElementById('collaborationFilters')?.addEventListener('submit', event => {
        event.preventDefault();
        dataTable.page.len(Number(document.getElementById('collaborationPageSize')?.value) || 10);
        dataTable.ajax.reload(null, true);
    });

    document.getElementById('collaborationPageSize')?.addEventListener('change', event => {
        dataTable.page.len(Number(event.target.value) || 10).draw();
    });

    $table.on('click', '.koc-collaboration-toggle', function () {
        const tableRow = this.closest('tr');
        const row = dataTable.row(tableRow);
        const open = !row.child.isShown();
        if (open) {
            row.child(timelineHtml(row.data()), 'koc-collaboration-child-row').show();
        } else {
            row.child.hide();
        }
        this.classList.toggle('is-open', open);
        this.setAttribute('aria-expanded', String(open));
        this.title = open ? 'Thu gọn lịch sử' : 'Xem lịch sử';
    });
})();
