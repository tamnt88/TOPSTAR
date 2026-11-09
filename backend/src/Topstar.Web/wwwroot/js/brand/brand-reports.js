(function () {
    'use strict';
    var form = document.querySelector('.js-brand-report-sync');
    if (!form) return;
    var select = form.querySelector('select[name="CampaignId"]');
    if (!select) return;
    var selectHost = select.closest('.brand-report-select');
    var picker = document.createElement('div');
    picker.className = 'brand-search-select';
    picker.innerHTML = '<button type="button" class="brand-search-select-control" aria-expanded="false"><span></span><i class="fa-solid fa-chevron-down"></i></button><div class="brand-search-select-panel"><div class="brand-search-select-input"><i class="fa-solid fa-magnifying-glass"></i><input type="search" autocomplete="off" placeholder="Tìm tên chiến dịch"></div><div class="brand-search-select-options"></div><div class="brand-search-select-empty">Không tìm thấy chiến dịch phù hợp.</div></div>';
    select.classList.add('brand-search-select-native');
    selectHost.appendChild(picker);
    var control = picker.querySelector('.brand-search-select-control');
    var controlText = control.querySelector('span');
    var search = picker.querySelector('input');
    var optionsHost = picker.querySelector('.brand-search-select-options');
    var empty = picker.querySelector('.brand-search-select-empty');

    function updateControl() {
        var option = select.options[select.selectedIndex];
        controlText.textContent = select.value && option ? option.text : (select.dataset.placeholder || 'Tìm và chọn chiến dịch');
        control.classList.toggle('has-value', !!select.value);
    }
    function renderOptions() {
        var query = search.value.trim().toLocaleLowerCase('vi');
        optionsHost.innerHTML = '';
        var count = 0;
        Array.from(select.options).forEach(function (option) {
            if (!option.value || (query && !option.text.toLocaleLowerCase('vi').includes(query))) return;
            count++;
            var button = document.createElement('button');
            button.type = 'button';
            button.dataset.value = option.value;
            button.className = option.selected ? 'is-selected' : '';
            button.innerHTML = '<span></span><i class="fa-solid fa-check"></i>';
            button.querySelector('span').textContent = option.text;
            optionsHost.appendChild(button);
        });
        empty.hidden = count > 0;
    }
    function openPicker() { picker.classList.add('is-open'); control.setAttribute('aria-expanded', 'true'); search.value = ''; renderOptions(); setTimeout(function () { search.focus(); }, 0); }
    function closePicker() { picker.classList.remove('is-open'); control.setAttribute('aria-expanded', 'false'); }
    control.addEventListener('click', function () { picker.classList.contains('is-open') ? closePicker() : openPicker(); });
    search.addEventListener('input', renderOptions);
    optionsHost.addEventListener('click', function (event) {
        var button = event.target.closest('button[data-value]');
        if (!button) return;
        select.value = button.dataset.value;
        select.dispatchEvent(new Event('change', { bubbles: true }));
        updateControl();
        closePicker();
    });
    const outsideClick = event => { if (!picker.contains(event.target)) closePicker(); };
    document.addEventListener('click', outsideClick);
    if(window.topstarWorkspaceUrl) document.addEventListener('topstar:workspace-dispose',()=>document.removeEventListener('click',outsideClick),{once:true});
    updateControl();
    form.addEventListener('submit', function (event) {
        var valid = select && select.value;
        form.classList.toggle('has-error', !valid);
        if (!valid) { event.preventDefault(); select.classList.add('is-invalid'); control.classList.add('is-invalid'); select.setAttribute('aria-invalid', 'true'); control.focus(); }
    });
    select.addEventListener('change', function () { if (select.value) { form.classList.remove('has-error'); select.classList.remove('is-invalid'); control.classList.remove('is-invalid'); select.setAttribute('aria-invalid', 'false'); } });
})();
$(function () {
    const table = document.getElementById("brandReportKocTable");
    if (!table || !$.fn.DataTable) return;
    $(table).DataTable(TopstarLists.adapt($(table), {
        pageLength: 10,
        lengthMenu: [[5, 10, 20, 50], ["5 dòng", "10 dòng", "20 dòng", "50 dòng"]],
        autoWidth: false,
        order: [[2, "desc"]],
        columnDefs: [{ targets: 0, orderable: false, searchable: false }],
        dom: '<"brand-report-dt-toolbar"f l>t<"brand-report-dt-footer"ip>',
        language: { search: "", searchPlaceholder: "Tìm theo tên KOC", lengthMenu: "Hiển thị _MENU_", emptyTable: "Chưa có dữ liệu hiệu quả KOC.", zeroRecords: "Không tìm thấy KOC phù hợp.", info: "Hiển thị _START_–_END_ trong _TOTAL_ KOC", infoEmpty: "Hiển thị 0 KOC", infoFiltered: "(lọc từ _MAX_ KOC)", paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" } },
        drawCallback: function () { const api=this.api(),start=api.page.info().start;api.column(0,{page:"current"}).nodes().each(function(cell,index){cell.innerHTML=start+index+1;}); }
    }));
});
