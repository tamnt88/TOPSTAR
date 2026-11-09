(function ($) {
    "use strict";
    const element = document.getElementById("profileQueueTable");
    if (!element || !$.fn.DataTable) return;
    const table = $(element).DataTable(TopstarLists.adapt($(element), {
        pageLength: 10,
        lengthMenu: [[10, 20, 50], ["10 dòng", "20 dòng", "50 dòng"]],
        order: [[4, "asc"]],
        autoWidth: false,
        columnDefs: [
            { targets: [0, 5], orderable: false, searchable: false },
            { targets: 0, width: "54px" },
            { targets: 2, width: "105px" },
            { targets: 4, width: "145px" },
            { targets: 5, width: "90px" }
        ],
        dom: "t<'admin-profile-queue-footer'ip>",
        language: {
            emptyTable: "Không có hồ sơ đang chờ duyệt.",
            zeroRecords: "Không tìm thấy hồ sơ phù hợp.",
            info: "Hiển thị _START_–_END_ trong _TOTAL_ hồ sơ",
            infoEmpty: "Hiển thị 0 hồ sơ",
            infoFiltered: "(lọc từ _MAX_ hồ sơ)",
            paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" }
        },
        drawCallback: function () {
            const api = this.api();
            const start = api.page.info().start;
            api.column(0, { page: "current" }).nodes().each(function (cell, index) { cell.textContent = start + index + 1; });
        }
    }));
    const applyFilters = function () {
        table.search(document.getElementById("profileQueueSearch").value.trim());
        const type = document.getElementById("profileQueueType").value;
        table.column(2).search(type ? "^" + $.fn.dataTable.util.escapeRegex(type) + "$" : "", true, false);
        table.page.len(Number(document.getElementById("profileQueueLength").value) || 10).draw();
    };
    document.getElementById("profileQueueFilter").addEventListener("click", applyFilters);
    document.getElementById("profileQueueSearch").addEventListener("input", applyFilters);
    document.getElementById("profileQueueType").addEventListener("change", applyFilters);
    document.getElementById("profileQueueLength").addEventListener("change", applyFilters);
})(jQuery);
