(() => {
    const tableElement = document.getElementById('kocDisputeTable');
    if (!tableElement || !window.jQuery?.fn?.DataTable) return;

    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, character => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[character]);
    const formatDate = value => window.TopstarFormat.dateTime(value);
    const statusMap = {
        Open: ['Mới mở', 'is-open', 'fa-circle-plus'],
        InReview: ['Đang xử lý', 'is-review', 'fa-spinner'],
        Resolved: ['Đã giải quyết', 'is-resolved', 'fa-circle-check'],
        Rejected: ['Đã từ chối', 'is-rejected', 'fa-circle-xmark'],
        Cancelled: ['Đã hủy', 'is-cancelled', 'fa-ban']
    };
    const categoryMap = {
        Booking: 'Lịch booking',
        Shipment: 'Giao nhận sản phẩm',
        Deliverable: 'Nội dung bàn giao',
        Payment: 'Thanh toán',
        Review: 'Đánh giá',
        Other: 'Vấn đề khác'
    };
    const statusBadge = value => {
        const status = statusMap[value] || [value || 'Chưa rõ', 'is-cancelled', 'fa-circle-question'];
        return `<span class="koc-dispute-status ${status[1]}"><i class="fa-solid ${status[2]}"></i>${escapeHtml(status[0])}</span>`;
    };

    const table = window.jQuery(tableElement).DataTable(TopstarLists.adapt(window.jQuery(tableElement), {
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
                request.query = document.getElementById('disputeSearch')?.value.trim() || '';
                request.status = document.getElementById('disputeStatus')?.value || '';
            },
            dataSrc: response => {
                const total = Number(response.recordsFiltered) || 0;
                document.querySelector('[data-dispute-count]').innerHTML =
                    `<i class="fa-solid fa-ticket"></i> ${total} yêu cầu`;
                return response.data || [];
            },
            error: () => window.KocToast?.show('Không thể tải danh sách tranh chấp. Vui lòng thử lại.', 'error')
        },
        columns: [
            {
                data: null, width: '54px',
                render: (_value, _type, _row, meta) => meta.settings._iDisplayStart + meta.row + 1
            },
            {
                data: 'campaignTitle',
                render: (value, _type, row) => `<div class="koc-dispute-campaign">
                    <span><i class="fa-solid fa-bullhorn"></i></span>
                    <div><strong title="${escapeHtml(value)}">${escapeHtml(value)}</strong><small><i class="fa-regular fa-building"></i>${escapeHtml(row.brandName)}</small></div>
                </div>`
            },
            {
                data: 'subject',
                render: (value, _type, row) => `<div class="koc-dispute-subject">
                    <strong title="${escapeHtml(value)}">${escapeHtml(value)}</strong>
                    <small>${escapeHtml(categoryMap[row.category] || row.category)}</small>
                </div>`
            },
            {
                data: 'createdAtUtc',
                render: value => `<span class="koc-dispute-date"><i class="fa-regular fa-calendar"></i>${formatDate(value)}</span>`
            },
            { data: 'status', render: value => statusBadge(value) },
            {
                data: 'historyCount', width: '94px',
                render: value => `<span class="koc-dispute-history"><i class="fa-regular fa-comments"></i>${Number(value) || 0} cập nhật</span>`
            },
            {
                data: null, width: '98px',
                render: (_value, _type, row) => {
                    const url = `${tableElement.dataset.detailPage}?id=${encodeURIComponent(row.id)}`;
                    return `<a class="btn btn-outline-primary btn-sm koc-dispute-detail-button" href="${url}">
                        <i class="fa-regular fa-eye"></i><span>Xem</span>
                    </a>`;
                }
            }
        ],
        language: {
            processing: '<div class="koc-table-loading"><i class="fa-solid fa-spinner fa-spin"></i><span>Đang tải yêu cầu...</span></div>',
            emptyTable: `<div class="koc-dispute-empty">
                <span><i class="fa-solid fa-scale-balanced"></i></span>
                <div><strong>Chưa có tranh chấp nào</strong><p>Khi phát sinh vấn đề trong quá trình hợp tác, bạn có thể tạo yêu cầu để Admin hỗ trợ.</p></div>
                <button type="button" class="btn btn-primary btn-sm" data-bs-toggle="modal" data-bs-target="#openDisputeModal"><i class="fa-solid fa-plus"></i>Mở tranh chấp</button>
            </div>`,
            zeroRecords: `<div class="koc-dispute-empty is-search">
                <span><i class="fa-solid fa-magnifying-glass"></i></span>
                <div><strong>Không tìm thấy yêu cầu phù hợp</strong><p>Hãy thay đổi từ khóa hoặc trạng thái để xem kết quả khác.</p></div>
            </div>`,
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ yêu cầu',
            infoEmpty: 'Chưa có yêu cầu',
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

    document.getElementById('disputeFilters')?.addEventListener('submit', event => {
        event.preventDefault();
        table.page.len(Number(document.getElementById('disputePageSize')?.value) || 10);
        table.ajax.reload();
    });

    const openForm = document.getElementById('openDisputeForm');
    const participation = document.getElementById('disputeParticipationId');
    const participationSearch = document.getElementById('disputeParticipationSearch');
    const participationWidget = document.querySelector('[data-searchable-select]');
    const participationMenu = document.getElementById('disputeParticipationOptions');
    const subject = document.getElementById('disputeSubject');
    const description = document.getElementById('disputeDescription');
    const setValidity = (field, valid) => {
        field?.classList.toggle('is-invalid', !valid);
        field?.setAttribute('aria-invalid', valid ? 'false' : 'true');
        return valid;
    };
    const validateOpenForm = () => {
        const participationValid = Boolean(participation?.value);
        const subjectLength = subject?.value.trim().length || 0;
        const descriptionLength = description?.value.trim().length || 0;
        return [
            setValidity(participationSearch, participationValid),
            setValidity(subject, subjectLength >= 5 && subjectLength <= 200),
            setValidity(description, descriptionLength >= 10 && descriptionLength <= 2000)
        ].every(Boolean);
    };

    openForm?.addEventListener('submit', event => {
        if (!validateOpenForm()) {
            event.preventDefault();
            openForm.querySelector('.is-invalid')?.focus();
        }
    });
    [participation, subject, description].forEach(field => {
        field?.addEventListener(field.tagName === 'SELECT' ? 'change' : 'input', () => {
            if (field.classList.contains('is-invalid')) validateOpenForm();
        });
    });

    const participationOptions = participation
        ? [...participation.options].slice(1).map(option => ({
            value: option.value,
            label: option.textContent.trim(),
            search: option.textContent.trim().toLocaleLowerCase('vi-VN')
        }))
        : [];
    const closeParticipationMenu = () => {
        participationWidget?.classList.remove('is-open');
        participationSearch?.setAttribute('aria-expanded', 'false');
    };
    const chooseParticipation = option => {
        participation.value = option.value;
        participationSearch.value = option.label;
        participationSearch.dataset.selectedLabel = option.label;
        setValidity(participationSearch, true);
        closeParticipationMenu();
    };
    const renderParticipationOptions = () => {
        if (!participationMenu) return;
        const keyword = participationSearch?.value.trim().toLocaleLowerCase('vi-VN') || '';
        const matched = participationOptions.filter(option => !keyword || option.search.includes(keyword)).slice(0, 50);
        participationMenu.innerHTML = matched.length
            ? matched.map(option => `<button type="button" role="option" data-participation-value="${escapeHtml(option.value)}">${escapeHtml(option.label)}</button>`).join('')
            : '<div class="koc-searchable-select-empty"><i class="fa-solid fa-magnifying-glass"></i>Không tìm thấy chiến dịch phù hợp</div>';
        participationWidget?.classList.add('is-open');
        participationSearch?.setAttribute('aria-expanded', 'true');
    };
    participationSearch?.addEventListener('focus', renderParticipationOptions);
    participationSearch?.addEventListener('input', () => {
        if (participationSearch.value !== participationSearch.dataset.selectedLabel) participation.value = '';
        renderParticipationOptions();
    });
    participationWidget?.querySelector('.koc-searchable-select-toggle')?.addEventListener('click', () => {
        if (participationWidget.classList.contains('is-open')) closeParticipationMenu();
        else {
            participationSearch?.focus();
            renderParticipationOptions();
        }
    });
    participationMenu?.addEventListener('click', event => {
        const button = event.target.closest('[data-participation-value]');
        if (!button) return;
        const option = participationOptions.find(item => item.value === button.dataset.participationValue);
        if (option) chooseParticipation(option);
    });
    document.addEventListener('click', event => {
        if (!participationWidget?.contains(event.target)) closeParticipationMenu();
    });
    participationSearch?.addEventListener('keydown', event => {
        if (event.key === 'Escape') closeParticipationMenu();
        if (event.key === 'Enter' && participationWidget?.classList.contains('is-open')) {
            const first = participationMenu?.querySelector('[data-participation-value]');
            if (first) {
                event.preventDefault();
                first.click();
            }
        }
    });
})();
