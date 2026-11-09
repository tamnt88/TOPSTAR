(() => {
    'use strict';
    if (!window.jQuery?.fn?.DataTable || !document.getElementById('kocCategoryHistoryTable')) return;
    const table = jQuery('#kocCategoryHistoryTable').DataTable(TopstarLists.adapt(jQuery('#kocCategoryHistoryTable'), {
        pageLength: 10,
        lengthMenu: [10, 20, 50],
        ordering: false,
        autoWidth: false,
        dom: "t<'category-history-footer'ip>",
        language: {
            info: 'Hiển thị _START_–_END_ trong _TOTAL_ thay đổi',
            infoEmpty: 'Chưa có lịch sử thay đổi',
            infoFiltered: '(lọc từ _MAX_ thay đổi)',
            zeroRecords: 'Không tìm thấy lịch sử phù hợp',
            emptyTable: 'Chưa có lịch sử thay đổi',
            paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' }
        }
    }));
    const search = document.getElementById('historySearch');
    const action = document.getElementById('historyAction');
    const length = document.getElementById('historyLength');
    const apply = () => {
        table.search(search.value.trim());
        table.column(2).search(action.value ? `^${jQuery.fn.dataTable.util.escapeRegex(action.value)}$` : '', true, false);
        table.page.len(Number(length.value) || 10).draw();
    };
    document.getElementById('historyApplyFilter').addEventListener('click', apply);
    search.addEventListener('keydown', event => {
        if (event.key === 'Enter') { event.preventDefault(); apply(); }
    });
})();
