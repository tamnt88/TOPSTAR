(function ($) {
    'use strict';
    const page = document.querySelector('.ops-exception-page');
    if (!page) return;
    const directSections = Array.from(page.querySelectorAll(':scope > section'));
    const assignments = ['stats', 'queue', 'sla', 'submission', 'review', 'dispute', 'shipment', 'booking'];
    assignments.forEach((name, index) => { if (directSections[index]) directSections[index].dataset.opsPanel = name; });
    const show = name => {
        page.querySelectorAll('[data-ops-tab]').forEach(x => x.classList.toggle('is-active', x.dataset.opsTab === name));
        page.querySelectorAll('[data-ops-panel]').forEach(x => {
            if (x.dataset.opsPanel !== 'stats') x.hidden = x.dataset.opsPanel !== name;
        });
        if (name !== 'queue') document.querySelector(`[data-ops-panel="${name}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    };
    page.querySelectorAll('[data-ops-tab]').forEach(x => x.addEventListener('click', () => show(x.dataset.opsTab)));
    page.querySelectorAll('[data-open-ops-tab]').forEach(x => x.addEventListener('click', () => show(x.dataset.openOpsTab)));
    show('queue');

    const element = document.getElementById('operationsQueueTable');
    if (!element || !$.fn.DataTable) return;
    const table = $(element).DataTable(TopstarLists.adapt($(element), {
        pageLength: 10, lengthChange: false, autoWidth: false, order: [[4, 'desc']],
        columnDefs: [{ targets: [0, 5], orderable: false, searchable: false }, { targets: 0, width: '52px' }, { targets: 5, width: '90px' }],
        dom: "t<'ops-queue-footer'ip>",
        language: { emptyTable: 'Không có công việc cần xử lý.', zeroRecords: 'Không tìm thấy công việc phù hợp.', info: 'Hiển thị _START_–_END_ trong _TOTAL_ công việc', infoEmpty: 'Hiển thị 0 công việc', infoFiltered: '(lọc từ _MAX_ công việc)', paginate: { first: '«', previous: '‹', next: '›', last: '»' } },
        drawCallback: function () { const api = this.api(), start = api.page.info().start; api.column(0, { page: 'current' }).nodes().each((cell, i) => cell.textContent = start + i + 1); }
    }));
    const apply = () => {
        const type = $('#opsQueueType').val();
        table.search($('#opsQueueSearch').val().trim()).column(2).search(type ? '^' + $.fn.dataTable.util.escapeRegex(type) + '$' : '', true, false).page.len(Number($('#opsQueueLength').val()) || 10).draw();
    };
    $('#opsQueueFilter').on('click', apply); $('#opsQueueSearch').on('input', apply); $('#opsQueueType,#opsQueueLength').on('change', apply);
})(jQuery);
