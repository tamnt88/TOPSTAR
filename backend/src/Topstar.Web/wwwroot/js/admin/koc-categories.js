(() => {
    'use strict';
    const stateElement = document.getElementById('categoryStateModal');
    if (!stateElement || !window.jQuery || !jQuery.fn.DataTable) return;

    const stateModal = bootstrap.Modal.getOrCreateInstance(stateElement);
    const stateForm = document.getElementById('categoryStateForm');
    const escape = value => $('<div>').text(value ?? '').html();
    const token = form => form.querySelector('input[name="__RequestVerificationToken"]').value;
    let rows = [];

    const table = $('#kocCategoryTable').DataTable(TopstarLists.adapt($('#kocCategoryTable'), {
        ajax: { url: '?handler=Data', dataSrc: json => { rows = json.data || []; updateSummary(); return rows; } },
        pageLength: 10,
        lengthMenu: [[10, 20, 50], [10, 20, 50]],
        ordering: false,
        autoWidth: false,
        dom: "t<'category-table-footer'ip>",
        language: { info: 'Hiển thị _START_–_END_ trong _TOTAL_ lĩnh vực', infoEmpty: 'Hiển thị 0 lĩnh vực', infoFiltered: '(lọc từ _MAX_ lĩnh vực)', zeroRecords: 'Không tìm thấy lĩnh vực phù hợp', emptyTable: 'Chưa có lĩnh vực nội dung', paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' } },
        columnDefs: [
            { targets: [0, 3], className: 'dt-body-center dt-head-center' },
            { targets: 4, className: 'dt-body-center dt-head-center' },
            { targets: 5, className: 'dt-body-center dt-head-center', orderable: false, searchable: false }
        ],
        columns: [
            { data: 'sortOrder', width: '90px' },
            { data: 'name', render: value => `<strong class="category-name">${escape(value)}</strong>` },
            { data: 'code', render: value => `<code>${escape(value)}</code>` },
            { data: 'usageCount', render: value => `<span class="usage-count">${Number(value).toLocaleString('vi-VN')}</span>` },
            { data: 'isActive', render: value => `<span class="status-tag ${value ? 'is-active' : 'is-inactive'}">${value ? 'Đang hiển thị' : 'Tạm ẩn'}</span>` },
            { data: null, orderable: false, searchable: false, width: '154px', render: row => `<div class="category-actions"><a class="icon-btn" href="/Admin/Settings/KocCategoryEdit?id=${encodeURIComponent(row.id)}" aria-label="Sửa ${escape(row.name)}" title="Sửa"><i class="fa-solid fa-pen"></i></a><button type="button" class="icon-btn ${row.isActive ? 'is-hide' : 'is-show'}" data-state="${row.id}" aria-label="${row.isActive ? 'Tạm ẩn' : 'Hiển thị'} ${escape(row.name)}" title="${row.isActive ? 'Tạm ẩn' : 'Hiển thị'}"><i class="fa-solid ${row.isActive ? 'fa-eye-slash' : 'fa-eye'}"></i></button><a class="icon-btn is-delete" href="/Admin/Settings/KocCategoryDelete?id=${encodeURIComponent(row.id)}" aria-label="Xóa ${escape(row.name)}" title="Xóa"><i class="fa-solid fa-trash-can"></i></a></div>` }
        ]
    }));

    const updateSummary = () => {
        document.querySelector('[data-total]').textContent = rows.length;
        document.querySelector('[data-active]').textContent = rows.filter(x => x.isActive).length;
        document.querySelector('[data-inactive]').textContent = rows.filter(x => !x.isActive).length;
    };
    const applyFilters = () => {
        table.search(document.getElementById('categorySearch').value.trim());
        table.column(4).search(document.getElementById('categoryStatus').value);
        table.page.len(Number(document.getElementById('categoryLength').value) || 10).draw();
    };
    document.getElementById('categoryApplyFilter').addEventListener('click', applyFilters);
    document.getElementById('categorySearch').addEventListener('keydown', event => {
        if (event.key === 'Enter') { event.preventDefault(); applyFilters(); }
    });

    const clearErrors = form => {
        form.querySelectorAll('.is-invalid').forEach(x => { x.classList.remove('is-invalid'); x.removeAttribute('aria-invalid'); });
        form.querySelectorAll('.invalid-feedback').forEach(x => { x.textContent = ''; x.style.display = ''; });
        const summary = form.querySelector('.category-form-summary'); summary.hidden = true; summary.textContent = '';
    };
    const fieldError = (form, name, message) => {
        const field = form.elements[name]; if (!field) return;
        field.classList.add('is-invalid'); field.setAttribute('aria-invalid', 'true');
        const feedback = form.querySelector(`[data-error-for="${name}"]`); if (feedback) { feedback.textContent = message; feedback.style.display = 'block'; }
    };
    const summaryError = (form, message) => { const summary = form.querySelector('.category-form-summary'); summary.textContent = message; summary.hidden = false; };
    const toast = (message, success) => { const box = document.querySelector('.category-toast'); box.textContent = message; box.className = `category-toast ${success ? 'is-success' : 'is-error'}`; box.hidden = false; window.setTimeout(() => box.hidden = true, 4200); };
    const post = async (handler, form) => {
        const response = await fetch(`?handler=${handler}`, { method: 'POST', headers: { 'RequestVerificationToken': token(form) }, body: new FormData(form) });
        const result = await response.json().catch(() => ({ success: false, message: 'Phản hồi máy chủ không hợp lệ.' }));
        if (!response.ok || !result.success) throw result;
        return result;
    };

    $('#kocCategoryTable').on('click', '[data-state]', function () { const row = rows.find(x => x.id === this.dataset.state); if (!row) return; const next = !row.isActive; stateForm.reset(); stateForm.elements.id.value = row.id; stateForm.elements.isActive.value = String(next); clearErrors(stateForm); document.getElementById('categoryStateTitle').textContent = next ? 'Hiển thị lại lĩnh vực' : 'Tạm ẩn lĩnh vực'; stateForm.querySelector('.category-state-copy').textContent = next ? `“${row.name}” sẽ xuất hiện lại trong lựa chọn của KOC và Brand.` : `“${row.name}” sẽ không còn xuất hiện trong lựa chọn mới. ${row.usageCount} liên kết lịch sử vẫn được giữ nguyên.`; stateModal.show(); });

    stateForm.addEventListener('submit', async event => { event.preventDefault(); clearErrors(stateForm); const button = stateForm.querySelector('[type="submit"]'); button.disabled = true; try { const result = await post('SetActive', stateForm); stateModal.hide(); toast(result.message, true); table.ajax.reload(null, false); } catch (error) { summaryError(stateForm, error.message || 'Không thể cập nhật trạng thái.'); } finally { button.disabled = false; } });
    [stateForm].forEach(form => form.addEventListener('input', event => { if (event.target.classList.contains('is-invalid')) { event.target.classList.remove('is-invalid'); event.target.removeAttribute('aria-invalid'); const feedback = form.querySelector(`[data-error-for="${event.target.name}"]`); if (feedback) feedback.textContent = ''; } }));
})();
