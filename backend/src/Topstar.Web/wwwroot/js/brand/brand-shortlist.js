(function ($) {
    'use strict';

    var element = document.getElementById('brandShortlistTable');
    if (!element) return;

    var statusMap = {
        Published: ['Đang tuyển KOC', 'published', 'fa-solid fa-users'],
        InProgress: ['Đang thực hiện', 'running', 'fa-solid fa-play'],
        Completed: ['Đã hoàn thành', 'completed', 'fa-solid fa-check']
    };

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : String(value)).html();
    }

    function showError(message) {
        $(element).find('tbody').html(
            '<tr><td colspan="6"><div class="brand-table-error"><i class="fa-solid fa-circle-exclamation"></i><div><strong>Không thể tải danh sách tuyển chọn</strong><span>' +
            escapeHtml(message) + '</span></div></div></td></tr>'
        );
        if (typeof window.showToast === 'function') window.showToast(message, 'error');
    }

    var Constructor = window.DataTable || ($.fn && $.fn.DataTable);
    if (typeof Constructor !== 'function') {
        showError('Thư viện danh sách chưa được tải. Vui lòng tải lại trang.');
        return;
    }

    var table = new Constructor(element, TopstarLists.adapt(element, {
        ajax: {
            url: window.location.pathname + '?handler=Data',
            data: function (request) {
                request.keyword = $('#shortlistKeyword').val() || '';
                request.status = $('#shortlistStatus').val() || '';
                request.orderColumn = request.order && request.order.length ? request.order[0].column : 0;
                request.orderDirection = request.order && request.order.length ? request.order[0].dir : 'desc';
            },
            dataSrc: function (response) {
                var summary = response.summary || {};
                $('#shortlistTotalCount').text(summary.total || 0);
                $('#shortlistRecruitingCount').text(summary.recruiting || 0);
                $('#shortlistRunningCount').text(summary.running || 0);
                $('#shortlistCompletedCount').text(summary.completed || 0);
                return response.data || [];
            },
            error: function (xhr) {
                showError((xhr.responseJSON && xhr.responseJSON.message) || 'Vui lòng thử tải lại trang.');
            }
        },
        processing: true,
        serverSide: true,
        pageLength: 10,
        lengthChange: false,
        autoWidth: false,
        order: [[2, 'desc']],
        dom: 'rt<"brand-dt-footer"ip>',
        language: {
            emptyTable: 'Chưa có chiến dịch cần tuyển chọn KOC.',
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ chiến dịch',
            infoEmpty: 'Hiển thị 0 chiến dịch',
            processing: 'Đang tải dữ liệu...',
            paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' }
        },
        columns: [
            { data: 'rowNumber', width: '54px', className: 'text-center', orderable: false },
            {
                data: 'title',
                render: function (value, type) {
                    if (type !== 'display') return value;
                    return '<div class="brand-campaign-name"><span class="brand-campaign-avatar"><i class="fa-solid fa-bullhorn"></i></span><div><strong>' +
                        escapeHtml(value) + '</strong><small>Chiến dịch tuyển chọn KOC</small></div></div>';
                }
            },
            {
                data: 'startDate',
                render: function (_, type, row) {
                    if (type !== 'display') return (row.startDate || '') + (row.endDate || '');
                    return '<div class="brand-campaign-period"><span><i class="fa-regular fa-calendar"></i>' +
                        escapeHtml(row.startDate || 'Chưa đặt') +
                        '</span><i class="fa-solid fa-arrow-right-long"></i><span>' +
                        escapeHtml(row.endDate || 'Không giới hạn') + '</span></div>';
                }
            },
            {
                data: 'applicationCount',
                className: 'text-center',
                render: function (value, type) {
                    if (type !== 'display') return value;
                    return '<span class="brand-count-pill"><i class="fa-solid fa-users"></i>' + Number(value || 0) + '</span>';
                }
            },
            {
                data: 'status',
                render: function (value, type) {
                    if (type !== 'display') return value;
                    var meta = statusMap[value] || [value, 'muted', 'fa-solid fa-circle'];
                    return '<span class="brand-status-pill is-' + meta[1] + '"><i class="' + meta[2] + '"></i>' + escapeHtml(meta[0]) + '</span>';
                }
            },
            {
                data: 'id',
                orderable: false,
                searchable: false,
                className: 'text-center',
                width: '150px',
                render: function (value) {
                    return '<a class="btn btn-brand-primary btn-sm text-nowrap" href="/Brand/Shortlist?id=' +
                        encodeURIComponent(value) + '"><i class="fa-solid fa-users-viewfinder"></i> Xem danh sách</a>';
                }
            }
        ],
        columnDefs: [{ targets: '_all', defaultContent: '' }]
    }));

    $('#shortlistFilterButton').on('click', function () { table.ajax.reload(); });
    $('#shortlistKeyword').on('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            table.ajax.reload();
        }
    });
    $('#shortlistStatus').on('change', function () { table.ajax.reload(); });
    $('#shortlistPageSize').on('change', function () { table.page.len(Number(this.value)).draw(); });
})(jQuery);
