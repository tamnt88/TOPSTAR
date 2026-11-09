(function (window, $) {
    'use strict';
    const sizes = [10, 20, 50, 100];
    const pageSize = value => sizes.includes(Number(value)) ? Number(value) : 20;
    const positive = value => Math.min(1000000, Math.max(1, Math.floor(Number(value)) || 1));

    // Each page supplies its fields and columns; URL state and feedback stay shared.
    function configure(element, options, fields, sizeSelector) {
        element = typeof element === 'string' ? document.querySelector(element) : element;
        const url = new URL(window.topstarWorkspaceUrl || window.location.href);
        const defaultOrder = options.order || [[0, 'asc']];
        const defaults = {};
        Object.entries(fields).forEach(([key, selector]) => {
            const input = document.querySelector(selector);
            if (!input) return;
            defaults[key] = input.type === 'hidden' ? input.value : input.tagName === 'SELECT' ? input.options[0]?.value || '' : '';
            if (url.searchParams.has(key)) {
                const value = url.searchParams.get(key);
                if (input.tagName !== 'SELECT' || Array.from(input.options).some(option => option.value === value)) input.value = value;
            }
        });
        const sizeInput = document.querySelector(sizeSelector);
        const length = pageSize(url.searchParams.get('pageSize'));
        if (sizeInput) {
            sizeInput.replaceChildren(...sizes.map(size => new Option(`${size} dòng`, String(size), false, size === length)));
        }
        options.pageLength = length;
        options.displayStart = (positive(url.searchParams.get('page')) - 1) * length;
        options.lengthChange = false;
        options.processing = true;
        options.language = Object.assign({}, options.language, {
            processing: 'Đang tải dữ liệu…', info: 'Hiển thị _START_–_END_ / _TOTAL_ kết quả',
            infoEmpty: 'Hiển thị 0 / 0 kết quả', infoFiltered: '',
            zeroRecords: 'Không tìm thấy kết quả phù hợp với bộ lọc.',
            paginate: { first: '«', previous: '‹', next: '›', last: '»' }
        });
        if (options.ordering !== false && url.searchParams.has('orderColumn')) {
            const column = Number(url.searchParams.get('orderColumn'));
            if (Number.isInteger(column) && column >= 0 && column < options.columns.length && options.columns[column].orderable !== false)
                options.order = [[column, url.searchParams.get('orderDirection') === 'asc' ? 'asc' : 'desc']];
        }
        const feedback = document.createElement('div');
        feedback.className = 'list-feedback';
        feedback.hidden = true;
        feedback.setAttribute('role', 'alert');
        const message = document.createElement('span');
        const retry = document.createElement('button');
        retry.type = 'button';
        retry.className = 'btn btn-outline-secondary btn-sm';
        retry.textContent = 'Thử lại';
        feedback.append(message, retry);
        element.before(feedback);
        let api;
        let applied = {};
        const source = options.ajax.dataSrc;
        if (typeof source === 'function') options.ajax.dataSrc = function (response) {
            // DataTables discards stale rows; also prevent stale responses changing KPI cards.
            const latest = api?.ajax.params()?.draw;
            if (latest && Number(response.draw) < latest) return response.data || [];
            return source(response);
        };
        const readFields = () => Object.fromEntries(Object.entries(fields).map(([key, selector]) => [key, document.querySelector(selector)?.value || '']));
        const data = options.ajax.data;
        options.ajax.data = function (request) {
            applied = readFields();
            element.setAttribute('aria-busy', 'true');
            feedback.hidden = true;
            return data ? data(request) : undefined;
        };
        options.ajax.error = function (xhr, status) {
            if (status === 'abort') return;
            element.setAttribute('aria-busy', 'false');
            api?.processing(false);
            message.textContent = xhr.status === 401 || (xhr.status === 200 && String(xhr.responseText || '').includes('<html'))
                ? 'Phiên đăng nhập đã hết hạn. Vui lòng tải lại trang để đăng nhập.'
                : xhr.status === 403 ? 'Bạn không có quyền hoặc chưa đủ điều kiện xem dữ liệu này.'
                : 'Không thể tải dữ liệu. Kết quả đang hiển thị có thể chưa cập nhật.';
            feedback.hidden = false;
        };
        retry.addEventListener('click', () => api?.ajax.reload(null, false));
        const init = options.initComplete;
        options.initComplete = function () {
            api = this.api();
            api.table().container().classList.add('topstar-list');
            if (init) init.apply(this, arguments);
        };
        const draw = options.drawCallback;
        options.drawCallback = function () {
            api = this.api();
            api.table().container().classList.add('topstar-list');
            element.setAttribute('aria-busy', 'false');
            const info = api.page.info();
            if (info.serverSide && info.page > Math.max(0, info.pages - 1)) {
                api.page(Math.max(0, info.pages - 1)).draw('page');
                return;
            }
            // DataTables draws once before its first response. Persist only applied filters.
            if (Object.keys(applied).length) {
                const current = new URL(window.topstarWorkspaceUrl || window.location.href);
                Object.entries(applied).forEach(([key, value]) => value ? current.searchParams.set(key, value) : current.searchParams.delete(key));
                current.searchParams.set('page', String(info.page + 1));
                current.searchParams.set('pageSize', String(info.length));
                if (options.ordering !== false && api.order().length) {
                    current.searchParams.set('orderColumn', String(api.order()[0][0]));
                    current.searchParams.set('orderDirection', api.order()[0][1]);
                }
                if (window.topstarWorkspaceUrl) window.topstarWorkspaceUrl = current.href;
                else window.history.replaceState(window.history.state, '', current);
            }
            normalize(api.table().container());
            if (draw) draw.apply(this, arguments);
        };
        $(element).on('init.dt', function () {
            api = $(element).DataTable();
            const clear = document.querySelector(element.dataset.clearFilters || '[data-unused-clear-filter]');
            clear?.addEventListener('click', () => {
                Object.entries(fields).forEach(([key, selector]) => { const input = document.querySelector(selector); if (input) input.value = defaults[key]; });
                if (sizeInput) sizeInput.value = '20';
                if (options.ordering !== false) api.order(defaultOrder);
                api.page.len(20).ajax.reload(null, true);
            });
        });
        return options;
    }

    function normalize(root) {
        const icons = { first: ['angles-left', 'Trang đầu'], previous: ['chevron-left', 'Trang trước'], next: ['chevron-right', 'Trang sau'], last: ['angles-right', 'Trang cuối'] };
        Object.entries(icons).forEach(([key, [icon, label]]) => {
            root.querySelectorAll(`.dt-paging .${key}, .dataTables_paginate .${key}`).forEach(node => {
                const control = node.matches('a,button') ? node : node.querySelector('a,button');
                if (!control) return;
                if (control.dataset.listIcon !== icon) { control.innerHTML = `<i class="fa-solid fa-${icon}" aria-hidden="true"></i>`; control.dataset.listIcon = icon; }
                control.setAttribute('aria-label', label);
                control.title = label;
            });
        });
        if (document.body.classList.contains('dashboard-body-koc')) {
            const table = root.matches?.('table') ? root : root.querySelector('table');
            if (table) {
                root.classList.add('koc-card-list');
                const labels = Array.from(table.querySelectorAll('thead th')).map(cell => cell.textContent.trim());
                table.querySelectorAll('tbody tr').forEach(row => row.querySelectorAll('td').forEach((cell, index) => {
                    cell.dataset.label = labels[index] || '';
                }));
            }
        }
    }
    // Covers existing tables too, including those initialized in page-level scripts.
    $(document).on('init.dt draw.dt', (_event, settings) => {
        const root = settings?.nTableWrapper;
        if (root) { root.classList.add('topstar-list'); normalize(root); }
    });
    function adapt(target, options) {
        const element = target?.jquery ? target[0] : typeof target === 'string' ? document.querySelector(target) : target;
        if (!element || options.topstarManaged) return options;
        options.topstarManaged = true;
        const id = element.id || ('table-' + Array.from(document.querySelectorAll('table')).indexOf(element));
        const serverWindow = document.querySelector(`[data-list-window="${CSS.escape(id)}"]`);
        if (serverWindow) {
            // Rows and their action forms are already paged by the query service.
            // Do not search or paginate only this slice again in the browser.
            options.paging = false; options.info = false; options.searching = false; options.ordering = false; options.serverSide = false;
            if (element.dataset.listRows) { options.data=JSON.parse(document.getElementById(element.dataset.listRows)?.textContent || '[]'); delete options.ajax; }
            const init = options.initComplete;
            options.initComplete = function () { this.api().table().container().classList.add('topstar-list'); if(init)init.apply(this,arguments); const footer=serverWindow.querySelector('.list-window-footer');if(footer)this.api().table().container().after(footer); };
            return options;
        }
        const prefix = `list.${id}.`;
        const url = new URL(window.topstarWorkspaceUrl || location.href);
        // Multiple tables on one page keep separate paging, search and ordering.
        let scope = element.closest('section, [data-list-scope]') || element.closest('main') || document;
        const eligible = input => !input.closest('.modal, form[method="post"]') && /search|keyword|filter|status|sort|pagesize|pagelength|fromdate|todate/i.test(input.id + ' ' + input.name);
        let controls = Array.from(scope.querySelectorAll('input,select')).filter(eligible);
        if (!controls.length && document.querySelectorAll('table:not(.modal table)').length === 1) controls = Array.from(document.querySelectorAll('input,select')).filter(eligible);
        const sizeInput = controls.find(input => /pagesize|pagelength|length$/i.test(input.id));
        const filters = controls.filter(input => input !== sizeInput && input.type !== 'hidden');
        const defaults = new Map(filters.map(input => [input, input.tagName === 'SELECT' ? input.options[0]?.value || '' : '']));
        filters.forEach(input => { const value = url.searchParams.get(prefix + (input.id || input.name)); if (value !== null && (input.tagName !== 'SELECT' || Array.from(input.options).some(o => o.value === value))) input.value = value; });
        const length = pageSize(url.searchParams.get(prefix + 'size'));
        if (sizeInput) sizeInput.replaceChildren(...sizes.map(size => new Option(`${size} dòng`, String(size), false, size === length)));
        options.pageLength = length;
        options.lengthMenu = sizes;
        options.displayStart = (positive(url.searchParams.get(prefix + 'page')) - 1) * length;
        options.lengthChange = !sizeInput && options.paging !== false;
        const search = url.searchParams.get(prefix + 'q');
        if (search) options.search = { search };
        const originalOrder = options.order || [];
        try {
            const order = JSON.parse(url.searchParams.get(prefix + 'order'));
            const count = options.columns?.length || element.querySelectorAll('thead th').length;
            if (Array.isArray(order) && order.every(x => Array.isArray(x) && Number.isInteger(x[0]) && x[0] >= 0 && x[0] < count && ['asc','desc'].includes(x[1]))) options.order = order;
        } catch (_) { /* Ignore malformed URL state. */ }
        options.language = Object.assign({}, options.language, { lengthMenu: 'Hiển thị _MENU_ dòng', search: '', searchPlaceholder: options.language?.searchPlaceholder || 'Tìm kiếm', processing: 'Đang tải dữ liệu…', info: 'Hiển thị _START_–_END_ / _TOTAL_ kết quả', infoEmpty: 'Hiển thị 0 / 0 kết quả', infoFiltered: '', zeroRecords: 'Không tìm thấy kết quả phù hợp với bộ lọc.', paginate: { first: '«', previous: '‹', next: '›', last: '»' } });
        let api;
        let ready = false;
        const feedback = document.createElement('div');
        feedback.className = 'list-feedback'; feedback.hidden = true; feedback.setAttribute('role','alert');
        const message = document.createElement('span'), retry = document.createElement('button');
        retry.type = 'button'; retry.className = 'btn btn-outline-secondary btn-sm'; retry.textContent = 'Thử lại'; feedback.append(message,retry); element.before(feedback);
        const reload = () => options.ajax ? api?.ajax.reload(null,false) : api?.draw(false);
        retry.addEventListener('click',reload);
        if (options.ajax && typeof options.ajax !== 'function') {
            if (typeof options.ajax === 'string') options.ajax = { url: options.ajax };
            options.processing = true;
            const data = options.ajax.data;
            options.ajax.data = function (request) { feedback.hidden = true; element.setAttribute('aria-busy','true'); return typeof data === 'function' ? data(request) : Object.assign(request,data || {}); };
            options.ajax.error = (xhr,status) => { if(status==='abort')return; api = api || $(element).DataTable(); api.processing(false); element.setAttribute('aria-busy','false'); message.textContent = xhr.status === 403 ? 'Bạn chưa đủ quyền hoặc điều kiện xem danh sách này.' : 'Không thể tải dữ liệu. Vui lòng thử lại; kết quả cũ có thể chưa cập nhật.'; feedback.hidden = false; };
        }
        const init = options.initComplete, draw = options.drawCallback;
        options.initComplete = function () {
            api = this.api(); ready = true;
            const container = api.table().container(); container.classList.add('topstar-list');
            if (init) init.apply(this,arguments);
            const builtInSearch = container.querySelector('.dt-search input, .dataTables_filter input');
            if (builtInSearch) $(builtInSearch).off('.DT').on('keydown.topstar',event=>{if(event.key==='Enter'){event.preventDefault();api.search(builtInSearch.value).draw();}});
            const toolbar = document.createElement('div'); toolbar.className = 'list-actions';
            const apply = document.createElement('button'); apply.type='button';apply.className='btn btn-primary btn-sm';apply.textContent='Lọc';
            apply.addEventListener('click',()=>{if(builtInSearch)api.search(builtInSearch.value); if(options.ajax)api.ajax.reload(null,true);else api.draw();});
            const clear = document.createElement('button'); clear.type='button'; clear.className='btn btn-light btn-sm';clear.textContent='Xóa lọc';
            clear.addEventListener('click',()=>{defaults.forEach((value,input)=>input.value=value);if(sizeInput)sizeInput.value='20';if(builtInSearch)builtInSearch.value='';api.search('').columns().search('');if(options.ordering!==false)api.order(originalOrder);api.page.len(20); if(options.ajax)api.ajax.reload(null,true);else api.draw();});
            if (element.dataset.listActions !== 'false' && (builtInSearch || filters.length)) {
                toolbar.append(apply, clear);
                container.before(toolbar);
            }
            save();
        };
        function save() {
            if (!ready || !api) return;
            const info = api.page.info(), current = new URL(window.topstarWorkspaceUrl || location.href);
            current.searchParams.set(prefix+'page',String(info.page+1));current.searchParams.set(prefix+'size',String(info.length));
            const query = api.search(); if(query)current.searchParams.set(prefix+'q',query);else current.searchParams.delete(prefix+'q');
            if(options.ordering!==false)current.searchParams.set(prefix+'order',JSON.stringify(api.order()));
            filters.forEach(input=>{const key=prefix+(input.id||input.name);if(input.value)current.searchParams.set(key,input.value);else current.searchParams.delete(key);});
            if (window.topstarWorkspaceUrl) window.topstarWorkspaceUrl=current.href;
            else history.replaceState(history.state,'',current);
        }
        options.drawCallback = function () {
            api=this.api();element.setAttribute('aria-busy','false');
            const info=api.page.info();
            if(info.serverSide && info.page>Math.max(0,info.pages-1)){api.page(Math.max(0,info.pages-1)).draw('page');return;}
            save();normalize(api.table().container());if(draw)draw.apply(this,arguments);
        };
        return options;
    }
    window.TopstarFormat = {
        money: value => Number(value || 0).toLocaleString('vi-VN') + ' ₫',
        dateTime: value => {
            if (!value) return '—';
            const text = String(value);
            const date = new Date(/^\d{4}-\d{2}-\d{2}T/.test(text) && !/(Z|[+-]\d{2}:?\d{2})$/i.test(text) ? text + 'Z' : value);
            return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('vi-VN', {timeZone:'Asia/Ho_Chi_Minh',day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'}).format(date);
        }
    };
    document.addEventListener('submit', event => {
        const form=event.target;
        if(form.method?.toLowerCase()!=='post')return;
        const action=new URL(form.action,location.href);
        if(action.origin!==location.origin || action.pathname.toLowerCase()!==new URL(window.topstarWorkspaceUrl || location.href).pathname.toLowerCase())return;
        new URL(window.topstarWorkspaceUrl || location.href).searchParams.forEach((value,key)=>{if(key.startsWith('list.'))action.searchParams.set(key,value);});
        form.action=action.href;
    },true);
    window.TopstarLists = { configure, adapt, pageSize, sizes, money: value => Number(value || 0).toLocaleString('vi-VN') + ' ₫' };
    document.addEventListener('click', event => {
        const reset=event.target.closest('[data-list-reset]');
        if(reset){const form=reset.closest('form');form.querySelectorAll('input:not([type="hidden"])').forEach(input=>input.value='');form.querySelectorAll('select').forEach(input=>input.value=input.name.endsWith('.size')?'20':'');form.requestSubmit();}
        const shipment=event.target.closest('[data-shipment-filter]');
        const windowBlock=document.querySelector('[data-list-window="brandShipmentTable"]');
        if(shipment && windowBlock){event.preventDefault();event.stopImmediatePropagation();const value=shipment.dataset.shipmentFilter;const map={'Đang vận chuyển':'InTransit','Đã giao hàng':'Delivered',preparation:'preparation',exception:'exception'};const url=new URL(window.topstarWorkspaceUrl || location.href);url.searchParams.set('list.brandShipmentTable.status',map[value]||'');url.searchParams.set('list.brandShipmentTable.page','1');if(window.topstarWorkspaceUrl) document.dispatchEvent(new CustomEvent('topstar:workspace-navigate',{detail:url.href}));else location.assign(url);}
    },true);
})(window, window.jQuery);
