(function ($) {
    'use strict';

    function showToast(message, success) {
        if (typeof window.showToast === 'function') {
            window.showToast(message, success ? 'success' : 'error');
            return;
        }
        var toast = $('<div class="position-fixed top-0 end-0 m-3 p-3 rounded-3 text-white shadow" style="z-index:2000"></div>')
            .css('background', success ? '#0f766e' : '#dc3545')
            .text(message)
            .appendTo(document.body);
        window.setTimeout(function () { toast.fadeOut(200, function () { toast.remove(); }); }, 3500);
    }

    function closeNavigation(except) {
        $('.brand-nav-group.open').not(except).removeClass('open').find('> .brand-nav-toggle').attr('aria-expanded', 'false');
    }

    $('.brand-nav-toggle').on('click', function (event) {
        event.preventDefault();
        event.stopPropagation();
        var $group = $(this).closest('.brand-nav-group');
        var willOpen = !$group.hasClass('open');
        closeNavigation($group);
        $group.toggleClass('open', willOpen);
        $(this).attr('aria-expanded', willOpen ? 'true' : 'false');
    });

    $('.brand-nav-mobile').on('click', function () {
        var $nav = $('.brand-nav');
        var open = !$nav.hasClass('open');
        $nav.toggleClass('open', open);
        $(this).attr('aria-expanded', open ? 'true' : 'false');
    });

    $(document).on('click', function () { closeNavigation(); });

    function initializeBrandContent() {
    $('.brand-form-tab').off('click.brandContent').on('click.brandContent', function () {
        var paneId = $(this).data('pane');
        $('.brand-form-tab').removeClass('active').attr('aria-selected', 'false');
        $(this).addClass('active').attr('aria-selected', 'true');
        $('.brand-form-pane').removeClass('active');
        $('#' + paneId).addClass('active');
    });

    $('[data-brand-validation-form]').each(function () {
        var $form = $(this);
        var $summary = $form.find('[data-brand-error-summary]');
        var $submit = $form.find('[data-brand-submit]');

        function paneFor(element) {
            return $(element).closest('.brand-form-pane');
        }

        function refreshTabErrors() {
            $form.find('select.brand-native-select-hidden').each(function () {
                $(this).next('.brand-search-select').toggleClass('has-validation-error', $(this).hasClass('input-validation-error'));
            });
            $('.brand-form-tab').each(function () {
                var paneId = String($(this).data('pane') || '');
                var hasError = $('#' + paneId).find('.input-validation-error, .field-validation-error').filter(function () {
                    return $(this).is(':input') || $.trim($(this).text()).length > 0;
                }).length > 0;
                $(this).toggleClass('has-error', hasError);
            });
        }

        function revealFirstError() {
            var $first = $form.find('.input-validation-error').first();
            if (!$first.length) {
                $first = $form.find('.field-validation-error').filter(function () {
                    return $.trim($(this).text()).length > 0;
                }).first();
            }
            if (!$first.length) return false;
            var $pane = paneFor($first);
            if ($pane.length) $('.brand-form-tab[data-pane="' + $pane.attr('id') + '"]').trigger('click');
            $summary.removeClass('d-none');
            refreshTabErrors();
            window.setTimeout(function () {
                var $field = $first.is(':input') ? $first : $first.siblings(':input').first();
                ($field.length ? $field[0] : $first[0]).scrollIntoView({ behavior: 'smooth', block: 'center' });
                if ($field.length) $field.trigger('focus');
            }, 80);
            return true;
        }

        $form.on('submit', function (event) {
            if (event.isDefaultPrevented()) return;
            if (typeof $form.valid === 'function' && !$form.valid()) {
                event.preventDefault();
                revealFirstError();
                notify('Thông tin chưa hợp lệ. Vui lòng kiểm tra các ô được đánh dấu.', false);
                return;
            }
            window.TopstarForms?.guardNativeSubmit(this, event);
            $form.addClass('is-submitting');
            $submit.prop('disabled', true).find('i').attr('class', 'fa-solid fa-spinner fa-spin');
            $submit.find('span').text('Đang lưu...');
        });
        $form.on('input change', ':input', function () {
            window.setTimeout(function () {
                refreshTabErrors();
                if (!$form.find('.input-validation-error').length) $summary.addClass('d-none');
            }, 0);
        });
        revealFirstError();
    });

    $('[data-brand-requirement-target]').on('click', function () {
        var paneId = String($(this).data('brand-requirement-pane') || '');
        var targetId = String($(this).data('brand-requirement-target') || '');
        var $tab = $('.brand-form-tab[data-pane="' + paneId + '"]');
        if ($tab.length) $tab.trigger('click');
        window.setTimeout(function () {
            var target = document.getElementById(targetId);
            if (!target) return;
            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
            target.focus({ preventScroll: true });
            $(target).addClass('brand-requirement-focus');
            window.setTimeout(function () { $(target).removeClass('brand-requirement-focus'); }, 1800);
        }, 80);
    });

    function normalizeSearchText(value) {
        return String(value || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .trim();
    }

    function enhanceBrandSearchSelect(selector, searchPlaceholder) {
        var $select = $(selector);
        if (!$select.length || $select.data('brand-search-ready')) return null;
        if ($.fn.select2) return null;

        $select.data('brand-search-ready', true).addClass('brand-native-select-hidden');
        var $widget = $('<div class="brand-search-select"></div>');
        var $control = $(
            '<button type="button" class="brand-search-control" aria-haspopup="listbox" aria-expanded="false">' +
                '<span class="brand-search-value"></span>' +
                '<i class="fa-solid fa-chevron-down" aria-hidden="true"></i>' +
            '</button>'
        );
        var $panel = $('<div class="brand-search-panel"></div>');
        var $searchWrap = $('<div class="brand-search-input-wrap"><i class="fa-solid fa-magnifying-glass" aria-hidden="true"></i></div>');
        var $search = $('<input type="search" autocomplete="off" aria-label="' + searchPlaceholder + '" placeholder="' + searchPlaceholder + '" />');
        var $options = $('<div class="brand-search-options" role="listbox"></div>');
        var emptyText = 'Không tìm thấy kết quả phù hợp.';

        $select.after($widget);
        $widget.toggleClass('has-validation-error', $select.hasClass('input-validation-error'));
        $searchWrap.append($search);
        $panel.append($searchWrap, $options);
        $widget.append($control, $panel);

        function selectedLabel() {
            var option = $select.find('option:selected').get(0);
            return option ? option.text : '';
        }

        function close() {
            $widget.removeClass('open');
            $control.attr('aria-expanded', 'false');
            $search.val('');
        }

        function render() {
            var query = normalizeSearchText($search.val());
            var currentValue = String($select.val() || '');
            var matched = 0;
            $options.empty();

            $select.find('option').each(function () {
                var option = this;
                var label = String(option.text || '').trim();
                var value = String(option.value || '');
                if (!label || (query && normalizeSearchText(label).indexOf(query) === -1)) return;

                matched += 1;
                var $option = $('<button type="button" class="brand-search-option" role="option"></button>')
                    .attr('data-value', value)
                    .attr('aria-selected', value === currentValue ? 'true' : 'false')
                    .toggleClass('selected', value === currentValue)
                    .text(label);
                $options.append($option);
            });

            if (!matched) {
                $options.append($('<div class="brand-search-empty"></div>').text(emptyText));
            }
        }

        function refresh() {
            $control.find('.brand-search-value').text(selectedLabel());
            $control.prop('disabled', $select.prop('disabled'));
            render();
        }

        $control.on('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            $('.brand-search-select.open').not($widget).each(function () {
                $(this).removeClass('open').find('.brand-search-control').attr('aria-expanded', 'false');
            });
            var opening = !$widget.hasClass('open');
            $widget.toggleClass('open', opening);
            $control.attr('aria-expanded', opening ? 'true' : 'false');
            if (opening) {
                render();
                window.setTimeout(function () { $search.trigger('focus'); }, 0);
            }
        });

        $search.on('input', render).on('keydown', function (event) {
            if (event.key === 'Escape') {
                close();
                $control.trigger('focus');
            }
        });

        $options.on('click', '.brand-search-option', function () {
            $select.val(String($(this).data('value') || '')).trigger('change');
            refresh();
            close();
            $control.trigger('focus');
        });

        $select.on('change.brandSearchSelect', refresh);
        $(document).on('click.brandSearchSelect', function (event) {
            if (!$(event.target).closest($widget).length) close();
        });

        if (window.MutationObserver) {
            new MutationObserver(refresh).observe($select[0], {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['disabled']
            });
        }

        refresh();
        return { refresh: refresh, close: close };
    }

    enhanceBrandSearchSelect('#brand-edit-province', 'Tìm Tỉnh/Thành phố');
    enhanceBrandSearchSelect('#brand-edit-ward', 'Tìm Phường/Xã');

    $('#brand-edit-province').on('change', function () {
        var $province = $(this);
        var $ward = $('#brand-edit-ward');
        var provinceId = String($province.val() || '').trim();
        $ward.empty().append($('<option/>', { value: '', text: 'Chọn Phường/Xã' }));
        if (!provinceId) return;

        $ward.prop('disabled', true);
        $.get('/Brand/EditProfile?handler=WardsByProvince', { provinceId: provinceId })
            .done(function (response) {
                $.each(response && response.data ? response.data : [], function (_, ward) {
                    $ward.append($('<option/>', { value: ward.id, text: ward.name }));
                });
            })
            .fail(function () { showToast('Không thể tải danh sách Phường/Xã. Vui lòng thử lại.', false); })
            .always(function () { $ward.prop('disabled', false); });
    });

    }
    initializeBrandContent();
    document.addEventListener('topstar:workspace-ready', initializeBrandContent);

    var $passwordModal = $('#brandChangePasswordModal');
    var $passwordForm = $('#brandChangePasswordForm');

    function fieldError(name, message) {
        var $input = $passwordForm.find('[name="' + name + '"]');
        $input.closest('.brand-password-field').toggleClass('has-error', Boolean(message));
        $passwordForm.find('[data-error-for="' + name + '"]').text(message || '');
    }

    function resetPasswordForm() {
        if (!$passwordForm.length) return;
        $passwordForm[0].reset();
        $passwordForm.find('.brand-password-field').removeClass('has-error');
        $passwordForm.find('.brand-field-error').text('');
        $passwordForm.find('[data-password-toggle] i').attr('class', 'fa-regular fa-eye');
        $passwordForm.find('input[type="text"]').attr('type', 'password');
    }

    $passwordForm.on('click', '[data-password-toggle]', function () {
        var $input = $(this).siblings('input');
        var reveal = $input.attr('type') === 'password';
        $input.attr('type', reveal ? 'text' : 'password');
        $(this).find('i').attr('class', reveal ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye');
    });

    $passwordForm.on('input', 'input', function () { fieldError(this.name, ''); });

    $passwordForm.on('submit', function (event) {
        event.preventDefault();
        var currentPassword = String($passwordForm.find('[name="currentPassword"]').val() || '');
        var newPassword = String($passwordForm.find('[name="newPassword"]').val() || '');
        var confirmPassword = String($passwordForm.find('[name="confirmPassword"]').val() || '');
        var valid = true;

        ['currentPassword', 'newPassword', 'confirmPassword'].forEach(function (name) { fieldError(name, ''); });
        if (!currentPassword) { fieldError('currentPassword', 'Vui lòng nhập mật khẩu hiện tại.'); valid = false; }
        if (!newPassword) { fieldError('newPassword', 'Vui lòng nhập mật khẩu mới.'); valid = false; }
        else if (newPassword.length < 8 || newPassword.length > 128) { fieldError('newPassword', 'Mật khẩu phải từ 8 đến 128 ký tự.'); valid = false; }
        else if (!/[A-Z]/.test(newPassword) || !/[a-z]/.test(newPassword) || !/\d/.test(newPassword)) { fieldError('newPassword', 'Cần có chữ hoa, chữ thường và chữ số.'); valid = false; }
        if (!confirmPassword) { fieldError('confirmPassword', 'Vui lòng xác nhận mật khẩu mới.'); valid = false; }
        else if (newPassword !== confirmPassword) { fieldError('confirmPassword', 'Xác nhận mật khẩu không khớp.'); valid = false; }
        if (!valid) return;

        var $button = $passwordForm.find('[type="submit"]');
        $button.prop('disabled', true);
        $.ajax({
            url: $passwordForm.attr('action'),
            method: 'POST',
            data: $passwordForm.serialize()
        }).done(function (response) {
            if (response && response.success) {
                bootstrap.Modal.getOrCreateInstance($passwordModal[0]).hide();
                showToast(response.message || 'Đã cập nhật mật khẩu.', true);
                return;
            }
            $.each(response && response.errors ? response.errors : {}, function (name, message) { fieldError(name, message); });
            showToast(response && response.message ? response.message : 'Không thể cập nhật mật khẩu.', false);
        }).fail(function (xhr) {
            var response = xhr.responseJSON;
            $.each(response && response.errors ? response.errors : {}, function (name, message) { fieldError(name, message); });
            showToast(response && response.message ? response.message : 'Không thể kết nối máy chủ. Vui lòng thử lại.', false);
        }).always(function () { $button.prop('disabled', false); });
    });

    $passwordModal.on('hidden.bs.modal', resetPasswordForm);
})(jQuery);
