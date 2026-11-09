(() => {
    document.querySelectorAll('select.brand-search-multiselect').forEach(select => {
        if (window.jQuery?.fn.select2) return;
        select.classList.add('brand-native-multiselect');
        const root = document.createElement('div');
        root.className = 'brand-multiselect';
        root.innerHTML = '<div class="brand-multiselect-control" role="combobox" aria-expanded="false"><div class="brand-multiselect-values"></div><input type="search" class="brand-multiselect-search" autocomplete="off"><i class="fa-solid fa-chevron-down"></i></div><div class="brand-multiselect-panel"><div class="brand-multiselect-options"></div><div class="brand-multiselect-empty">Không tìm thấy kết quả phù hợp.</div></div>';
        select.insertAdjacentElement('afterend', root);
        const control = root.querySelector('.brand-multiselect-control');
        const values = root.querySelector('.brand-multiselect-values');
        const search = root.querySelector('.brand-multiselect-search');
        const optionsHost = root.querySelector('.brand-multiselect-options');
        const empty = root.querySelector('.brand-multiselect-empty');
        search.placeholder = select.dataset.placeholder || 'Tìm và chọn';

        function renderOptions(query) {
            const normalized = (query || '').trim().toLocaleLowerCase('vi');
            optionsHost.innerHTML = '';
            let count = 0;
            [...select.options].forEach((option, index) => {
                if (normalized && !option.text.toLocaleLowerCase('vi').includes(normalized)) return;
                count++;
                const item = document.createElement('button');
                item.type = 'button';
                item.className = `brand-multiselect-option${option.selected ? ' is-selected' : ''}`;
                item.dataset.index = String(index);
                const label = document.createElement('span');
                label.textContent = option.text;
                const check = document.createElement('i');
                check.className = 'fa-solid fa-check';
                item.append(label, check);
                optionsHost.appendChild(item);
            });
            empty.hidden = count > 0;
        }
        function renderValues() {
            values.innerHTML = '';
            [...select.selectedOptions].forEach(option => {
                const chip = document.createElement('span');
                chip.className = 'brand-multiselect-chip';
                const label = document.createElement('span');
                label.textContent = option.text;
                const remove = document.createElement('button');
                remove.type = 'button';
                remove.dataset.value = option.value;
                remove.setAttribute('aria-label', `Bỏ chọn ${option.text}`);
                remove.innerHTML = '<i class="fa-solid fa-xmark"></i>';
                chip.append(label, remove);
                values.appendChild(chip);
            });
            search.placeholder = select.selectedOptions.length ? 'Tìm thêm...' : (select.dataset.placeholder || 'Tìm và chọn');
            renderOptions(search.value);
        }
        function open() {
            if (select.matches(':disabled')) return;
            root.classList.add('is-open');
            control.setAttribute('aria-expanded', 'true');
            search.focus();
            renderOptions(search.value);
        }
        function close() {
            root.classList.remove('is-open');
            control.setAttribute('aria-expanded', 'false');
            search.value = '';
            renderOptions('');
        }
        control.addEventListener('click', event => {
            const remove = event.target.closest('.brand-multiselect-chip button');
            if (remove) {
                event.stopPropagation();
                const option = [...select.options].find(item => item.value === remove.dataset.value);
                if (option) option.selected = false;
                select.dispatchEvent(new Event('change', { bubbles: true }));
                renderValues();
                return;
            }
            open();
        });
        search.addEventListener('input', () => { open(); renderOptions(search.value); });
        search.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
        optionsHost.addEventListener('click', event => {
            const item = event.target.closest('.brand-multiselect-option');
            if (!item) return;
            const option = select.options[Number(item.dataset.index)];
            option.selected = !option.selected;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            renderValues();
            search.focus();
        });
        const outsideClick = event => { if (!root.contains(event.target)) close(); };
        document.addEventListener('click', outsideClick);
        if(window.topstarWorkspaceUrl) document.addEventListener('topstar:workspace-dispose',()=>document.removeEventListener('click',outsideClick),{once:true});
        renderValues();
    });

    document.querySelectorAll('[data-campaign-image-preview]').forEach(preview => {
        const input = document.getElementById(preview.dataset.campaignImagePreview);
        let objectUrl;
        input?.addEventListener('change', () => {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            const file = input.files?.[0];
            if (!file) return;
            if (!['image/jpeg','image/png','image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
                window.TopstarNotify?.warning('Chọn ảnh JPG, PNG hoặc WEBP, tối đa 5 MB.');
                input.value = '';
                return;
            }
            objectUrl = URL.createObjectURL(file);
            const image = document.createElement('img');
            image.src = objectUrl;
            image.alt = 'Ảnh chiến dịch đã chọn';
            preview.replaceChildren(image);
        });
    });
    const executionModes = {
        remote: 'RemoteProductReview',
        livestream: 'LivestreamOnline',
        onsite: ['FoodReviewOnsite', 'StoreReviewOnsite', 'ServiceReviewOnsite', 'EventOnsite']
    };
    function selectedExecutionMode() {
        return document.querySelector('input[name="Input.Execution.Mode"]:checked')?.value || '';
    }
    function updateExecutionFields() {
        const mode = selectedExecutionMode();
        const onsite = executionModes.onsite.includes(mode);
        document.querySelectorAll('[data-execution-group="onsite"]').forEach(group => {
            group.hidden = !onsite;
            group.querySelectorAll('input,textarea,select').forEach(field => field.disabled = !onsite);
        });
        document.querySelectorAll('[data-execution-group="remote"]').forEach(group => {
            group.hidden = mode !== executionModes.remote;
            group.querySelectorAll('input,textarea,select').forEach(field => field.disabled = mode !== executionModes.remote);
        });
        const scheduled = onsite || mode === executionModes.livestream;
        document.querySelectorAll('[data-execution-group="scheduled"]').forEach(group => {
            group.hidden = !scheduled;
            group.querySelectorAll('input,textarea,select').forEach(field => field.disabled = !scheduled);
        });
    }
    document.querySelectorAll('input[name="Input.Execution.Mode"]').forEach(input => input.addEventListener('change', updateExecutionFields));
    updateExecutionFields();

    const deliverableList = document.getElementById('deliverable-list');
    const addDeliverableButton = document.getElementById('add-deliverable');
    if (deliverableList && addDeliverableButton) {
        const reindexDeliverables = () => {
            [...deliverableList.querySelectorAll('.deliverable-row')].forEach((row, index) => {
                row.querySelector('.deliverable-type').name = `Input.Deliverables[${index}].DeliverableType`;
                row.querySelector('.deliverable-quantity').name = `Input.Deliverables[${index}].Quantity`;
                row.querySelector('.deliverable-due').name = `Input.Deliverables[${index}].DueDateUtc`;
            });
            [...deliverableList.querySelectorAll('.remove-deliverable')].forEach(button => {
                button.disabled = deliverableList.children.length === 1;
            });
            addDeliverableButton.disabled = deliverableList.children.length >= 10;
        };

        deliverableList.addEventListener('click', event => {
            const removeButton = event.target.closest('.remove-deliverable');
            if (!removeButton || deliverableList.children.length === 1) return;
            removeButton.closest('.deliverable-row')?.remove();
            reindexDeliverables();
        });
        addDeliverableButton.addEventListener('click', () => {
            if (deliverableList.children.length >= 10) return;
            const row = document.createElement('div');
            row.className = 'deliverable-row border rounded-3 p-3 mb-2';
            row.innerHTML = '<div class="row g-2 align-items-start"><div class="col-12 col-md-5"><label class="form-label small">Loại nội dung <span class="text-danger">*</span></label><div class="campaign-validation-field"><select class="form-select deliverable-type"><option value="">Chọn loại nội dung</option></select><i class="fa-solid fa-circle-exclamation"></i></div><span class="invalid-feedback campaign-deliverable-type-error"></span></div><div class="col-5 col-md-2"><label class="form-label small">Số lượng <span class="text-danger">*</span></label><div class="campaign-validation-field"><input type="number" inputmode="numeric" class="form-control deliverable-quantity" value="1" min="1" max="100"><i class="fa-solid fa-circle-exclamation"></i></div><span class="invalid-feedback campaign-deliverable-quantity-error"></span></div><div class="col-7 col-md-4"><label class="form-label small">Hạn nộp dự kiến <span class="text-danger">*</span></label><input type="datetime-local" class="form-control deliverable-due" required><span class="invalid-feedback campaign-deliverable-due-error"></span></div><div class="col-12 col-md-1 deliverable-action-col"><button type="button" class="btn btn-outline-danger w-100 remove-deliverable" aria-label="Xóa nội dung"><i class="fa-solid fa-trash-can"></i></button></div></div>';
            const typeSelect = row.querySelector('.deliverable-type');
            document.querySelectorAll('#deliverableTypeOptions option').forEach(option => typeSelect.append(option.cloneNode(true)));
            deliverableList.appendChild(row);
            reindexDeliverables();
        });
        reindexDeliverables();
    }

    const field = document.getElementById('Input_Objective');
    const campaignForm = document.getElementById('campaign-form');
    const today = campaignForm?.dataset.today || new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Ho_Chi_Minh',year:'numeric',month:'2-digit',day:'2-digit'}).format(new Date());
    function updateDateBounds() {
        const start = document.getElementById('Input_StartDate'), end = document.getElementById('Input_EndDate');
        if (!start || !end) return;
        start.min=today;
        const minimum=start.value&&start.value>today?start.value:today;
        end.min=minimum;
        document.querySelectorAll('.deliverable-due').forEach(input=>{input.min=minimum+'T00:00'; input.max=end.value?end.value+'T23:59':'';});
    }
    campaignForm?.addEventListener('change',updateDateBounds);
    if(campaignForm) new MutationObserver(updateDateBounds).observe(document.getElementById('deliverable-list')||campaignForm,{childList:true,subtree:true});
    updateDateBounds();

    function setInvalid(input, message, messageElement) {
        input.classList.add('is-invalid');
        input.setAttribute('aria-invalid', 'true');
        if (messageElement) {
            messageElement.textContent = message;
            messageElement.classList.add('field-validation-error');
        }
    }
    function clearInvalid(input, messageElement) {
        input.classList.remove('is-invalid', 'input-validation-error');
        input.setAttribute('aria-invalid', 'false');
        if (messageElement) {
            messageElement.textContent = '';
            messageElement.classList.remove('field-validation-error');
        }
    }
    function showInvalidTab(input) {
        for (let parent = input?.parentElement; parent; parent = parent.parentElement) if (parent.tagName === 'DETAILS') parent.open = true;
        const pane = input.closest('.tab-pane');
        if (!pane) return;
        const trigger = document.querySelector(`[data-bs-target="#${pane.id}"]`);
        if (trigger) bootstrap.Tab.getOrCreateInstance(trigger).show();
    }
    function validateCampaignForm(focusFirst) {
        let first = null;
        const title = document.getElementById('Input_Title');
        const titleError = document.getElementById('campaignTitleError');
        const titleLength = title.value.trim().length;
        clearInvalid(title, titleError);
        if (!titleLength) {
            setInvalid(title, 'Vui lòng nhập tên chiến dịch.', titleError);
            first = title;
        } else if (titleLength > 200) {
            setInvalid(title, 'Tên chiến dịch không được vượt quá 200 ký tự.', titleError);
            first = title;
        }

        const budget = document.getElementById('campaignBudgetDisplay');
        const budgetValue = document.getElementById('Input_BudgetAmount');
        const budgetError = document.getElementById('campaignBudgetError');
        if (budget) {
        clearInvalid(budget, budgetError);
        const numericBudget = Number((budget.value || '').replace(/[^\d]/g, ''));
        budgetValue.value = numericBudget ? String(numericBudget) : '0';
        if (numericBudget <= 0) {
            setInvalid(budget, 'Ngân sách phải lớn hơn 0.', budgetError);
            first ||= budget;
        }

        }

        const startDate = document.getElementById('Input_StartDate');
        const endDate = document.getElementById('Input_EndDate');
        const startError = document.getElementById('campaignStartDateError');
        const endError = document.getElementById('campaignEndDateError');
        clearInvalid(startDate, startError);
        clearInvalid(endDate, endError);
        if (!startDate.value) {
            setInvalid(startDate, 'Vui lòng chọn ngày bắt đầu.', startError);
            first ||= startDate;
        }
        if(startDate.value && startDate.value < today) { setInvalid(startDate,'Ngày bắt đầu dự kiến phải từ hôm nay trở đi.',startError); first ||= startDate; }
        if (!endDate.value) {
            setInvalid(endDate, 'Vui lòng chọn ngày kết thúc.', endError);
            first ||= endDate;
        } else if (startDate.value && endDate.value < startDate.value) {
            setInvalid(endDate, 'Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.', endError);
            first ||= endDate;
        }

        document.querySelectorAll('.deliverable-row').forEach(row => {
            const due = row.querySelector('.deliverable-due');
            const dueError = row.querySelector('.campaign-deliverable-due-error');
            clearInvalid(due,dueError);
            const dueDay = due?.value.slice(0,10);
            if(dueDay && dueDay < today) {setInvalid(due,'Hạn nộp phải từ hôm nay trở đi.',dueError);first ||= due;}
            if (!due?.value) {
                setInvalid(due,'Vui lòng chọn ngày và giờ hạn nộp.',dueError);
                first ||= due;
            }

            if(dueDay && ((startDate.value && dueDay < startDate.value) || (endDate.value && dueDay > endDate.value))) {
                setInvalid(due, startDate.value && dueDay < startDate.value ? 'Hạn nộp không được trước ngày bắt đầu dự kiến của chiến dịch.' : 'Hạn nộp không được sau ngày kết thúc dự kiến của chiến dịch.',dueError);
                first ||= due;
            }
            const type = row.querySelector('.deliverable-type');
            const quantity = row.querySelector('.deliverable-quantity');
            const typeError = row.querySelector('.campaign-deliverable-type-error');
            const quantityError = row.querySelector('.campaign-deliverable-quantity-error');
            clearInvalid(type, typeError);
            clearInvalid(quantity, quantityError);
            if (!type.value.trim()) {
                setInvalid(type, 'Vui lòng nhập loại nội dung.', typeError);
                first ||= type;
            } else if (type.value.trim().length > 100) {
                setInvalid(type, 'Loại nội dung không được vượt quá 100 ký tự.', typeError);
                first ||= type;
            }
            const value = Number(quantity.value);
            if (!Number.isInteger(value) || value < 1 || value > 100) {
                setInvalid(quantity, 'Số lượng phải từ 1 đến 100.', quantityError);
                first ||= quantity;
            }
        });

        const mode = selectedExecutionMode();
        const onsite = executionModes.onsite.includes(mode);
        const executionFields = [
            ['Input_Execution_VenueName', 'Vui lòng nhập tên địa điểm.', onsite],
            ['Input_Execution_VenueAddress', 'Vui lòng nhập địa chỉ thực hiện.', onsite],
            ['Input_Execution_ContactName', 'Vui lòng nhập người liên hệ.', onsite],
            ['Input_Execution_ContactPhone', 'Vui lòng nhập số điện thoại liên hệ.', onsite],
            ['Input_Execution_Schedule', mode === executionModes.livestream ? 'Vui lòng nhập lịch livestream dự kiến.' : 'Vui lòng nhập khung thời gian KOC có thể đến.', onsite || mode === executionModes.livestream],
        ];
        executionFields.forEach(([id, message, required]) => {
            const input = document.getElementById(id);
            if (!input) return;
            const error = input.closest('.campaign-validation-field')?.nextElementSibling;
            clearInvalid(input, error);
            const deferred = document.getElementById('Input_CheckoutAtCreation')?.value.toLowerCase() === 'true';
            if (!deferred && required && !input.value.trim()) {
                setInvalid(input, message, error);
                first ||= input;
            }
        });
        const phone = document.getElementById('Input_Execution_ContactPhone');
        if (onsite && phone?.value.trim() && !/^[0-9+\s().-]{8,20}$/.test(phone.value.trim())) {
            const error = phone.closest('.campaign-validation-field')?.nextElementSibling;
            setInvalid(phone, 'Số điện thoại liên hệ không đúng định dạng.', error);
            first ||= phone;
        }
        if (first && focusFirst) {
            showInvalidTab(first);
            window.setTimeout(() => first.focus(), 120);
        }
        return !first;
    }
    campaignForm?.addEventListener('submit', event => {
        if (!validateCampaignForm(true)) event.preventDefault();
    });
    campaignForm?.addEventListener('input', event => {
        const input = event.target.closest('.form-control');
        if (!input || (!input.classList.contains('is-invalid') && !input.classList.contains('input-validation-error'))) return;
        validateCampaignForm(false);
    });
    const budgetDisplay = document.getElementById('campaignBudgetDisplay');
    const budgetHidden = document.getElementById('Input_BudgetAmount');
    if (budgetDisplay && budgetHidden) {
        const initialBudget = Number(String(budgetHidden.value || '').replace(',', '.'));
        budgetDisplay.value = initialBudget > 0 ? Math.trunc(initialBudget).toLocaleString('vi-VN') : '';
        if (budgetHidden.classList.contains('input-validation-error')) {
            budgetHidden.classList.remove('input-validation-error');
            budgetDisplay.classList.add('input-validation-error');
        }
        budgetDisplay.addEventListener('input', function () {
            const digits = this.value.replace(/[^\d]/g, '').replace(/^0+(?=\d)/, '');
            budgetHidden.value = digits || '0';
            this.value = digits ? Number(digits).toLocaleString('vi-VN') : '';
        });
    }
    if (campaignForm?.querySelector('#campaignAdditional .field-validation-error')) document.getElementById('campaignAdditional').open = true;
    const serverInvalid = campaignForm?.querySelector('.input-validation-error');
    if (serverInvalid) {
        showInvalidTab(serverInvalid);
        window.setTimeout(() => serverInvalid.focus(), 120);
    }

    const C = window.CKEDITOR;
    if (!field || !C?.ClassicEditor) return;

    function ServerUploadAdapter(loader) {
        this.loader = loader;
        this.controller = new AbortController();
    }
    ServerUploadAdapter.prototype.upload = function () {
        return this.loader.file.then(file => {
            const data = new FormData();
            const token = field.closest('form')?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            data.append('upload', file);
            data.append('__RequestVerificationToken', token);
            return fetch(`${new URL(window.topstarWorkspaceUrl || window.location.href).pathname}?handler=EditorImage`, {
                method: 'POST',
                body: data,
                signal: this.controller.signal,
                headers: { RequestVerificationToken: token }
            }).then(async response => {
                const body = await response.json();
                if (!response.ok || !body.url) throw new Error(body.error?.message || 'Không thể tải ảnh lên.');
                return { default: body.url };
            });
        });
    };
    ServerUploadAdapter.prototype.abort = function () { this.controller.abort(); };
    function ServerUploadAdapterPlugin(editor) {
        editor.plugins.get('FileRepository').createUploadAdapter = loader => new ServerUploadAdapter(loader);
    }

    C.ClassicEditor.create(field, {
        licenseKey: 'GPL',
        language: 'vi',
        plugins: [
            C.Essentials, C.Paragraph, C.Heading, C.Bold, C.Italic, C.Underline, C.Strikethrough,
            C.Subscript, C.Superscript, C.Code, C.RemoveFormat, C.FontFamily, C.FontSize, C.FontColor,
            C.FontBackgroundColor, C.Highlight, C.Alignment, C.List, C.ListProperties, C.TodoList,
            C.Indent, C.IndentBlock, C.Link, C.Table, C.TableToolbar, C.BlockQuote,
            C.Image, C.ImageToolbar, C.ImageCaption, C.ImageStyle, C.ImageResize, C.ImageUpload,
            C.GeneralHtmlSupport, ServerUploadAdapterPlugin
        ],
        toolbar: {
            items: [
                'undo', 'redo', '|', 'heading', '|', 'fontFamily', 'fontSize', 'fontColor',
                'fontBackgroundColor', 'highlight', '|', 'bold', 'italic', 'underline', 'strikethrough',
                'subscript', 'superscript', 'code', 'removeFormat', '|', 'alignment', '|',
                'bulletedList', 'numberedList', 'todoList', 'outdent', 'indent', '|', 'link',
                'insertTable', 'blockQuote', '|', 'uploadImage'
            ],
            shouldNotGroupWhenFull: false
        },
        heading: {
            options: [
                { model: 'paragraph', title: 'Đoạn văn', class: 'ck-heading_paragraph' },
                { model: 'heading2', view: 'h2', title: 'Tiêu đề 1', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Tiêu đề 2', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Tiêu đề 3', class: 'ck-heading_heading4' }
            ]
        },
        table: { contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'] },
        image: { toolbar: ['imageTextAlternative', 'toggleImageCaption', '|', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side', '|', 'resizeImage'] },
        link: { addTargetToExternalLinks: true, defaultProtocol: 'https://' },
        htmlSupport: {
            allow: [{
                name: /^(p|br|ul|ol|li|span|h2|h3|h4|blockquote|pre|code|a|figure|figcaption|img|table|thead|tbody|tr|th|td)$/,
                attributes: true,
                classes: true,
                styles: true
            }]
        },
        placeholder: 'Nhập brief, mục tiêu, yêu cầu nội dung và lưu ý cho KOC'
    }).then(editor => {
        if (!field.isConnected) { editor.destroy(); return; }
        document.addEventListener('topstar:workspace-dispose', () => editor.destroy(), { once:true });
        const form = field.closest('form');
        form?.addEventListener('submit', () => {
            field.value = editor.getData();
        });
    }).catch(error => {
        console.error('Không thể khởi tạo CKEditor cho nội dung chiến dịch.', error);
    });
})();

(() => {
    const input = document.getElementById('Input_DesiredKocCount');
    const message = document.getElementById('campaignDesiredKocCountError');
    if (!input || !message) return;
    const quota = Number(input.dataset.packageQuota || input.max || 100);
    const validateQuota = () => {
        const value = Number(input.value || 0);
        const invalid = value > quota;
        input.classList.toggle('is-invalid', invalid);
        input.setAttribute('aria-invalid', invalid ? 'true' : 'false');
        if (invalid) {
            input.setCustomValidity(`Gói đã mua chỉ cho phép xác nhận tối đa ${quota} KOC.`);
            message.textContent = `Gói đã mua chỉ cho phép xác nhận tối đa ${quota} KOC.`;
            message.classList.add('field-validation-error');
            message.classList.remove('field-validation-valid');
        } else {
            input.setCustomValidity('');
            if (message.textContent.includes('xác nhận tối đa')) {
                message.textContent = '';
                message.classList.remove('field-validation-error');
                message.classList.add('field-validation-valid');
            }
        }
    };
    input.addEventListener('input', validateQuota);
    input.addEventListener('change', validateQuota);
    input.closest('form')?.addEventListener('submit', validateQuota);
    validateQuota();
})();
