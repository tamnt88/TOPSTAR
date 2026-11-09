(function ($) {
    "use strict";
    if (!$.fn.DataTable) return;
    function create(selector, label, dateColumn) {
        const table = document.querySelector(selector);
        if (!table) return;
        $(table).DataTable(TopstarLists.adapt($(table), {
            pageLength: 10,
            lengthMenu: [[5, 10, 20, 50], ["5 dòng", "10 dòng", "20 dòng", "50 dòng"]],
            autoWidth: false,
            order: [[dateColumn, "desc"]],
            columnDefs: [{ targets: -1, orderable: false, searchable: false }],
            dom: '<"brand-billing-dt-toolbar"f l>t<"brand-billing-dt-footer"ip>',
            language: {
                search: "", searchPlaceholder: "Tìm " + label.toLowerCase(),
                lengthMenu: "Hiển thị _MENU_", emptyTable: "Chưa có " + label.toLowerCase() + ".",
                zeroRecords: "Không tìm thấy dữ liệu phù hợp.",
                info: "Hiển thị _START_–_END_ trong _TOTAL_ dòng", infoEmpty: "Hiển thị 0 dòng",
                infoFiltered: "(lọc từ _MAX_ dòng)", paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" }
            },
            initComplete: function () {
                const wrapper = table.closest(".dt-container");
                const input = wrapper && wrapper.querySelector(".dt-search input");
                if (input) input.setAttribute("aria-label", "Tìm " + label.toLowerCase());
            }
        }));
    }
    create("#brandPackagePurchaseTable", "đơn mua gói", 3);
    create("#brandServiceFeeTable", "phí dịch vụ", 3);
})(jQuery);
