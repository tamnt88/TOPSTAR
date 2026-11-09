(() => {
    document.querySelectorAll('.brand-prepay-amount-form').forEach(prepayForm => {
        const input = prepayForm.querySelector('[name="prepayAmount"]');
        if (!input) return;
        const format = () => {
            const raw = input.value.replace(/\./g, '');
            if (/^\d+$/.test(raw) && Number.isSafeInteger(Number(raw))) {
                input.value = Number(raw).toLocaleString('vi-VN');
            }
        };
        input.addEventListener('input', format);
        format();
    });
    const form = document.querySelector('.js-funding-form');
    if (form) {
        const radios = [...form.querySelectorAll('[name="allocationMode"]')];
        const inputs = [...form.querySelectorAll('.brand-money-input input')];
        const error = form.querySelector('.brand-funding-error');
        const budget = Number(form.dataset.budget || 0);
        const allocatedOutput = form.querySelector('[data-allocated]');
        const remainingOutput = form.querySelector('[data-remaining]');
        const fundingStats = [...document.querySelectorAll('.brand-funding-stats strong')];
        const submitButton = form.querySelector('button[type="submit"], button:not([type])');
        const clean = value => /^[0-9.]*$/.test(value||'') ? Number((value || '').replace(/\./g, '')) : NaN;
        const money = value => `${Math.max(0, value).toLocaleString('vi-VN')} ₫`;
        const format = input => { const value = clean(input.value); if(Number.isSafeInteger(value))input.value = value ? value.toLocaleString('vi-VN') : ''; };
        const isCustom = () => radios.find(x => x.checked)?.value === 'Custom';
        const refresh = (showMismatch = false) => {
            if(inputs.some(input=>!Number.isSafeInteger(clean(input.value)))) {
                if(submitButton)submitButton.disabled=true;
                if(error)error.textContent='Thù lao phải là số tiền nguyên đồng hợp lệ, không dùng số âm hoặc ký tự khác.';
                if(remainingOutput)remainingOutput.textContent='Chưa hợp lệ';
                return {allocated:NaN,remaining:NaN};
            }
            const allocated = inputs.reduce((sum, input) => sum + clean(input.value), 0);
            const remaining = budget - allocated;
            if (allocatedOutput) allocatedOutput.textContent = money(allocated);
            if (fundingStats[0]) fundingStats[0].textContent = money(allocated);
            if (fundingStats[1]) fundingStats[1].textContent = money(Math.max(0, remaining));
            if (remainingOutput) {
                remainingOutput.textContent = remaining < 0 ? `Vượt ${money(Math.abs(remaining))}` : money(remaining);
                remainingOutput.classList.toggle('is-over', remaining < 0);
            }
            const validAmounts = inputs.length && inputs.every(input=>Number.isSafeInteger(clean(input.value))&&clean(input.value)>0);
            if (submitButton) submitButton.disabled = !Number.isSafeInteger(budget) || budget<=0 || !validAmounts || allocated<=0 || allocated>budget || (!isCustom() && allocated!==budget);
            const remainingButton=form.querySelector('[data-funding-action="remaining"]');
            if(remainingButton)remainingButton.disabled=remaining<=0;
            if (error && (showMismatch || remaining < 0)) {
                error.innerHTML = remaining < 0
                    ? '<i class="fa-solid fa-circle-exclamation"></i> Tổng thù lao không được vượt ngân sách chiến dịch.'
                    : remaining > 0
                        ? (isCustom() ? '' : `<i class="fa-solid fa-circle-exclamation"></i> Cần phân bổ thêm ${money(remaining)}.`)
                        : '';
            } else if (error) error.textContent = '';
            return { allocated, remaining };
        };
        const sync = () => {
            const custom = isCustom();
            inputs.forEach(input => {
                if (custom) input.value = input.dataset.customValue || '';
                else input.value = input.dataset.equalValue || '';
                format(input);
                input.dataset.lastValid = String(clean(input.value) || '');
                input.disabled = !custom;
                input.closest('.brand-money-input').classList.remove('is-invalid');
            });
            refresh(false);
        };
        inputs.forEach(input => input.addEventListener('input', () => {
            const next = clean(input.value);
            const others = inputs.filter(x => x !== input).reduce((sum, x) => sum + clean(x.value), 0);
            input.dataset.customValue = String(next || '');
            input.dataset.lastValid = String(next || '');
            format(input);
            refresh(false);
        }));
        radios.forEach(radio => radio.addEventListener('change', sync));
        form.querySelector('[data-funding-action="equal"]')?.addEventListener('click', () => {
            const equalRadio = radios.find(x => x.value === 'Equal');
            if (equalRadio) equalRadio.checked = true;
            sync();
            inputs[0]?.focus();
        });
        form.querySelector('[data-funding-action="remaining"]')?.addEventListener('click', () => {
            if(inputs.some(input=>!Number.isSafeInteger(clean(input.value)))){error.textContent='Vui lòng sửa thù lao chưa hợp lệ trước khi phân bổ số dư.';return;}
            if (!isCustom()) radios.find(x => x.value === 'Custom').checked = true;
            inputs.forEach(input => { input.disabled = false; });
            const target = inputs.find(input => clean(input.value) === 0) || inputs.at(-1);
            if (!target) return;
            const others = inputs.filter(input => input !== target).reduce((sum, input) => sum + clean(input.value), 0);
            const value = Math.max(0, budget - others);
            target.value = String(value || '');
            target.dataset.customValue = String(value || '');
            target.dataset.lastValid = String(value || '');
            format(target);
            refresh(false);
            target.focus();
        });
        form.querySelector('[data-funding-action="clear"]')?.addEventListener('click', () => {
            const customRadio = radios.find(x => x.value === 'Custom');
            if (customRadio) customRadio.checked = true;
            inputs.forEach(input => { input.dataset.customValue = ''; });
            sync();
            inputs[0]?.focus();
        });
        sync();
        form.addEventListener('submit', event => {
            if (!inputs.length) { event.preventDefault(); error.textContent='Chưa có KOC được xác nhận để phân bổ thù lao.'; return; }
            let invalid = false;
            inputs.forEach(input => { const bad = clean(input.value) <= 0; input.closest('.brand-money-input').classList.toggle('is-invalid', bad); invalid ||= bad; });
            const totals = refresh(true);
            if (invalid || totals.allocated<=0 || totals.allocated>budget || (!isCustom() && totals.allocated !== budget)) { event.preventDefault(); if (invalid) error.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i> Vui lòng nhập thù lao lớn hơn 0 cho tất cả KOC.'; return; }
            inputs.forEach(input => input.value = String(clean(input.value)));
        });
    }

    document.querySelectorAll('.js-cancel-funding').forEach(cancelForm => {
        cancelForm.addEventListener('submit', event => {
            const reason = cancelForm.querySelector('[name="cancellationReason"]');
            const invalid = reason.value.trim().length < 10 || reason.value.trim().length > 500;
            reason.classList.toggle('is-invalid', invalid);
            if (invalid) { event.preventDefault(); event.stopPropagation(); reason.focus(); }
        });
    });

    document.querySelectorAll('[data-funding-review]').forEach(form => {
        const budget=Number(form.dataset.budget), platform=Number(form.dataset.platformFee), gateway=Number(form.dataset.gatewayFee);
        const modal=document.getElementById('fundingReviewModal'), confirm=modal?.querySelector('[data-review-confirm]');
        const money=value=>value.toLocaleString('vi-VN')+' đ';
        const refresh=()=>{
            const method=form.querySelector('[name="paymentMethod"]:checked')?.value;
            const fee=method==='OnePay'||method==='VnPay'?gateway:0;
            const total=budget+platform+fee+Number(form.dataset.vatAdded||0);
            form.querySelector('[data-review-gateway]').textContent=money(fee);
            form.querySelector('[data-review-total]').textContent=money(total);
            form.querySelector('[name="expectedTotal"]').value=String(total);
            if(modal)modal.querySelector('[data-review-modal-total]').textContent=money(total);
        };
        form.querySelectorAll('[name="paymentMethod"]').forEach(input=>input.addEventListener('change',()=>{delete form.dataset.reviewConfirmed;refresh();}));
        refresh();
        form.addEventListener('submit',event=>{
            if(event.defaultPrevented || form.dataset.reviewConfirmed==='true')return;
            event.preventDefault();refresh();if(modal)bootstrap.Modal.getOrCreateInstance(modal).show();
        });
        confirm?.addEventListener('click',()=>{form.dataset.reviewConfirmed='true';form.requestSubmit();});
    });

    document.querySelectorAll('.js-payment-proof-form').forEach(proofForm => {
        const file = proofForm.querySelector('[name="proofFile"]');
        const note = proofForm.querySelector('[name="proofNote"]');
        file?.addEventListener('change', () => {
            const selected = file.files?.[0];
            const label = proofForm.querySelector('.brand-payment-proof-file span b');
            if (label) label.textContent = selected?.name || 'Chọn ảnh từ thiết bị';
            file.closest('.brand-payment-proof-file')?.classList.remove('is-invalid');
        });
        proofForm.addEventListener('submit', event => {
            const selected = file?.files?.[0];
            const validFile = !!selected && selected.size <= 5 * 1024 * 1024 && /\.(jpe?g|png|webp)$/i.test(selected.name);
            const validNote = !!note && note.value.trim().length >= 10 && note.value.trim().length <= 500;
            file?.closest('.brand-payment-proof-file')?.classList.toggle('is-invalid', !validFile);
            note?.classList.toggle('is-invalid', !validNote);
            if (!validFile || !validNote) { event.preventDefault(); event.stopPropagation(); (!validFile ? file : note)?.focus(); }
        });
    });
})();
