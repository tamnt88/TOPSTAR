(function ($) {
    'use strict';

    var tableElement = document.getElementById('brandCampaignTable');
    if (!tableElement) return;

    var statusMap = {
        Draft: ['Bản nháp', 'draft', 'fa-regular fa-pen-to-square'],
        Submitted: ['Chờ duyệt', 'review', 'fa-regular fa-clock'],
        NeedEdit: ['Cần bổ sung thông tin', 'warning', 'fa-solid fa-pen'],
        Published: ['Đang tuyển KOC', 'published', 'fa-solid fa-users'],
        InProgress: ['Đang triển khai', 'running', 'fa-solid fa-play'],
        Completed: ['Đã hoàn thành', 'completed', 'fa-solid fa-check'],
        Rejected: ['Không được phê duyệt', 'danger', 'fa-solid fa-xmark'],
        Paused: ['Tạm ngừng', 'warning', 'fa-solid fa-pause'],
        Cancelled: ['Đã hủy', 'muted', 'fa-solid fa-ban']
    };
    var dataUrl = window.location.pathname + '?handler=Data';
    var submitUrl = window.location.pathname + '?handler=Submit';
    var submitId = null;
    var submitModalElement = document.getElementById('brandCampaignSubmitModal');
    var submitModal = window.bootstrap && submitModalElement
        ? bootstrap.Modal.getOrCreateInstance(submitModalElement)
        : null;

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : String(value)).html().replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function toast(message, type) {
        if (typeof window.showToast === 'function') window.showToast(message, type);
    }

    function actionMenu(row) {
        var actions = [
            '<a class="dropdown-item" href="/Brand/CampaignWorkspace?id=' + row.id + '"><i class="fa-solid fa-table-cells-large"></i> Mở trung tâm chiến dịch</a>',
            '<a class="dropdown-item" href="/Brand/CampaignEdit?id=' + row.id + '"><i class="fa-regular fa-file-lines"></i> Xem hồ sơ chiến dịch</a>'
        ];
        if (row.canEdit) actions.push('<a class="dropdown-item" href="/Brand/CampaignEdit?id=' + row.id + '"><i class="fa-solid fa-pen"></i> Sửa chiến dịch</a>');
        if (row.canEditImages) actions.push('<button class="dropdown-item js-campaign-images" type="button" data-id="' + row.id + '"><i class="fa-solid fa-images"></i> Đổi ảnh đại diện / banner</button>');
        if (row.canShortlist) actions.push('<a class="dropdown-item" href="/Brand/Shortlist?id=' + row.id + '"><i class="fa-solid fa-user-check"></i> ' + (row.isShortlistFinalized ? 'Xem KOC đã chốt' : 'Quản lý KOC') + '</a>');
        if (row.canReviewContent) actions.push('<a class="dropdown-item" href="/Brand/Deliverables?id=' + row.id + '"><i class="fa-solid fa-list-check"></i> Duyệt nội dung</a>');
        if (row.canReviewKoc) actions.push('<a class="dropdown-item" href="/Brand/Reviews"><i class="fa-solid fa-star"></i> Đánh giá KOC</a>');
        var next = row.canEdit
            ? ['/Brand/CampaignEdit?id=', 'Tiếp tục', 'fa-solid fa-arrow-right']
            : row.canReviewContent && row.status !== 'Completed'
                ? ['/Brand/Deliverables?id=', 'Duyệt nội dung', 'fa-solid fa-list-check']
                : row.canShortlist
                    ? ['/Brand/Shortlist?id=', 'Xem KOC', 'fa-solid fa-users']
                    : ['/Brand/CampaignWorkspace?id=', 'Xem chi tiết', 'fa-regular fa-eye'];
        var submitButton = '<a class="btn btn-brand-primary btn-sm" href="' + next[0] + row.id + '"><i class="' + next[2] + '"></i> ' + next[1] + '</a>';
        return '<div class="brand-row-action-group">' + submitButton +
            '<div class="dropdown dropup brand-row-actions">' +
            '<button class="btn btn-outline-secondary btn-sm" type="button" data-bs-toggle="dropdown" data-bs-boundary="viewport" aria-expanded="false" aria-label="Mở menu thao tác"><i class="fa-solid fa-ellipsis-vertical"></i></button>' +
            '<div class="dropdown-menu dropdown-menu-end">' + actions.join('') + '</div></div></div>';
    }

    function renderCampaignCard(row) {
        var meta = statusMap[row.hasWorkStarted && row.status === 'Published' ? 'InProgress' : row.status] || ['Đang cập nhật', 'muted', 'fa-regular fa-clock'];
        var href = '/Brand/' + (row.canEdit ? 'CampaignEdit' : 'CampaignWorkspace') + '?id=' + encodeURIComponent(row.id);
        var image = row.bannerUrl || row.avatarUrl;
        var cover = image
            ? '<img class="' + (row.bannerUrl ? 'is-banner' : 'is-avatar') + '" src="' + escapeHtml(image) + '" alt="" loading="lazy">'
            : '<div class="campaign-cover-placeholder"><span class="campaign-cover-symbol"><i class="fa-solid fa-bullhorn"></i></span></div>';
        var selected = Number(row.selectedKocCount || 0), total = Number(row.desiredKocCount || 0);
        var money = function (value) { return Number(value || 0).toLocaleString('vi-VN') + ' đ'; };
        var funded = Number(row.fundedBudget || 0);
        var payment = funded > 0
            ? '<div class="campaign-project-payment"><span>Ngân sách đã thanh toán</span><strong>' + money(funded) + '</strong></div>'
            : row.status === 'Cancelled'
                ? ''
                : '<div class="campaign-project-payment is-unpaid"><span><i class="fa-regular fa-clock"></i> Chưa thanh toán</span></div>';
        return '<article class="campaign-project-card">' +
            '<a class="campaign-project-cover" href="' + href + '" aria-label="' + escapeHtml(row.title) + '">' + cover + '</a>' +
            '<div class="campaign-project-body"><span class="brand-status-pill is-' + meta[1] + '"><i class="' + meta[2] + '"></i>' + escapeHtml(meta[0]) + '</span>' +
            '<h2><a class="campaign-project-title" href="' + href + '">' + escapeHtml(row.title) + '</a></h2>' +
            '<div class="campaign-project-dates"><i class="fa-regular fa-calendar"></i> ' + escapeHtml(row.startDate || 'Chưa đặt ngày') + ' <span>→</span> ' + escapeHtml(row.endDate || 'Chưa đặt ngày') + '</div>' +
            '<dl class="campaign-project-metrics"><div><dt>Ngân sách</dt><dd>' + money(row.budgetAmount) + '</dd></div><div><dt>KOC đã chọn</dt><dd><i class="fa-solid fa-users"></i> ' + selected + '<span> / ' + total + '</span></dd></div></dl>' +
            payment +
            '<footer>' + actionMenu(row) + '</footer></div></article>';
    }

    var DataTableConstructor = window.DataTable || ($.fn && $.fn.DataTable);
    if (typeof DataTableConstructor !== 'function') {
        $(tableElement).find('tbody').html(
            '<tr><td colspan="8"><div class="brand-table-error"><i class="fa-solid fa-triangle-exclamation"></i><div><strong>Không thể khởi tạo bảng chiến dịch</strong><span>Thư viện DataTables chưa được tải. Vui lòng tải lại trang.</span></div></div></td></tr>'
        );
        toast('Không thể khởi tạo bảng chiến dịch. Vui lòng tải lại trang.', 'error');
        return;
    }

    var table = new DataTableConstructor(tableElement, TopstarLists.configure(tableElement, {
        ajax: {
            url: dataUrl,
            data: function (request) {
                request.keyword = $('#brandCampaignKeyword').val() || '';
                request.status = $('#brandCampaignStatus').val() || '';
                request.orderColumn = request.order && request.order.length ? request.order[0].column : 0;
                request.orderDirection = request.order && request.order.length ? request.order[0].dir : 'desc';
            },
            dataSrc: function (response) {
                var summary = response.summary || {};
                $('#brandCampaignDraftCount').text(summary.draft || 0);
                $('#brandCampaignReviewCount').text(summary.review || 0);
                $('#brandCampaignRunningCount').text(summary.running || 0);
                $('#brandCampaignCompletedCount').text(summary.completed || 0);
                return response.data || [];
            },
            error: function (xhr) {
                document.getElementById('brandCampaignCards').innerHTML = '<div class="campaign-project-empty">Không thể tải danh sách chiến dịch. Vui lòng tải lại trang.</div>';
                $(tableElement).find('tbody').html(
                    '<tr><td colspan="8"><div class="brand-table-error"><i class="fa-solid fa-circle-exclamation"></i><div><strong>Không thể tải dữ liệu chiến dịch</strong><span>' +
                    escapeHtml((xhr.responseJSON && xhr.responseJSON.message) || 'Vui lòng thử tải lại trang.') +
                    '</span></div></div></td></tr>'
                );
                toast((xhr.responseJSON && xhr.responseJSON.message) || 'Không thể tải danh sách chiến dịch.', 'error');
            }
        },
        drawCallback: function () {
            var rows = this.api().rows({ page: 'current' }).data().toArray();
            document.getElementById('brandCampaignCards').innerHTML = rows.length
                ? rows.map(renderCampaignCard).join('')
                : '<div class="campaign-project-empty"><i class="fa-regular fa-folder-open"></i><h2>Chưa có chiến dịch phù hợp</h2><p>Thử thay đổi bộ lọc hoặc bắt đầu một chiến dịch mới.</p><a class="btn btn-brand-primary" href="/Brand/CampaignNeeds"><i class="fa-solid fa-plus"></i> Tạo chiến dịch</a></div>';
        },
        processing: true,
        serverSide: true,
        pageLength: 10,
        lengthChange: false,
        autoWidth: false,
        order: [[3, 'desc']],
        dom: 'rt<"brand-dt-footer"ip>',
        language: {
            emptyTable: 'Chưa có chiến dịch phù hợp.',
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ chiến dịch',
            infoEmpty: 'Hiển thị 0 chiến dịch',
            processing: 'Đang tải dữ liệu...',
            paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' }
        },
        columns: [
            { data: 'rowNumber', width: '54px', className: 'text-center', orderable: false },
            {
                data: 'title',
                render: function (value, type, row) {
                    if (type !== 'display') return value;
                    var avatar = row.avatarUrl ? '<img class="brand-campaign-thumbnail" src="' + escapeHtml(row.avatarUrl) + '" alt="" loading="lazy"/>' : '<i class="fa-solid fa-bullhorn"></i>';
                    return '<div class="brand-campaign-name"><span class="brand-campaign-avatar">' + avatar + '</span><div><a class="brand-campaign-workspace-link" href="/Brand/CampaignWorkspace?id=' + row.id + '" title="Mở trung tâm chiến dịch">' + escapeHtml(value) + '</a></div></div>';
                }
            },
            {
                data: 'budgetAmount',
                render: function (value, type, row) {
                    if (type !== 'display') return value;
                    return '<strong class="brand-campaign-money">' + Number(value || 0).toLocaleString('vi-VN') + ' đ</strong><small class="brand-campaign-funded" title="Ngân sách thù lao đã ký quỹ, không gồm phí; đã trừ khoản hoàn tiền">Đã thanh toán: <b>' + Number(row.fundedBudget || 0).toLocaleString('vi-VN') + ' đ</b></small>';
                }
            },
            {
                data: 'startDate',
                render: function (_, type, row) {
                    if (type !== 'display') return (row.startDate || '') + (row.endDate || '');
                    return '<div class="brand-campaign-period"><span><i class="fa-regular fa-calendar"></i>' + escapeHtml(row.startDate || 'Chưa đặt') + '</span><i class="fa-solid fa-arrow-right-long"></i><span>' + escapeHtml(row.endDate || 'Không giới hạn') + '</span></div>';
                }
            },
            {
                data: 'selectedKocCount',
                className: 'text-center',
                render: function (value, type, row) {
                    if (type !== 'display') return value;
                    return '<span class="brand-count-pill" title="KOC đã được Brand chọn / tổng số cần tuyển"><i class="fa-solid fa-users"></i>' + Number(value || 0) + '/' + Number(row.desiredKocCount || 0) + '</span>';
                }
            },
            {
                data: 'status',
                render: function (value, type, row) {
                    if (type !== 'display') return value;
                    if (row.hasWorkStarted && value === 'Published') value = 'InProgress';
                    var meta = statusMap[value] || [value, 'muted', 'fa-solid fa-circle'];
                    return '<span class="brand-status-pill is-' + meta[1] + '"><i class="' + meta[2] + '"></i>' + escapeHtml(meta[0]) + '</span><small class="brand-campaign-next-hint">' + escapeHtml(({ Draft: 'Tiếp tục hoàn thiện chiến dịch', NeedEdit: 'Cập nhật theo góp ý của TopStar', Submitted: 'TopStar đang xem xét nội dung', Published: 'Xem danh sách và tiến độ nhận việc', InProgress: 'Theo dõi và duyệt nội dung KOC', Completed: 'Xem kết quả hợp tác' })[value] || '') + '</small>';
                }
            },
            {
                data: null, orderable: false, searchable: false,
                render: function (_, type, row) {
                    var stage;
                    if (row.status === 'Completed') stage = ['Đã hoàn thành', 'completed', 'fa-solid fa-circle-check', 'Chiến dịch đã kết thúc'];
                    else if (row.status === 'Paused') stage = ['Tạm ngừng', 'warning', 'fa-solid fa-pause', 'Ký quỹ và danh sách KOC được giữ nguyên'];
                    else if (row.status === 'Cancelled') stage = ['Đã hủy', 'danger', 'fa-solid fa-ban', 'Chiến dịch đã được hủy'];
                    else if (['Draft', 'NeedEdit', 'Submitted', 'Rejected'].includes(row.status)) stage = ['Chưa triển khai', 'muted', 'fa-regular fa-clock', 'Chờ hoàn tất phê duyệt hồ sơ'];
                    else if (row.hasWorkStarted) stage = ['Đang thực hiện', 'running', 'fa-solid fa-play', 'Đã mở công việc cho KOC'];
                    else if (Number(row.fundedBudget) > 0) stage = ['Đã ký quỹ', 'published', 'fa-solid fa-shield-halved', 'Chưa mở công việc cho KOC'];
                    else if (Number(row.selectedKocCount) > 0) stage = ['Đang chuẩn bị', 'warning', 'fa-solid fa-list-check', 'Đã chọn KOC, chưa ghi nhận ký quỹ'];
                    else stage = ['Đang tuyển chọn', 'muted', 'fa-solid fa-users', 'Chưa chọn KOC tham gia'];
                    if (type !== 'display') return stage[0];
                    return '<div class="brand-campaign-stage"><span class="brand-status-pill is-' + stage[1] + '"><i class="' + stage[2] + '"></i>' + stage[0] + '</span><small>' + stage[3] + '</small></div>';
                }
            },
            { data: null, orderable: false, searchable: false, width: '80px', className: 'text-center', render: function (_, __, row) { return actionMenu(row); } }
        ],
        columnDefs: [{ targets: [0, 6], visible: false }, { targets: '_all', defaultContent: '' }]
    }, { keyword: '#brandCampaignKeyword', status: '#brandCampaignStatus' }, '#brandCampaignPageSize'));

    var imageForm = document.getElementById('campaignImagesForm');
    var imageModalElement = document.getElementById('campaignImagesModal');
    var imageModal = imageModalElement && window.bootstrap ? bootstrap.Modal.getOrCreateInstance(imageModalElement) : null;
    var previewUrls = {};
    function releasePreviews() {
        Object.keys(previewUrls).forEach(function (key) { URL.revokeObjectURL(previewUrls[key]); });
        previewUrls = {};
    }
    $(document).on('click', '.js-campaign-images', function () {
        var id = this.dataset.id;
        var row = table.rows().data().toArray().find(function (x) { return x.id === id; });
        if (!row || !row.canEditImages || !imageForm || !imageModal) return;
        releasePreviews();
        imageForm.reset();
        imageForm.elements.id.value = id;
        imageForm.querySelector('[data-media-campaign-name]').textContent = row.title;
        imageForm.querySelector('[data-media-error]').textContent = '';
        [['avatarFile', row.avatarUrl], ['bannerFile', row.bannerUrl]].forEach(function (entry) {
            var img = imageForm.querySelector('[data-media-preview="' + entry[0] + '"]');
            img.hidden = !entry[1];
            if (entry[1]) img.src = entry[1]; else img.removeAttribute('src');
        });
        imageModal.show();
    });
    imageModalElement?.addEventListener('hidden.bs.modal', releasePreviews);
    imageForm?.addEventListener('change', function (event) {
        var input = event.target;
        if (input.type !== 'file') return;
        var file = input.files?.[0];
        if (!file) return;
        var error = imageForm.querySelector('[data-media-error]');
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
            input.value = ''; error.textContent = 'Vui lòng chọn ảnh JPG, PNG hoặc WEBP tối đa 5 MB.'; return;
        }
        error.textContent = '';
        if (previewUrls[input.name]) URL.revokeObjectURL(previewUrls[input.name]);
        previewUrls[input.name] = URL.createObjectURL(file);
        var img = imageForm.querySelector('[data-media-preview="' + input.name + '"]');
        img.src = previewUrls[input.name]; img.hidden = false;
    });
    imageForm?.addEventListener('submit', function (event) {
        event.preventDefault();
        var error = imageForm.querySelector('[data-media-error]');
        if (!imageForm.elements.avatarFile.files.length && !imageForm.elements.bannerFile.files.length) {
            error.textContent = 'Vui lòng chọn ít nhất một ảnh mới.'; return;
        }
        var button = imageForm.querySelector('[type="submit"]');
        if (button.disabled) return;
        button.disabled = true;
        $.ajax({ url: imageForm.action, method: 'POST', data: new FormData(imageForm), processData: false, contentType: false })
            .done(function (result) { imageModal.hide(); table.ajax.reload(null, false); toast(result.message, 'success'); })
            .fail(function (xhr) { error.textContent = xhr.responseJSON?.message || 'Không thể cập nhật hình ảnh. Vui lòng thử lại.'; })
            .always(function () { button.disabled = false; });
    });

    $('#brandCampaignFilterButton').on('click', function () {
        table.ajax.reload();
    });
    $('#brandCampaignKeyword').on('keydown', function (event) {
        if (event.key === 'Enter') { event.preventDefault(); $('#brandCampaignFilterButton').trigger('click'); }
    });
    $('#brandCampaignStatus').on('change', function () { table.ajax.reload(); });
    $('#brandCampaignPageSize').on('change', function () { table.page.len(Number(this.value)).draw(); });

    $(document).on('click', '.js-submit-campaign', function () {
        submitId = $(this).data('id');
        $('#brandCampaignSubmitName').text($(this).data('name') || '');
        if (submitModal) submitModal.show();
    });

    $('#brandCampaignSubmitConfirm').on('click', function () {
        if (!submitId) return;
        var button = $(this);
        button.prop('disabled', true);
        $.ajax({
            url: submitUrl,
            method: 'POST',
            data: {
                id: submitId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').first().val()
            }
        }).done(function (response) {
            if (submitModal) submitModal.hide();
            toast(response.message || 'Đã gửi chiến dịch để duyệt.', 'success');
            table.ajax.reload(null, false);
        }).fail(function (xhr) {
            toast((xhr.responseJSON && xhr.responseJSON.message) || 'Không thể gửi duyệt chiến dịch.', 'error');
        }).always(function () { button.prop('disabled', false); });
    });
})(jQuery);

