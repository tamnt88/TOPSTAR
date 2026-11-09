(function ($) {
    'use strict';
    if (!$ || !$.fn.select2 || !$.fn.datepicker) return;
    const originalVal = $.fn.val;
    $.fn.val = function () {
        const result = originalVal.apply(this, arguments);
        if (arguments.length) this.filter('select.select2-hidden-accessible').trigger('change.select2');
        return result;
    };
    const isInactive = field => {
        for (let node = field.parentElement; node && node !== field.form; node = node.parentElement) {
            if (node.hidden || getComputedStyle(node).display === 'none') return true;
        }
        return false;
    };
    const configureValidation = form => {
        const validator = $(form).data('validator');
        if (!validator || validator.settings.tsControlsReady || form.dataset.sectionValidation === 'true') return;
        const previousIgnore = validator.settings.ignore;
        validator.settings.ignore = function (index, field) {
            if (field.matches('.select2-hidden-accessible,.ts-date-original,.ts-money-original')) return isInactive(field);
            return typeof previousIgnore === 'function' ? previousIgnore.call(field, index, field) : $(field).is(previousIgnore || []);
        };
        validator.settings.tsControlsReady = true;
    };
    window.TopstarForms = {
        focusField(field) {
            if (field.matches('.select2-hidden-accessible')) $(field).select2('open');
            else if (field.matches('.ts-date-original')) $(field).next('.ts-date-control').find('input').first().trigger('focus');
            else if (field.matches('.ts-money-original')) $(field).next('.ts-money-control').find('input').first().trigger('focus');
            else field.focus();
        },
        guardNativeSubmit(form, event) {
            const snapshot = Array.from(form.querySelectorAll('button,input[type="submit"]')).map(button => ({ button, disabled: button.disabled, html: button.innerHTML }));
            const submitter = event.submitter || event.originalEvent?.submitter;
            if (submitter?.name) {
                let field = form.querySelector('[data-ts-submitter]');
                if (!field) { field = document.createElement('input'); field.type = 'hidden'; field.dataset.tsSubmitter = 'true'; form.appendChild(field); }
                field.name = submitter.name; field.value = submitter.value;
            }
            const restore = () => {
                snapshot.forEach(x => { x.button.disabled = x.disabled; x.button.innerHTML = x.html; });
                form.classList.remove('is-submitting');
            };
            setTimeout(() => { if (event.defaultPrevented || event.isDefaultPrevented?.()) restore(); }, 0);
            window.addEventListener('pageshow', restore, { once: true });
        }
    };
    $.fn.select2.defaults.set('language', {
        noResults: () => 'Không tìm thấy kết quả',
        searching: () => 'Đang tìm kiếm…',
        errorLoading: () => 'Không thể tải kết quả',
        inputTooShort: () => 'Vui lòng nhập thêm ký tự',
        loadingMore: () => 'Đang tải thêm…'
    });
    $.datepicker.setDefaults({
        dateFormat: 'yy-mm-dd', firstDay: 1, changeMonth: true, changeYear: true,
        yearRange: 'c-100:c+20', showOtherMonths: true,
        monthNames: ['Tháng 1','Tháng 2','Tháng 3','Tháng 4','Tháng 5','Tháng 6','Tháng 7','Tháng 8','Tháng 9','Tháng 10','Tháng 11','Tháng 12'],
        monthNamesShort: ['Tháng 1','Tháng 2','Tháng 3','Tháng 4','Tháng 5','Tháng 6','Tháng 7','Tháng 8','Tháng 9','Tháng 10','Tháng 11','Tháng 12'],
        dayNamesMin: ['CN','T2','T3','T4','T5','T6','T7'],
        prevText: 'Tháng trước', nextText: 'Tháng sau', currentText: 'Hôm nay', closeText: 'Đóng'
    });
    const trimNumericZeros = value => /^-?\d+\.\d+$/.test(value) ? value.replace(/0+$/, '').replace(/\.$/, '') : value;
    const tabSelectors = '.nav-tabs,.account-detail-tabs,.menu-tabs,.admin-funding-tabs,.academy-tabs,.account-center-tabs,.security-tabs,.security-history-tabs,.campaign-command-tabs,.admin-campaign-tabs,.admin-notification-tabs,.ops-processing-tabs,.koc-history-tabs,.admin-violation-tabs,.balance-tabs,.brand-form-tabs,.brand-notification-tabs,.brand-package-tabs,.brand-review-tabs,.brand-shipment-tabs,.koc-notification-tabs,.koc-profile-tabs,.koc-review-tabs,.koc-task-detail-tabs,.tax-profile-tabs,.koc-wallet-tabs,.admin-dashboard-tabs,.admin-billing-tabs,.fee-policy-tabs,.admin-record-tabs,.homepage-tabs,.ops-tabs,.package-form-tabs';
    function moneySuffix(field) {
        if ($(field).closest('.ts-money-control').length) return;
        const parent = field.parentElement;
        const unit = parent && Array.from(parent.children).find(e => e !== field && /^(đ|₫|VND)$/i.test(e.textContent.trim()));
        if (unit) { parent.classList.add('ts-money-existing'); return; }
        $(field).wrap('<div class="ts-money-control"/>').after('<span class="ts-money-unit" aria-hidden="true">đ</span>');
    }
    function enhanceNumber(field) {
        if (field.type === 'hidden' || field.dataset.tsNumberReady || field.closest('.ck,.ck-editor,.ts-money-control,.ts-date-control')) return;
        const name = field.name || field.id || '';
        const label = field.labels?.length ? field.labels[0].textContent : '';
        const money = field.hasAttribute('data-money-input') || field.matches('.js-money,[data-money]') ||
            (!/rate|percent|phone|account|identity|taxnumber|followers|count/i.test(name) &&
            (/(?:amount|budget|budgetamount|minimumbudget|selfselectionminimumbudget|minimumbudgetperkoc|saleprice|referencebudget|fixedfee|minimumfee|maximumfee|dualapprovalthreshold|minimumwithdrawal|withdrawalfee|threshold|salary|price)$/i.test(name) || /(?:₫|\bVND\b|\(đ\))/i.test(label)));
        if (!money) {
            if (field.type === 'number' || field.inputMode === 'decimal') field.value = trimNumericZeros(field.value);
            return;
        }
        field.dataset.tsNumberReady = 'true';
        if (field.matches('.js-money,[data-money]') || /Display$/i.test(field.id) || (field.type === 'text' && !field.hasAttribute('data-val-number') && !field.hasAttribute('data-money-input') && /^-?\d{1,3}(?:\.\d{3})+$/.test(field.value))) { moneySuffix(field); return; }
        const original = field;
        const $group = $('<div class="ts-money-control"/>').insertAfter(original);
        const $display = $('<input type="text" class="form-control ts-money-display" inputmode="numeric" autocomplete="off"/>').appendTo($group);
        $('<span class="ts-money-unit" aria-hidden="true">đ</span>').appendTo($group);
        $display.attr('aria-label', label || 'Số tiền').attr('placeholder', original.placeholder || '');
        original.classList.add('ts-money-original'); original.tabIndex = -1;
        const sync = () => {
            if(original.validity.customError){$display.addClass('is-invalid');return;}
            const raw = trimNumericZeros(original.value);
            const formatted = /^-?\d+$/.test(raw) ? raw.replace(/\B(?=(\d{3})+(?!\d))/g, '.') : raw;
            if ($display.val() !== formatted) $display.val(formatted);
            $display.prop('disabled',original.disabled).prop('readOnly',original.readOnly).toggleClass('is-invalid',original.classList.contains('is-invalid'));
        };
        const update = () => {
            const entered = $display.val();
            const digitsAfterCaret = entered.slice($display[0].selectionStart ?? entered.length).replace(/\D/g,'').length;
            const text = entered.replace(/[\sđ₫]/gi,'');
            // Dots are visual thousands separators. While typing/deleting, groups may
            // temporarily have any length (e.g. 1.000 + 0), so never validate group sizes.
            let raw = /^-?[0-9.]*$/.test(text) ? text.replace(/\./g,'') : text;
            if (/^\d{1,3}(?:,\d{3})+$/.test(text)) raw=text.replace(/,/g,'');
            const valid = raw === '' || /^-?\d+$/.test(raw);
            original.setCustomValidity(valid ? '' : 'Vui lòng nhập số tiền nguyên đồng.');
            original.value = valid ? raw : '';
            $display.toggleClass('is-invalid',!valid);
            if (valid) sync();
            original.dispatchEvent(new Event('input',{bubbles:true}));original.dispatchEvent(new Event('change',{bubbles:true}));
            if (valid && document.activeElement === $display[0]) {
                const formatted = $display.val();
                let caret = formatted.length, remaining = digitsAfterCaret;
                while (caret > 0 && remaining > 0) { caret--; if (/\d/.test(formatted[caret])) remaining--; }
                $display[0].setSelectionRange(caret,caret);
            }
        };
        $display.on('input change',update);
        $(original).on('change',sync).on('invalid',()=>{$display.addClass('is-invalid').trigger('focus');});
        $(original.form).on('reset',()=>setTimeout(()=>{original.setCustomValidity('');original.classList.remove('is-invalid');sync();}));
        new MutationObserver(sync).observe(original,{attributes:true,attributeFilter:['value','disabled','readonly','class']});
        const descriptor=Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value');
        Object.defineProperty(original,'value',{configurable:true,get(){return descriptor.get.call(this);},set(value){descriptor.set.call(this,value);sync();}});
        sync();
    }
    function enhance(root) {
        const $root = $(root);
        $root.find(tabSelectors).addBack(tabSelectors).addClass('ts-tabs');
        $root.find('input').addBack('input').each(function(){enhanceNumber(this);});
        $root.find('form').addBack('form').each(function () { configureValidation(this); });
        $root.find('select').addBack('select').not('.select2-hidden-accessible,.ui-datepicker-month,.ui-datepicker-year,[data-native-select]').each(function () {
            const $select = $(this);
            $select.select2({ width: '100%', dropdownParent: $select.closest('.modal').length ? $select.closest('.modal') : $(document.body) });
            // Existing cascaded selects replace their options without emitting change.
            new MutationObserver(() => $select.trigger('change.select2')).observe(this, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'selected'] });
        });
        $root.find('input[type="date"],input[type="datetime-local"]').addBack('input[type="date"],input[type="datetime-local"]').each(function () {
            if (this.dataset.tsDateReady) return;
            this.dataset.tsDateReady = 'true';
            const original = this;
            const hasTime = original.type === 'datetime-local';
            const $group = $('<div class="ts-date-control"/>').insertAfter(original);
            const $dateWrap=$('<div class="ts-date-input-wrap"/>').appendTo($group);
            const $date = $('<input type="text" class="form-control ts-date-display" autocomplete="off" placeholder="dd/mm/yyyy"/>').appendTo($dateWrap);
            const $calendar=$('<button type="button" class="ts-date-icon" aria-label="Mở lịch" title="Chọn ngày"><svg viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M16 3v4M8 3v4M3 11h18M8 15h2m4 0h2"/></svg></button>').appendTo($dateWrap);
            $calendar.on('click',()=>{if(!original.disabled&&!original.readOnly)$date.datepicker('show');});
            $date.attr('aria-label', $('label[for="' + original.id + '"]').first().text() || 'Chọn ngày');
            const $time = hasTime ? $('<input type="time" class="form-control ts-date-time" aria-label="Giờ"/>').attr('step', original.step || '60').appendTo($group) : null;
            original.classList.add('ts-date-original');
            original.tabIndex = -1;
            const sync = () => {
                const parts = original.value.split('T');
                try { $date.datepicker('setDate', parts[0] ? $.datepicker.parseDate('yy-mm-dd', parts[0]) : null); } catch (_) { $date.val(''); }
                $date.prop('disabled', original.disabled).prop('readOnly', original.readOnly);
                $calendar.prop('disabled',original.disabled||original.readOnly);
                if ($time) $time.val(parts[1] || '').prop('disabled', original.disabled).prop('readOnly', original.readOnly);
            };
            const update = () => {
                try {
                    const date = $date.val() ? $.datepicker.formatDate('yy-mm-dd', $.datepicker.parseDate('dd/mm/yy', $date.val())) : '';
                    original.value = date ? date + (hasTime ? 'T' + ($time.val() || '00:00') : '') : '';
                    original.dispatchEvent(new Event('input', { bubbles: true }));
                    original.dispatchEvent(new Event('change', { bubbles: true }));
                    $date.removeClass('is-invalid');
                } catch (_) { original.value = ''; $date.addClass('is-invalid'); original.setCustomValidity('Vui lòng nhập ngày theo định dạng dd/mm/yyyy.'); return; }
                original.setCustomValidity('');
            };
            $date.datepicker({ dateFormat: 'dd/mm/yy', onSelect: update, beforeShow: () => {
                sync();
                $date.datepicker('option', 'minDate', original.min ? $.datepicker.parseDate('yy-mm-dd', original.min.split('T')[0]) : null);
                $date.datepicker('option', 'maxDate', original.max ? $.datepicker.parseDate('yy-mm-dd', original.max.split('T')[0]) : null);
            } }).on('change', update);
            $time?.on('change', update);
            $(original).on('change', sync).on('invalid', () => { $date.addClass('is-invalid').trigger('focus'); });
            $(original.form).on('reset', () => setTimeout(sync));
            $(original).closest('.modal').on('shown.bs.modal', sync);
            new MutationObserver(sync).observe(original, { attributes: true, attributeFilter: ['value','disabled','readonly'] });
            sync();
            const valueDescriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
            Object.defineProperty(original, 'value', {
                configurable: true,
                get() { return valueDescriptor.get.call(this); },
                set(value) { valueDescriptor.set.call(this, value); sync(); }
            });
        });
    }
    $(function () {
        enhance(document);
        if ($.validator) {
            $('form').each(function () { configureValidation(this); });
        }
        $(document).on('focusin.tsFormValidation', 'form :input', function () { if (this.form) configureValidation(this.form); });
        $(document).on('reset.tsFormControls', 'form', function () {
            const form = this;
            setTimeout(() => $(form).find('select.select2-hidden-accessible').trigger('change.select2'));
        });
        document.addEventListener('invalid', event => {
            const field = event.target;
            if (!field.matches('.select2-hidden-accessible,.ts-date-original,.ts-money-original')) return;
            event.preventDefault();
            field.classList.add('is-invalid');
            window.TopstarForms.focusField(field);
            window.TopstarNotify?.warning(field.validationMessage || 'Vui lòng kiểm tra thông tin bắt buộc.');
        }, true);
        new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => {
            if (node.nodeType === 1 && !node.closest('.select2-container,#ui-datepicker-div,.ts-date-control')) enhance(node);
        }))).observe(document.body, { childList: true, subtree: true });
    });
})(window.jQuery);
