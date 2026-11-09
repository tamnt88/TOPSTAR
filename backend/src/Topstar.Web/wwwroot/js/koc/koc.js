(() => {
    'use strict';

    const header = document.querySelector('.koc-header');
    if (!header) return;

    const closeMenus = except => {
        header.querySelectorAll('.koc-nav-dropdown.is-open').forEach(dropdown => {
            if (dropdown === except) return;
            dropdown.classList.remove('is-open');
            dropdown.querySelector('.koc-nav-toggle')?.setAttribute('aria-expanded', 'false');
        });
    };

    header.querySelectorAll('.koc-nav-toggle').forEach(toggle => {
        toggle.addEventListener('click', event => {
            event.preventDefault();
            event.stopPropagation();
            if (toggle.disabled || toggle.classList.contains('is-disabled')) return;
            const dropdown = toggle.closest('.koc-nav-dropdown');
            const willOpen = !dropdown.classList.contains('is-open');
            closeMenus(dropdown);
            dropdown.classList.toggle('is-open', willOpen);
            toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        });
    });

    const mobileToggle = header.querySelector('.koc-mobile-toggle');
    const nav = header.querySelector('.koc-primary-nav');
    mobileToggle?.addEventListener('click', event => {
        event.stopPropagation();
        const willOpen = !nav.classList.contains('is-open');
        nav.classList.toggle('is-open', willOpen);
        mobileToggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        if (willOpen && window.matchMedia('(max-width: 991.98px)').matches) {
            const activeGroup = nav.querySelector('.koc-nav-toggle.active')?.closest('.koc-nav-dropdown');
            if (activeGroup) {
                activeGroup.classList.add('is-open');
                activeGroup.querySelector('.koc-nav-toggle')?.setAttribute('aria-expanded', 'true');
            }
        }
    });
    document.querySelector('[data-koc-open-menu]')?.addEventListener('click', () => {
        mobileToggle?.click();
        if (nav?.classList.contains('is-open')) nav.querySelector('a,button')?.focus();
    });

    header.querySelectorAll('.koc-nav-menu').forEach(menu => {
        menu.addEventListener('click', event => event.stopPropagation());
    });

    document.addEventListener('click', () => {
        closeMenus();
        nav?.classList.remove('is-open');
        mobileToggle?.setAttribute('aria-expanded', 'false');
    });

    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        closeMenus();
        nav?.classList.remove('is-open');
        mobileToggle?.setAttribute('aria-expanded', 'false');
    });

    const activateEditTabFromHash = () => {
        if (!location.hash.startsWith('#pane-')) return;
        const pane = document.querySelector(location.hash);
        if (!pane) return;
        const tabButton = document.querySelector(`[data-bs-target="${location.hash}"]`);
        if (tabButton && window.bootstrap) {
            bootstrap.Tab.getOrCreateInstance(tabButton).show();
            requestAnimationFrame(() => pane.scrollIntoView({ block: 'start' }));
        }
    };
    activateEditTabFromHash();
    window.addEventListener('hashchange', activateEditTabFromHash);

    document.querySelectorAll('.koc-social-editor-toggle').forEach(toggle => {
        toggle.addEventListener('click', () => {
            const card = toggle.closest('.koc-social-editor-card');
            const willOpen = !card.classList.contains('is-open');
            card.classList.toggle('is-open', willOpen);
            toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
        });
    });

    const ensureKocToastHost = () => {
        let host = document.querySelector('.koc-toast-host');
        if (host) return host;
        host = document.createElement('div');
        host.className = 'koc-toast-host';
        host.setAttribute('aria-live', 'polite');
        document.body.appendChild(host);
        return host;
    };

    window.KocToast = {
        show(message, type = 'info') {
            const settings = {
                success: ['fa-circle-check', 'Thành công'],
                error: ['fa-circle-xmark', 'Không thành công'],
                warning: ['fa-triangle-exclamation', 'Cần chú ý'],
                info: ['fa-circle-info', 'Thông báo']
            }[type] || ['fa-circle-info', 'Thông báo'];
            const toast = document.createElement('div');
            toast.className = `koc-toast is-${type}`;
            toast.innerHTML = `<span><i class="fa-solid ${settings[0]}"></i></span><div><strong>${settings[1]}</strong><p></p></div><button type="button" aria-label="Đóng"><i class="fa-solid fa-xmark"></i></button>`;
            toast.querySelector('p').textContent = String(message || '');
            toast.querySelector('button').addEventListener('click', () => toast.remove());
            ensureKocToastHost().appendChild(toast);
            requestAnimationFrame(() => toast.classList.add('show'));
            setTimeout(() => { toast.classList.remove('show'); setTimeout(() => toast.remove(), 180); }, 4500);
        }
    };

    document.querySelectorAll('[data-koc-flash]').forEach(flash => {
        KocToast.show(flash.dataset.message, flash.dataset.type);
        flash.remove();
    });

    const refreshReviewPage = async url => {
        const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!response.ok) throw new Error('Không thể tải danh sách đánh giá.');
        const html = await response.text();
        const next = new DOMParser().parseFromString(html, 'text/html');
        ['.koc-review-overview', '.koc-review-workspace'].forEach(selector => {
            const currentNode = document.querySelector(selector);
            const nextNode = next.querySelector(selector);
            if (currentNode && nextNode) currentNode.replaceWith(nextNode);
        });
        history.replaceState(null, '', url);
        bindReviewForms();
        bindReviewNavigation();
    };

    const bindReviewNavigation = () => {
        document.querySelectorAll('[data-review-filter]:not([data-filter-bound])').forEach(form => {
            form.dataset.filterBound = 'true';
            form.addEventListener('submit', async event => {
                event.preventDefault();
                const params = new URLSearchParams(new FormData(form));
                params.delete('pageNumber');
                params.delete('sentPageNumber');
                try {
                    await refreshReviewPage(`${location.pathname}?${params.toString()}`);
                } catch (error) {
                    KocToast.show(error?.message || 'Không thể tải danh sách đánh giá.', 'error');
                }
            });
        });
        document.querySelectorAll('.koc-review-pagination a:not([data-page-bound]), .koc-review-filter-panel a:not([data-page-bound])').forEach(link => {
            link.dataset.pageBound = 'true';
            link.addEventListener('click', async event => {
                event.preventDefault();
                try {
                    await refreshReviewPage(link.href);
                } catch (error) {
                    KocToast.show(error?.message || 'Không thể tải danh sách đánh giá.', 'error');
                }
            });
        });
    };

    const bindReviewForms = () => {
        document.querySelectorAll('.koc-review-workspace form:not([data-review-filter]):not([data-ajax-bound])').forEach(form => {
            form.dataset.ajaxBound = 'true';
            const clearFieldError = field => {
                field?.classList.remove('is-invalid');
                field?.closest('fieldset, .koc-review-reply-form, .koc-review-appeal-form, .koc-review-form')
                    ?.querySelector(`[data-error-for="${field?.name}"]`)?.remove();
            };
            const setFieldError = (field, message) => {
                if (!field) return;
                clearFieldError(field);
                field.classList.add('is-invalid');
                const error = document.createElement('div');
                error.className = 'koc-field-error';
                error.dataset.errorFor = field.name;
                error.textContent = message;
                const anchor = field.name === 'rating'
                    ? field.closest('.koc-rating-options')
                    : field;
                anchor.insertAdjacentElement('afterend', error);
                field.focus({ preventScroll: true });
                field.scrollIntoView({ behavior: 'smooth', block: 'center' });
            };
            form.querySelectorAll('textarea,input').forEach(field => {
                field.addEventListener(field.type === 'radio' ? 'change' : 'input', () => clearFieldError(field));
            });
            form.addEventListener('submit', async event => {
                event.preventDefault();
                form.querySelectorAll('.koc-field-error').forEach(error => error.remove());
                form.querySelectorAll('.is-invalid').forEach(field => field.classList.remove('is-invalid'));

                const reply = form.querySelector('textarea[name="body"]');
                const appeal = form.querySelector('textarea[name="reason"]');
                const comment = form.querySelector('textarea[name="comment"]');
                const rating = form.querySelector('input[name="rating"]:checked');
                const replyMin = reply?.minLength > 0 ? reply.minLength : 2;
                const replyMax = reply?.maxLength > 0 ? reply.maxLength : 2000;
                if (reply && (reply.value.trim().length < replyMin || reply.value.trim().length > replyMax)) {
                    setFieldError(reply, `Vui lòng nhập nội dung từ ${replyMin} đến ${replyMax.toLocaleString('vi-VN')} ký tự.`);
                    return;
                }
                const appealMin = appeal?.minLength > 0 ? appeal.minLength : 10;
                const appealMax = appeal?.maxLength > 0 ? appeal.maxLength : 2000;
                if (appeal && (appeal.value.trim().length < appealMin || appeal.value.trim().length > appealMax)) {
                    setFieldError(appeal, `Vui lòng nhập lý do từ ${appealMin} đến ${appealMax.toLocaleString('vi-VN')} ký tự.`);
                    return;
                }
                if (form.querySelector('input[name="rating"]') && !rating) {
                    setFieldError(form.querySelector('input[name="rating"]'), 'Vui lòng chọn mức độ hài lòng từ 1 đến 5 sao.');
                    return;
                }
                if (comment && comment.value.trim().length > 2000) {
                    setFieldError(comment, 'Nhận xét không được vượt quá 2.000 ký tự.');
                    return;
                }

                const button = form.querySelector('button[type="submit"],button:not([type])');
                if (button?.disabled) return;
                const oldHtml = button?.innerHTML;
                if (button) {
                    button.disabled = true;
                    button.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i>Đang xử lý';
                }
                try {
                    const response = await fetch(form.action, {
                        method: 'POST',
                        body: new FormData(form),
                        headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
                    });
                    const contentType = response.headers.get('content-type') || '';
                    if (!contentType.includes('application/json')) {
                        throw new Error(response.redirected
                            ? 'Phiên làm việc hoặc điều kiện hồ sơ đã thay đổi. Vui lòng tải lại trang.'
                            : `Máy chủ trả về phản hồi không hợp lệ (${response.status}).`);
                    }
                    const result = await response.json();
                    KocToast.show(result.message, result.success ? 'success' : 'error');
                    if (result.success) {
                        const activeTab = form.closest('#review-pane-sent') ? 'sent' : 'received';
                        const params = new URLSearchParams(location.search);
                        params.set('tab', activeTab);
                        await refreshReviewPage(`${location.pathname}?${params.toString()}`);
                    }
                } catch (error) {
                    KocToast.show(error?.message || 'Không thể kết nối máy chủ. Vui lòng thử lại.', 'error');
                } finally {
                    if (button?.isConnected) {
                        button.disabled = false;
                        button.innerHTML = oldHtml;
                    }
                }
            });
        });
    };
    bindReviewForms();
    bindReviewNavigation();
})();

(() => {
    const tabs = document.querySelectorAll('.koc-profile-tabs [data-bs-toggle="tab"]');
    if (!tabs.length || typeof bootstrap === 'undefined') return;
    const activateFromHash = () => {
        const target = location.hash;
        const button = target ? document.querySelector(`.koc-profile-tabs [data-bs-target="${target}"]`) : null;
        if (button) bootstrap.Tab.getOrCreateInstance(button).show();
    };
    activateFromHash();
    tabs.forEach(button => button.addEventListener('shown.bs.tab', event => {
        const target = event.target.getAttribute('data-bs-target');
        if (target) history.replaceState(null, '', `${location.pathname}${location.search}${target}`);
    }));
    window.addEventListener('hashchange', activateFromHash);
})();

(() => {
    'use strict';

    const modalElement = document.getElementById('kocChangePasswordModal');
    const form = document.getElementById('kocChangePasswordForm');
    if (!modalElement || !form) return;

    const fields = {
        currentPassword: form.elements.currentPassword,
        newPassword: form.elements.newPassword,
        confirmPassword: form.elements.confirmPassword
    };

    const clearError = name => {
        const field = fields[name];
        if (!field) return;
        field.classList.remove('is-invalid');
        form.querySelector(`[data-error-for="${name}"]`).textContent = '';
    };

    const setError = (name, message) => {
        const field = fields[name];
        if (!field) return;
        field.classList.add('is-invalid');
        form.querySelector(`[data-error-for="${name}"]`).textContent = message;
    };

    Object.entries(fields).forEach(([name, field]) => {
        field?.addEventListener('input', () => clearError(name));
    });

    form.querySelectorAll('[data-password-toggle]').forEach(button => {
        button.addEventListener('click', () => {
            const input = button.closest('.koc-password-control')?.querySelector('input');
            if (!input) return;
            const visible = input.type === 'text';
            input.type = visible ? 'password' : 'text';
            button.setAttribute('aria-label', visible ? 'Hiện mật khẩu' : 'Ẩn mật khẩu');
            button.querySelector('i').className = visible ? 'fa-regular fa-eye' : 'fa-regular fa-eye-slash';
        });
    });

    const validate = () => {
        Object.keys(fields).forEach(clearError);
        let valid = true;
        const currentPassword = fields.currentPassword.value;
        const newPassword = fields.newPassword.value;
        const confirmPassword = fields.confirmPassword.value;

        if (!currentPassword) {
            setError('currentPassword', 'Vui lòng nhập mật khẩu hiện tại.');
            valid = false;
        }
        if (!newPassword) {
            setError('newPassword', 'Vui lòng nhập mật khẩu mới.');
            valid = false;
        } else if (newPassword.length < 8 || newPassword.length > 128) {
            setError('newPassword', 'Mật khẩu mới phải từ 8 đến 128 ký tự.');
            valid = false;
        } else if (!/[A-Z]/.test(newPassword) || !/[a-z]/.test(newPassword) || !/\d/.test(newPassword)) {
            setError('newPassword', 'Mật khẩu phải có chữ hoa, chữ thường và chữ số.');
            valid = false;
        } else if (newPassword === currentPassword) {
            setError('newPassword', 'Mật khẩu mới phải khác mật khẩu hiện tại.');
            valid = false;
        }
        if (!confirmPassword) {
            setError('confirmPassword', 'Vui lòng xác nhận mật khẩu mới.');
            valid = false;
        } else if (confirmPassword !== newPassword) {
            setError('confirmPassword', 'Xác nhận mật khẩu không khớp.');
            valid = false;
        }

        if (!valid) {
            form.querySelector('.is-invalid')?.focus();
        }
        return valid;
    };

    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (!validate()) return;

        const submit = form.querySelector('button[type="submit"]');
        const oldHtml = submit.innerHTML;
        submit.disabled = true;
        submit.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i>Đang xử lý';

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                }
            });
            const contentType = response.headers.get('content-type') || '';
            if (!contentType.includes('application/json')) {
                throw new Error(response.status === 401 || response.redirected
                    ? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
                    : 'Máy chủ trả về phản hồi không hợp lệ.');
            }

            const result = await response.json();
            if (result.errors) {
                Object.entries(result.errors).forEach(([name, message]) => setError(name, message));
            }
            if (!result.success) {
                window.KocToast?.show(result.message || 'Không thể đổi mật khẩu.', 'error');
                form.querySelector('.is-invalid')?.focus();
                return;
            }

            window.KocToast?.show(result.message || 'Đổi mật khẩu thành công.', 'success');
            bootstrap.Modal.getOrCreateInstance(modalElement).hide();
        } catch (error) {
            window.KocToast?.show(error?.message || 'Không thể kết nối máy chủ. Vui lòng thử lại.', 'error');
        } finally {
            submit.disabled = false;
            submit.innerHTML = oldHtml;
        }
    });

    modalElement.addEventListener('show.bs.modal', () => {
        const accountToggle = document.querySelector('.koc-account-toggle');
        if (accountToggle && window.bootstrap) {
            bootstrap.Dropdown.getOrCreateInstance(accountToggle).hide();
        }
    });

    modalElement.addEventListener('hidden.bs.modal', () => {
        form.reset();
        Object.keys(fields).forEach(clearError);
        form.querySelectorAll('[data-password-toggle]').forEach(button => {
            const input = button.closest('.koc-password-control')?.querySelector('input');
            if (input) input.type = 'password';
            button.setAttribute('aria-label', 'Hiện mật khẩu');
            button.querySelector('i').className = 'fa-regular fa-eye';
        });
    });
})();
