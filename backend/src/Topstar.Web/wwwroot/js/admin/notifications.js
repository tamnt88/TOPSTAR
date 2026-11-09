(function ($) {
    'use strict';
    const element = document.getElementById('adminNotificationTable');
    if (!element || !$.fn.DataTable) return;
    const table = $(element).DataTable(TopstarLists.adapt($(element), {
        pageLength: 10,
        lengthChange: false,
        autoWidth: false,
        order: [[3, 'desc']],
        columnDefs: [{ targets: [0, 5], orderable: false, searchable: false }, { targets: 0, width: '54px' }, { targets: 5, width: '132px' }],
        dom: "t<'admin-notification-footer'ip>",
        language: {
            emptyTable: 'Chưa có thông báo theo bộ lọc này.',
            zeroRecords: 'Không tìm thấy thông báo phù hợp.',
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ thông báo',
            infoEmpty: 'Hiển thị 0 thông báo',
            infoFiltered: '(lọc từ _MAX_ thông báo)',
            paginate: { first: '«', previous: '‹', next: '›', last: '»' }
        },
        drawCallback: function () {
            const api = this.api(), start = api.page.info().start;
            api.column(0, { page: 'current' }).nodes().each((cell, index) => cell.textContent = start + index + 1);
        }
    }));
    const apply = () => {
        const type = $('#notificationType').val();
        table.search($('#notificationSearch').val().trim());
        table.column(2).search(type ? '^' + $.fn.dataTable.util.escapeRegex(type) + '$' : '', true, false);
        table.page.len(Number($('#notificationLength').val()) || 10).draw();
    };
    $('#notificationApply').on('click', apply);
    $('#notificationSearch').on('input', apply);
    $('#notificationType,#notificationLength').on('change', apply);
})(jQuery);
