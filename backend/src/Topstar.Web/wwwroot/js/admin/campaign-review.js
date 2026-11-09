(() => {
  'use strict';
  const root = document.querySelector('[data-command-center]');
  if (!root) return;
  const flash = document.querySelector('[data-admin-flash]');
  if (flash?.dataset.message) window.AdminToast?.show(flash.dataset.message, flash.dataset.type || 'info');
  const buttons = [...root.querySelectorAll('[data-tab]')];
  const panels = [...root.querySelectorAll('[data-panel]')];
  const knownTabs = new Set(buttons.map(button => button.dataset.tab));
  const matchingDataTables = [];
  const activate = requestedTab => {
    const tab = knownTabs.has(requestedTab) ? requestedTab : 'overview';
    buttons.forEach(button => { const active = button.dataset.tab === tab; button.classList.toggle('is-active', active); button.setAttribute('aria-selected', String(active)); });
    panels.forEach(panel => panel.classList.toggle('is-active', panel.dataset.panel === tab));
    const url = new URL(window.location.href); url.searchParams.set('tab', tab); window.history.replaceState(null, '', url);
    if (tab === 'matching') window.setTimeout(() => matchingDataTables.forEach(table => table.columns.adjust()), 0);
  };
  buttons.forEach(button => button.addEventListener('click', () => activate(button.dataset.tab)));
  root.querySelectorAll('[data-open-tab]').forEach(button => button.addEventListener('click', () => {
    activate(button.dataset.openTab);
    root.querySelector('.campaign-command-tabs')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }));
  const createTable = (element, options = {}) => {
    if (!element || !window.jQuery?.fn?.DataTable) return null;
    const table = window.jQuery(element).DataTable({
      pageLength: 10,
      lengthMenu: [[5, 10, 20, 50], ['5 dòng', '10 dòng', '20 dòng', '50 dòng']],
      autoWidth: false,
      order: [],
      lengthChange: false,
      dom: "t<'campaign-dt-footer'i p>",
      language: {
        lengthMenu: 'Hiển thị _MENU_',
        info: '_START_–_END_ / _TOTAL_ KOC',
        infoEmpty: 'Chưa có KOC',
        zeroRecords: 'Không tìm thấy KOC phù hợp',
        paginate: { first: 'Đầu', previous: '‹', next: '›', last: 'Cuối' }
      },
      drawCallback: function () {
        const wrapper = this.api().table().container();
        const paging = wrapper.querySelector('.dt-paging');
        if (paging) paging.hidden = this.api().page.info().pages <= 1;
      },
      ...options
    });
    matchingDataTables.push(table);
    return table;
  };
  createTable(root.querySelector('[data-panel="matching"] .campaign-command-table:not(#assignedKocTable):not(#candidateKocTable)'), {
    columnDefs: [{ orderable: false, targets: [0, 5] }]
  });
  const assignedTable = createTable(document.getElementById('assignedKocTable'), {
    columnDefs: [{ orderable: false, searchable: false, targets: [5] }]
  });
  document.getElementById('assignedKocSearch')?.addEventListener('input', event => assignedTable?.search(event.target.value).draw());
  document.getElementById('assignedKocStatus')?.addEventListener('change', event => {
    const value = event.target.value;
    assignedTable?.column(4).search(value ? `^${window.jQuery.fn.dataTable.util.escapeRegex(value)}$` : '', true, false).draw();
  });
  let minimumCandidateFit = 0;
  const candidateElement = document.getElementById('candidateKocTable');
  if (candidateElement && window.jQuery?.fn?.DataTable) {
    window.jQuery.fn.dataTable.ext.search.push((settings, data, index) => {
      if (settings.nTable !== candidateElement || !minimumCandidateFit) return true;
      const cell = settings.aoData[index]?.anCells?.[4];
      return Number(cell?.dataset.fit || String(data[4]).replace(/[^0-9.,-]/g, '').replace(',', '.')) >= minimumCandidateFit;
    });
  }
  const candidateTable = createTable(candidateElement, {
    columnDefs: [{ orderable: false, searchable: false, targets: [0] }]
  });
  document.getElementById('candidateKocSearch')?.addEventListener('input', event => candidateTable?.search(event.target.value).draw());
  document.getElementById('candidateKocFit')?.addEventListener('change', event => {
    minimumCandidateFit = Number(event.target.value || 0);
    candidateTable?.draw();
  });
  const removeModalElement = document.getElementById('removeAssignedKocModal');
  const removeModal = removeModalElement && window.bootstrap?.Modal
    ? window.bootstrap.Modal.getOrCreateInstance(removeModalElement)
    : null;
  let pendingRemoveForm = null;
  root.querySelectorAll('.js-remove-assigned-form').forEach(form => form.addEventListener('submit', event => {
    if (form.dataset.confirmed === 'true') return;
    event.preventDefault();
    pendingRemoveForm = form;
    const name = removeModalElement?.querySelector('[data-remove-koc-name]');
    if (name) name.textContent = form.dataset.kocName || 'KOC này';
    removeModal?.show();
  }));
  removeModalElement?.querySelector('[data-confirm-remove-koc]')?.addEventListener('click', () => {
    if (!pendingRemoveForm) return;
    pendingRemoveForm.dataset.confirmed = 'true';
    removeModal?.hide();
    pendingRemoveForm.requestSubmit();
  });
  removeModalElement?.addEventListener('hidden.bs.modal', () => { pendingRemoveForm = null; });
  if (root.dataset.canManageMatching !== 'true') {
    root.querySelectorAll('[data-panel="matching"] form input, [data-panel="matching"] form button').forEach(control => { control.disabled = true; });
  }
  const setInvalid = (field, invalid) => {
    const wrapper = field.closest('.admin-validation-field');
    wrapper?.classList.toggle('is-invalid', invalid);
    field.classList.toggle('is-invalid', invalid);
    field.setAttribute('aria-invalid', String(invalid));
  };
  root.querySelectorAll('.js-command-form, .js-matching-form').forEach(form => {
    form.addEventListener('submit', event => {
      let valid = true; let firstInvalid = null;
      form.querySelectorAll('[required], [maxlength], [minlength]').forEach(field => {
        const value = field.value.trim();
        const min = Number(field.getAttribute('minlength') || 0);
        const max = Number(field.getAttribute('maxlength') || Number.MAX_SAFE_INTEGER);
        const invalid = (field.required && !value) || value.length < min || value.length > max;
        setInvalid(field, invalid);
        if (invalid && !firstInvalid) firstInvalid = field;
        valid = valid && !invalid;
      });
      if (form.classList.contains('js-matching-form')) {
        const selectable = [...form.querySelectorAll('input[type="checkbox"]:not(:disabled)')];
        if (selectable.length && !selectable.some(input => input.checked)) {
          valid = false; firstInvalid ??= selectable[0];
          window.TopstarAdmin?.toast?.('Vui lòng chọn ít nhất một KOC trước khi xác nhận.', 'warning');
        }
      }
      if (!valid) { event.preventDefault(); firstInvalid?.focus(); }
    });
    form.querySelectorAll('input, textarea, select').forEach(field => {
      const clear = () => setInvalid(field, false);
      field.addEventListener('input', clear); field.addEventListener('change', clear);
    });
  });
  activate(root.dataset.activeTab || new URL(window.location.href).searchParams.get('tab'));
})();
