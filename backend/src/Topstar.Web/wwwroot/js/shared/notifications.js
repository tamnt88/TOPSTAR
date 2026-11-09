((window, $) => {
    'use strict';
    if (!$ || window.TopstarNotify) return;

    const iconMap = { success: 'fa-circle-check', error: 'fa-circle-xmark', warning: 'fa-triangle-exclamation', info: 'fa-circle-info' };
    const titleMap = { success: 'Thành công', error: 'Không thể thực hiện', warning: 'Cần lưu ý', info: 'Thông báo' };
    let confirmResolver = null;

    const ensureUi = () => {
        if (!$('.ts-notify-stack').length) $('body').append('<div class="ts-notify-stack" aria-live="polite"></div>');
        if (!$('.ts-confirm-backdrop').length) {
            $('body').append(`<div class="ts-confirm-backdrop" role="presentation"><section class="ts-confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="tsConfirmTitle" aria-describedby="tsConfirmMessage"><div class="ts-confirm-body"><span class="ts-confirm-icon"><i class="fa-solid fa-triangle-exclamation"></i></span><h2 id="tsConfirmTitle">Xác nhận thao tác</h2><p id="tsConfirmMessage"></p></div><div class="ts-confirm-actions"><button type="button" class="ts-confirm-cancel">Hủy</button><button type="button" class="ts-confirm-accept">Xác nhận</button></div></section></div>`);
        }
    };

    const toast = (message, type = 'info', options = {}) => {
        ensureUi();
        const safeType = iconMap[type] ? type : 'info';
        const $toast = $('<article/>', { class: `ts-notify-toast is-${safeType}`, role: safeType === 'error' ? 'alert' : 'status' });
        $toast.append($('<span/>', { class: 'ts-notify-toast__icon' }).append($('<i/>', { class: `fa-solid ${iconMap[safeType]}` })));
        const $copy = $('<div/>').append($('<strong/>').text(options.title || titleMap[safeType]), $('<p/>').text(message || ''));
        const $close = $('<button/>', { type: 'button', 'aria-label': 'Đóng thông báo' }).append('<i class="fa-solid fa-xmark"></i>');
        $toast.append($copy, $close).appendTo('.ts-notify-stack');
        requestAnimationFrame(() => $toast.addClass('is-visible'));
        const close = () => { $toast.addClass('is-leaving').removeClass('is-visible'); setTimeout(() => $toast.remove(), 190); };
        $close.on('click', close);
        setTimeout(close, options.duration || (safeType === 'error' ? 6500 : 4500));
        return $toast;
    };

    const closeConfirm = result => {
        $('.ts-confirm-backdrop').removeClass('is-open');
        const resolver = confirmResolver;
        confirmResolver = null;
        resolver?.(result);
    };

    const confirm = (message, options = {}) => {
        ensureUi();
        if (confirmResolver) closeConfirm(false);
        const $backdrop = $('.ts-confirm-backdrop');
        const $dialog = $backdrop.find('.ts-confirm-dialog').toggleClass('is-danger', options.tone === 'danger');
        $dialog.find('h2').text(options.title || 'Xác nhận thao tác');
        $dialog.find('p').text(message || 'Bạn có chắc chắn muốn tiếp tục?');
        $dialog.find('.ts-confirm-accept').text(options.confirmText || 'Xác nhận');
        $dialog.find('.ts-confirm-cancel').text(options.cancelText || 'Hủy');
        $backdrop.addClass('is-open');
        setTimeout(() => $dialog.find('.ts-confirm-accept').trigger('focus'));
        return new Promise(resolve => { confirmResolver = resolve; });
    };

    $(document)
        .on('click', '.ts-confirm-cancel', () => closeConfirm(false))
        .on('click', '.ts-confirm-accept', () => closeConfirm(true))
        .on('click', '.ts-confirm-backdrop', event => { if (event.target === event.currentTarget) closeConfirm(false); })
        .on('keydown', event => { if (event.key === 'Escape' && $('.ts-confirm-backdrop').hasClass('is-open')) closeConfirm(false); })
        .on('click', 'button[data-confirm], input[type="submit"][data-confirm]', function (event) {
            const button = this;
            if (button.dataset.confirmApproved === 'true') { delete button.dataset.confirmApproved; return; }
            event.preventDefault();
            const form = button.form;
            if (form && !form.checkValidity()) { form.reportValidity(); return; }
            confirm(button.dataset.confirm, {
                title: button.dataset.confirmTitle,
                confirmText: button.dataset.confirmButton,
                tone: button.dataset.confirmTone
            }).then(approved => {
                if (!approved) return;
                button.dataset.confirmApproved = 'true';
                button.click();
            });
        })
        .on('submit', 'form[data-confirm]', function (event) {
            const form = this;
            if (form.dataset.confirmApproved === 'true') { delete form.dataset.confirmApproved; return; }
            event.preventDefault();
            if (!form.checkValidity()) { form.reportValidity(); return; }
            confirm(form.dataset.confirm, {
                title: form.dataset.confirmTitle,
                confirmText: form.dataset.confirmButton,
                tone: form.dataset.confirmTone
            }).then(approved => {
                if (!approved) return;
                form.dataset.confirmApproved = 'true';
                form.requestSubmit(event.originalEvent?.submitter || undefined);
            });
        });

    window.TopstarNotify = {
        toast,
        success: (message, options) => toast(message, 'success', options),
        error: (message, options) => toast(message, 'error', options),
        warning: (message, options) => toast(message, 'warning', options),
        info: (message, options) => toast(message, 'info', options),
        confirm
    };

    // One-time messages rendered by Razor use the shared toast UI. Persistent
    // business-state warnings intentionally remain visible on the page.
    const consumeServerNotifications = root => {
        $(root || document).find('[data-ts-notification]').addBack('[data-ts-notification]').each(function () {
            const $message = $(this);
            if ($message.data('tsNotificationConsumed')) return;
            $message.data('tsNotificationConsumed', true);
            const type = $message.attr('data-ts-notification') || 'info';
            const text = $message.attr('data-message') || $message.text().trim();
            const title = $message.attr('data-title');
            $message.remove();
            if (text) toast(text, type, title ? { title } : {});
        });
    };

    consumeServerNotifications(document);
    $(() => consumeServerNotifications(document));

    // Also supports partial page updates that append a server-message marker.
    new MutationObserver(mutations => {
        mutations.forEach(mutation => mutation.addedNodes.forEach(node => {
            if (node.nodeType === Node.ELEMENT_NODE) consumeServerNotifications(node);
        }));
    }).observe(document.body, { childList: true, subtree: true });

    // Compatibility layer for legacy pages and third-party widgets. Calls to
    // browser alert/confirm are rendered by the shared jQuery UI instead.
    window.alert = message => { toast(String(message ?? ''), 'warning'); };
    window.confirm = message => {
        const trigger = window.event?.currentTarget;
        if (!(trigger instanceof HTMLElement)) {
            confirm(String(message ?? '')).then(() => {});
            return false;
        }
        if (trigger.dataset.tsLegacyConfirmApproved === 'true') {
            delete trigger.dataset.tsLegacyConfirmApproved;
            return true;
        }
        confirm(String(message ?? '')).then(approved => {
            if (!approved) return;
            trigger.dataset.tsLegacyConfirmApproved = 'true';
            if (trigger instanceof HTMLFormElement) trigger.requestSubmit();
            else trigger.click();
        });
        return false;
    };
})(window, window.jQuery);
