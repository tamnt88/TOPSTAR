(function () {
    'use strict';

    function setError(form, input, message) {
        input.classList.add('is-invalid');
        input.setAttribute('aria-invalid', 'true');
        var error = form.querySelector('[data-error-for="' + input.name + '"]');
        if (error) error.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i><span>' + message + '</span>';
    }

    function clearError(form, input) {
        input.classList.remove('is-invalid');
        input.setAttribute('aria-invalid', 'false');
        var error = form.querySelector('[data-error-for="' + input.name + '"]');
        if (error) error.textContent = '';
    }

    function validate(form) {
        var firstInvalid = null;
        var start = form.querySelector('[name="startsAtLocal"]');
        var end = form.querySelector('[name="endsAtLocal"]');
        var note = form.querySelector('[name="note"]');
        var reason = form.querySelector('[name="reason"]');

        [start, end, note, reason].forEach(function (input) { if (input) clearError(form, input); });

        if (start && !start.value) { setError(form, start, 'Vui lòng chọn thời gian bắt đầu.'); firstInvalid = firstInvalid || start; }
        if (end && !end.value) { setError(form, end, 'Vui lòng chọn thời gian kết thúc.'); firstInvalid = firstInvalid || end; }
        if (start && end && start.value && end.value && new Date(end.value) <= new Date(start.value)) {
            setError(form, end, 'Thời gian kết thúc phải sau thời gian bắt đầu.');
            firstInvalid = firstInvalid || end;
        }
        if (note && (note.value.trim().length < 10 || note.value.trim().length > 1000)) {
            setError(form, note, 'Vui lòng nhập lý do đổi lịch từ 10 đến 1.000 ký tự.');
            firstInvalid = firstInvalid || note;
        }
        if (reason && (reason.value.trim().length < 10 || reason.value.trim().length > 1000)) {
            setError(form, reason, 'Vui lòng nhập lý do hủy lịch từ 10 đến 1.000 ký tự.');
            firstInvalid = firstInvalid || reason;
        }
        if (firstInvalid) firstInvalid.focus();
        return !firstInvalid;
    }

    document.querySelectorAll('.js-brand-booking-form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!validate(form)) event.preventDefault();
        });
        form.querySelectorAll('input, textarea').forEach(function (input) {
            input.addEventListener('input', function () {
                if (input.classList.contains('is-invalid')) validate(form);
            });
            input.addEventListener('change', function () {
                if (input.classList.contains('is-invalid')) validate(form);
            });
        });
    });
})();
$(function () {
    const table = document.getElementById("brandBookingTable");
    if (!table || !$.fn.DataTable) return;
    $(table).DataTable(TopstarLists.adapt($(table), {
        pageLength: 10,
        lengthMenu: [[5, 10, 20, 50], ["5 dòng", "10 dòng", "20 dòng", "50 dòng"]],
        ordering: false,
        autoWidth: false,
        dom: '<"brand-booking-dt-toolbar"f l>t<"brand-booking-dt-footer"ip>',
        language: { search: "", searchPlaceholder: "Tìm theo KOC hoặc chiến dịch", lengthMenu: "Hiển thị _MENU_", emptyTable: "Chưa có lịch làm việc.", zeroRecords: "Không tìm thấy lịch phù hợp.", info: "Hiển thị _START_–_END_ trong _TOTAL_ lịch", infoEmpty: "Hiển thị 0 lịch", infoFiltered: "(lọc từ _MAX_ lịch)", paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" } }
    }));
});
