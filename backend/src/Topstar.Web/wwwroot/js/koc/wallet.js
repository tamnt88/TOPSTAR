(() => {
    const hasDataTables = Boolean(window.jQuery?.fn?.DataTable);

    const language = {
        emptyTable: 'Chưa có dữ liệu.',
        zeroRecords: 'Không tìm thấy dữ liệu phù hợp.',
        info: 'Hiển thị _START_–_END_ trong _TOTAL_ kết quả',
        infoEmpty: 'Không có dữ liệu',
        infoFiltered: '',
        lengthMenu: 'Hiển thị _MENU_ dòng',
        paginate: {
            first: 'Đầu',
            previous: '<i class="fa-solid fa-chevron-left"></i>',
            next: '<i class="fa-solid fa-chevron-right"></i>',
            last: 'Cuối'
        }
    };

    const createTable = (selector, searchSelector, statusSelector, dateColumn) => {
        if (!hasDataTables) return null;
        const element = document.querySelector(selector);
        if (!element) return null;

        const table = window.jQuery(element).DataTable(TopstarLists.adapt(window.jQuery(element), {
            pageLength: 10,
            lengthMenu: [10, 20, 50],
            ordering: true,
            order: [[dateColumn, 'desc']],
            autoWidth: false,
            responsive: false,
            dom: 'rt<"koc-wallet-table-footer"ip>',
            columnDefs: [
                {
                    targets: 0,
                    orderable: false,
                    searchable: false,
                    width: '48px',
                    render: (_value, _type, _row, meta) => meta.settings._iDisplayStart + meta.row + 1
                }
            ],
            language
        }));

        const search = document.querySelector(searchSelector);
        const status = document.querySelector(statusSelector);
        search?.addEventListener('input', () => table.search(search.value.trim()).draw());
        status?.addEventListener('change', () => table.column(status.dataset.column).search(status.value, false, false).draw());
        table.on('draw', () => {
            table.column(0, { page: 'current' }).nodes().each((cell, index) => {
                cell.textContent = table.page.info().start + index + 1;
            });
        });
        return table;
    };

    document.querySelector('#walletPayoutStatus')?.setAttribute('data-column', '5');
    document.querySelector('#walletTransactionStatus')?.setAttribute('data-column', '3');
    const payoutTable = createTable('#walletPayoutTable', '#walletPayoutSearch', '#walletPayoutStatus', 2);
    const transactionTable = createTable('#walletTransactionTable', '#walletTransactionSearch', '#walletTransactionStatus', 2);

    document.querySelectorAll('[data-bs-toggle="tab"]').forEach(tab => {
        tab.addEventListener('shown.bs.tab', () => {
            payoutTable?.columns.adjust();
            transactionTable?.columns.adjust();
        });
    });

    const historyModal = document.getElementById('walletHistoryModal');
    historyModal?.addEventListener('show.bs.modal', event => {
        const trigger = event.relatedTarget;
        const templateId = trigger?.getAttribute('data-wallet-history-id');
        const template = templateId ? document.getElementById(templateId) : null;
        const content = historyModal.querySelector('[data-wallet-history-content]');
        const title = historyModal.querySelector('.modal-title');
        if (!template || !content) {
            event.preventDefault();
            return;
        }
        content.replaceChildren(template.content.cloneNode(true));
        if (title) title.textContent = `Chi tiết giao dịch · ${template.dataset.walletHistoryTitle || 'Chiến dịch'}`;
    });
    historyModal?.addEventListener('hidden.bs.modal', () => {
        historyModal.querySelector('[data-wallet-history-content]')?.replaceChildren();
        const title = historyModal.querySelector('.modal-title');
        if (title) title.textContent = 'Chi tiết giao dịch';
    });

    const setFieldError = (field, message) => {
        field.classList.add('is-invalid');
        let feedback = field.parentElement.querySelector('.invalid-feedback');
        if (!feedback) {
            feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            field.insertAdjacentElement('afterend', feedback);
        }
        feedback.innerHTML = `<i class="fa-solid fa-circle-exclamation"></i> ${message}`;
    };
    const clearFieldError = field => {
        field.classList.remove('is-invalid');
        field.parentElement.querySelector('.invalid-feedback')?.remove();
    };

    const bankForm = document.getElementById('walletBankForm');
    document.getElementById('addPayoutAccountModal')?.addEventListener('shown.bs.modal', () => {
        (bankForm?.querySelector('.is-invalid') || document.getElementById('walletBankCode'))?.focus();
    });
    bankForm?.addEventListener('submit', event => {
        const bankCode = bankForm.elements.bankCode;
        const holder = bankForm.elements.accountHolderName;
        const account = bankForm.elements.accountNumber;
        [bankCode, holder, account].forEach(clearFieldError);
        let valid = true;
        const normalizedBank = bankCode.value.trim();
        const normalizedHolder = holder.value.trim();
        const normalizedAccount = account.value.replace(/[\s-]/g, '');
        if (!/^[A-Za-z0-9]{2,20}$/.test(normalizedBank)) {
            setFieldError(bankCode, 'Mã ngân hàng gồm 2–20 ký tự chữ hoặc số.');
            valid = false;
        }
        if (normalizedHolder.length < 2 || normalizedHolder.length > 150) {
            setFieldError(holder, 'Vui lòng nhập tên chủ tài khoản từ 2 đến 150 ký tự.');
            valid = false;
        }
        if (!/^\d{6,30}$/.test(normalizedAccount)) {
            setFieldError(account, 'Số tài khoản phải có từ 6 đến 30 chữ số.');
            valid = false;
        }
        if (!valid) {
            event.preventDefault();
            bankForm.querySelector('.is-invalid')?.focus();
        }
    });

    const deleteForm = document.getElementById('deletePayoutAccountForm');
    document.querySelectorAll('[data-delete-account-id][data-delete-account-label]').forEach(button => {
        button.addEventListener('click', () => {
            const idInput = deleteForm?.querySelector('input[data-delete-account-id]');
            const label = document.querySelector('#deletePayoutAccountModal [data-delete-account-label]');
            if (idInput) idInput.value = button.dataset.deleteAccountId || '';
            if (label) label.textContent = button.dataset.deleteAccountLabel || 'Tài khoản đã chọn';
        });
    });
    deleteForm?.addEventListener('submit', event => {
        const reason = deleteForm.elements.reason;
        const value = reason.value.trim();
        reason.classList.remove('is-invalid');
        if (value.length < 10 || value.length > 500) {
            event.preventDefault();
            reason.classList.add('is-invalid');
            const feedback = deleteForm.querySelector('[data-delete-account-error]');
            if (feedback) feedback.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i> Vui lòng nhập lý do xóa từ 10 đến 500 ký tự.';
            reason.focus();
        }
    });

    const moneyFormatter = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 });
    const payoutForm = document.getElementById('walletPayoutForm');
    if (payoutForm) {
        const display = document.getElementById('walletPayoutAmountDisplay');
        const hidden = document.getElementById('walletPayoutAmount');
        const wrapper = display?.closest('.koc-wallet-money-input');
        const feedback = payoutForm.querySelector('[data-payout-error]');
        const available = Number(payoutForm.dataset.available || 0);
        const minimum = Number(payoutForm.dataset.minimum || 0);
        const fixedFee = Number(payoutForm.dataset.fixedFee || 0);
        const feeRate = Number(payoutForm.dataset.feeRate || 0);
        const requestedOutput = payoutForm.querySelector('[data-payout-requested]');
        const feeOutput = payoutForm.querySelector('[data-payout-fee]');
        const netOutput = payoutForm.querySelector('[data-payout-net]');

        const parseAmount = value => Number(String(value || '').replace(/[^\d]/g, '')) || 0;
        const formatMoney = value => `${moneyFormatter.format(Math.max(0, Math.round(value)))} ₫`;
        const updateCalculation = amount => {
            const fee = amount > 0 ? fixedFee + (amount * feeRate) : 0;
            if (requestedOutput) requestedOutput.textContent = formatMoney(amount);
            if (feeOutput) feeOutput.textContent = formatMoney(fee);
            if (netOutput) netOutput.textContent = formatMoney(Math.max(0, amount - fee));
        };
        const setAmount = amount => {
            const normalized = Math.max(0, Math.floor(Number(amount) || 0));
            if (display) display.value = normalized ? moneyFormatter.format(normalized) : '';
            if (hidden) hidden.value = normalized ? String(normalized) : '';
            updateCalculation(normalized);
        };
        const clearPayoutError = () => {
            display?.classList.remove('is-invalid');
            wrapper?.classList.remove('is-invalid');
        };
        const showPayoutError = message => {
            display?.classList.add('is-invalid');
            wrapper?.classList.add('is-invalid');
            if (feedback) feedback.innerHTML = `<i class="fa-solid fa-circle-exclamation"></i> ${message}`;
        };

        display?.addEventListener('input', () => {
            clearPayoutError();
            setAmount(parseAmount(display.value));
        });
        display?.addEventListener('blur', () => setAmount(parseAmount(display.value)));
        payoutForm.querySelectorAll('[data-payout-ratio]').forEach(button => {
            button.addEventListener('click', () => {
                const ratio = Number(button.dataset.payoutRatio || 0);
                const amount = ratio === 1 ? available : Math.floor((available * ratio) / 1000) * 1000;
                clearPayoutError();
                setAmount(amount);
                display?.focus();
            });
        });
        payoutForm.addEventListener('submit', event => {
            clearPayoutError();
            const amount = parseAmount(display?.value);
            if (!Number.isFinite(amount) || amount < minimum || amount > available) {
                event.preventDefault();
                showPayoutError(`Vui lòng nhập số tiền từ ${moneyFormatter.format(minimum)} ₫ đến ${moneyFormatter.format(available)} ₫.`);
                display?.focus();
                return;
            }
            if (hidden) hidden.value = String(amount);
        });
        setAmount(parseAmount(display?.value || hidden?.value));
        document.getElementById('walletPayoutModal')?.addEventListener('shown.bs.modal', () => display?.focus());
    }

    const otpForm = document.getElementById('walletPayoutOtpForm');
    if (otpForm) {
        const code = document.getElementById('walletPayoutOtpCode');
        const feedback = otpForm.querySelector('[data-payout-otp-error]');
        code?.addEventListener('input', () => {
            code.value = code.value.replace(/\D/g, '').slice(0, 6);
            code.classList.remove('is-invalid');
        });
        otpForm.addEventListener('submit', event => {
            const value = code?.value.trim() || '';
            code?.classList.remove('is-invalid');
            if (!/^\d{6}$/.test(value)) {
                event.preventDefault();
                code?.classList.add('is-invalid');
                if (feedback) feedback.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i> Vui lòng nhập đúng mã OTP gồm 6 chữ số.';
                code?.focus();
            }
        });
        document.getElementById('walletPayoutOtpModal')?.addEventListener('shown.bs.modal', () => code?.focus());
    }

    const cancelPayoutForm = document.getElementById('cancelPayoutForm');
    if (cancelPayoutForm) {
        const payoutId = cancelPayoutForm.querySelector('[data-cancel-payout-id]');
        const reason = document.getElementById('cancelPayoutReason');
        const feedback = cancelPayoutForm.querySelector('[data-cancel-payout-error]');
        const label = document.querySelector('#cancelPayoutModal [data-cancel-payout-label]');

        document.querySelectorAll('[data-cancel-payout-id][data-cancel-payout-label]').forEach(button => {
            button.addEventListener('click', () => {
                if (payoutId) payoutId.value = button.dataset.cancelPayoutId || '';
                if (label) label.textContent = button.dataset.cancelPayoutLabel || 'Yêu cầu rút tiền đã chọn';
                reason?.classList.remove('is-invalid');
                if (reason) reason.value = '';
            });
        });

        reason?.addEventListener('input', () => reason.classList.remove('is-invalid'));
        cancelPayoutForm.addEventListener('submit', event => {
            const value = reason?.value.trim() || '';
            reason?.classList.remove('is-invalid');
            if (value.length < 10 || value.length > 500) {
                event.preventDefault();
                reason?.classList.add('is-invalid');
                if (feedback) {
                    feedback.innerHTML = '<i class="fa-solid fa-circle-exclamation"></i> Vui lòng nhập lý do hủy từ 10 đến 500 ký tự.';
                }
                reason?.focus();
            }
        });
        document.getElementById('cancelPayoutModal')?.addEventListener('shown.bs.modal', () => reason?.focus());
    }

    document.querySelectorAll('[data-auto-open-modal="true"]').forEach(modal => {
        window.bootstrap?.Modal.getOrCreateInstance(modal).show();
    });
})();
