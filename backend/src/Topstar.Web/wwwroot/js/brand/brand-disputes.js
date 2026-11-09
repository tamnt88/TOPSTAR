(function () {
    'use strict';
    var form = document.querySelector('.js-brand-dispute-form');
    if (!form) return;
    var participation = form.elements.participationId;
    var picker = document.createElement('div');
    picker.className = 'brand-search-select';
    picker.innerHTML = '<button type="button" class="brand-search-select-control" aria-expanded="false"><span>Tìm và chọn chiến dịch hoặc KOC</span><i class="fa-solid fa-chevron-down"></i></button><div class="brand-search-select-panel"><div class="brand-search-select-input"><i class="fa-solid fa-magnifying-glass"></i><input type="search" autocomplete="off" placeholder="Tìm tên chiến dịch hoặc KOC"></div><div class="brand-search-select-options"></div><div class="brand-search-select-empty">Không tìm thấy chiến dịch hoặc KOC phù hợp.</div></div>';
    participation.classList.add('brand-search-select-native');
    participation.insertAdjacentElement('afterend', picker);
    var control = picker.querySelector('.brand-search-select-control');
    var controlText = control.querySelector('span');
    var search = picker.querySelector('input');
    var optionsHost = picker.querySelector('.brand-search-select-options');
    var empty = picker.querySelector('.brand-search-select-empty');
    function render() {
        var query = search.value.trim().toLocaleLowerCase('vi');
        optionsHost.innerHTML = '';
        var count = 0;
        Array.from(participation.options).forEach(function (option) {
            if (!option.value || (query && !option.text.toLocaleLowerCase('vi').includes(query))) return;
            count++;
            var button = document.createElement('button');
            button.type = 'button'; button.dataset.value = option.value; button.className = option.selected ? 'is-selected' : '';
            button.innerHTML = '<span></span><i class="fa-solid fa-check"></i>';
            button.querySelector('span').textContent = option.text;
            optionsHost.appendChild(button);
        });
        empty.hidden = count > 0;
    }
    function update() {
        var option = participation.options[participation.selectedIndex];
        controlText.textContent = participation.value && option ? option.text : 'Tìm và chọn chiến dịch hoặc KOC';
        control.classList.toggle('has-value', !!participation.value);
    }
    control.addEventListener('click', function () {
        picker.classList.toggle('is-open');
        control.setAttribute('aria-expanded', picker.classList.contains('is-open') ? 'true' : 'false');
        if (picker.classList.contains('is-open')) { search.value = ''; render(); setTimeout(function () { search.focus(); }, 0); }
    });
    search.addEventListener('input', render);
    optionsHost.addEventListener('click', function (event) {
        var button = event.target.closest('button[data-value]'); if (!button) return;
        participation.value = button.dataset.value; participation.dispatchEvent(new Event('change', { bubbles: true })); update(); picker.classList.remove('is-open');
    });
    const outsideClick = event => { if (!picker.contains(event.target)) picker.classList.remove('is-open'); };
    document.addEventListener('click', outsideClick);
    if(window.topstarWorkspaceUrl) document.addEventListener('topstar:workspace-dispose',()=>document.removeEventListener('click',outsideClick),{once:true});
    function error(input, message) { input.classList.add('is-invalid'); if (input === participation) control.classList.add('is-invalid'); var host = form.querySelector('[data-error-for="' + input.name + '"]'); if (host) host.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i><span>' + message + '</span>'; }
    function clear(input) { input.classList.remove('is-invalid'); if (input === participation) control.classList.remove('is-invalid'); var host = form.querySelector('[data-error-for="' + input.name + '"]'); if (host) host.textContent = ''; }
    form.addEventListener('submit', function (event) {
        var first = null, category = form.elements.category, subject = form.elements.subject, description = form.elements.description;
        [participation, category, subject, description].forEach(clear);
        if (!participation.value) { error(participation, 'Vui lòng chọn chiến dịch cần hỗ trợ.'); first = first || control; }
        if (!category.value) { error(category, 'Vui lòng chọn nhóm vấn đề.'); first = first || category; }
        if (subject.value.trim().length < 5 || subject.value.trim().length > 200) { error(subject, 'Tiêu đề phải từ 5 đến 200 ký tự.'); first = first || subject; }
        if (description.value.trim().length < 10 || description.value.trim().length > 2000) { error(description, 'Mô tả phải từ 10 đến 2.000 ký tự.'); first = first || description; }
        if (first) { event.preventDefault(); first.focus(); }
    });
    form.querySelectorAll('input,textarea,select').forEach(function (input) { input.addEventListener('input', function () { if (input.classList.contains('is-invalid')) clear(input); }); input.addEventListener('change', function () { if (input.classList.contains('is-invalid')) clear(input); }); });
    update();
})();
$(function () {
    const table = document.getElementById("brandDisputeTable");
    if (!table || !$.fn.DataTable) return;
    $(table).DataTable(TopstarLists.adapt($(table), {
        pageLength: 10,
        lengthMenu: [[5, 10, 20, 50], ["5 dòng", "10 dòng", "20 dòng", "50 dòng"]],
        autoWidth: false,
        order: [[3, "desc"]],
        columnDefs: [{ targets: [0, 5], orderable: false, searchable: false }],
        dom: '<"brand-dispute-dt-toolbar"f l>t<"brand-dispute-dt-footer"ip>',
        language: { search: "", searchPlaceholder: "Tìm theo tiêu đề, chiến dịch hoặc KOC", lengthMenu: "Hiển thị _MENU_", emptyTable: "Chưa có yêu cầu hỗ trợ.", zeroRecords: "Không tìm thấy yêu cầu phù hợp.", info: "Hiển thị _START_–_END_ trong _TOTAL_ hồ sơ", infoEmpty: "Hiển thị 0 hồ sơ", infoFiltered: "(lọc từ _MAX_ hồ sơ)", paginate: { first: "Đầu", previous: "‹", next: "›", last: "Cuối" } },
        drawCallback: function () { const api=this.api(),start=api.page.info().start;api.column(0,{page:"current"}).nodes().each(function(cell,index){cell.innerHTML=start+index+1;}); }
    }));
});
