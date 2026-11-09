(function ($) {
  function ensureToastHost() {
    var $host = $('#adminToastHost');
    if ($host.length) return $host;
    $host = $('<div id="adminToastHost" class="admin-toast-host" aria-live="polite" aria-atomic="true"></div>');
    $('body').append($host);
    return $host;
  }

  window.AdminToast = {
    show: function (message, type, title) {
      type = ['success', 'error', 'warning', 'info'].indexOf(type) >= 0 ? type : 'info';
      var settings = {
        success: { icon: 'fa-circle-check', title: 'Thành công' },
        error: { icon: 'fa-circle-xmark', title: 'Không thành công' },
        warning: { icon: 'fa-triangle-exclamation', title: 'Cần chú ý' },
        info: { icon: 'fa-circle-info', title: 'Thông báo' }
      }[type];
      var $toast = $('<div class="toast admin-toast admin-toast-' + type +
        '" role="status" data-bs-delay="4500">' +
        '<div class="admin-toast-icon"><i class="fa-solid ' + settings.icon + '"></i></div>' +
        '<div class="admin-toast-content"><strong></strong><p></p></div>' +
        '<button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Đóng"></button></div>');
      $toast.find('strong').text(title || settings.title);
      $toast.find('p').text(message || '');
      ensureToastHost().append($toast);
      $toast.on('hidden.bs.toast', function () { $toast.remove(); });
      bootstrap.Toast.getOrCreateInstance($toast[0]).show();
    }
  };

  window.alert = function (message) {
    window.AdminToast.show(String(message || ''), 'info');
  };

  $(function () {
    // Remember safe list filters per Admin page. Explicit query strings always win.
    (function restoreAdminFilters() {
      if (location.search) return;
      var key = 'topstar.admin.filters:' + location.pathname.toLowerCase();
      var saved;
      try { saved = JSON.parse(sessionStorage.getItem(key) || '{}'); } catch (_) { saved = {}; }
      var controls = document.querySelectorAll('main input[id], main select[id]');
      controls.forEach(function (control) {
        if (!/search|filter|status|type|scope|keyword|length|pageSize|fromDate|toDate/i.test(control.id)) return;
        if (control.type === 'password' || control.type === 'hidden' || control.type === 'file') return;
        if (Object.prototype.hasOwnProperty.call(saved, control.id)) {
          if (control.type === 'checkbox') control.checked = !!saved[control.id];
          else control.value = saved[control.id];
        }
        control.addEventListener('change', save);
        if (control.tagName === 'INPUT') control.addEventListener('input', save);
      });
      function save() {
        var state = {};
        controls.forEach(function (control) {
          if (!/search|filter|status|type|scope|keyword|length|pageSize|fromDate|toDate/i.test(control.id)) return;
          if (control.type === 'password' || control.type === 'hidden' || control.type === 'file') return;
          state[control.id] = control.type === 'checkbox' ? control.checked : control.value;
        });
        try { sessionStorage.setItem(key, JSON.stringify(state)); } catch (_) { }
      }
    })();

    $('[data-admin-flash]').each(function () {
      var $flash = $(this);
      window.AdminToast.show(
        String($flash.attr('data-message') || ''),
        String($flash.attr('data-type') || 'info')
      );
      $flash.remove();
    });

    $('form').attr('novalidate', 'novalidate');

    function validationMessage(field) {
      var $field = $(field);
      var value = String($field.val() == null ? '' : $field.val()).trim();
      var label = $('label[for="' + field.id + '"]').first().text().trim() || 'Trường này';
      var customMessage = String($field.attr('data-validation-message') || '').trim();
      var min = Number($field.attr('minlength') || 0);
      var max = Number($field.attr('maxlength') || 0);
      if ($field.prop('required') && !value) return customMessage || label + ' là bắt buộc.';
      if (min && value.length < min) return customMessage || label + ' phải có ít nhất ' + min + ' ký tự.';
      if (max && value.length > max) return customMessage || label + ' không được vượt quá ' + max + ' ký tự.';
      if (field.type === 'number' && value) {
        var number = Number(value);
        var minimum = $field.attr('min');
        var maximum = $field.attr('max');
        if (!Number.isFinite(number)) return label + ' không hợp lệ.';
        if (minimum !== undefined && minimum !== null && minimum !== '' && number < Number(minimum))
          return label + ' phải từ ' + Number(minimum).toLocaleString('vi-VN') + ' trở lên.';
        if (maximum !== undefined && maximum !== null && maximum !== '' && number > Number(maximum))
          return label + ' không được vượt quá ' + Number(maximum).toLocaleString('vi-VN') + '.';
      }
      if (field.type === 'email' && value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Email không đúng định dạng.';
      return '';
    }

    function feedbackFor($form, field, $field) {
      var name = $field.attr('name');
      var $feedback = name ? $form.find('[data-valmsg-for="' + name.replace(/"/g, '\\"') + '"]').first() : $();
      if (!$feedback.length) $feedback = $field.next('.admin-client-error');
      if (!$feedback.length) {
        $feedback = $('<div class="invalid-feedback admin-client-error"></div>');
        ($field.closest('.input-group').length ? $field.closest('.input-group') : $field).after($feedback);
      }
      return $feedback;
    }

    $(document).on('submit', 'form', function (event) {
      var firstInvalid = null;
      $(this).find('input, select, textarea').filter(':enabled').each(function () {
        if (this.type === 'hidden' || this.type === 'submit' || this.type === 'button') return;
        var message = validationMessage(this);
        var $field = $(this);
        var $feedback = feedbackFor($(event.currentTarget), this, $field);
        $field.toggleClass('is-invalid', !!message).attr('aria-invalid', message ? 'true' : 'false');
        $feedback.toggleClass('field-validation-error', !!message).toggleClass('field-validation-valid', !message).text(message);
        if (message && !firstInvalid) firstInvalid = this;
      });
      if (firstInvalid) {
        event.preventDefault();
        event.stopImmediatePropagation();
        var pane = $(firstInvalid).closest('.tab-pane')[0];
        var trigger = pane ? document.querySelector('[data-bs-target="#' + pane.id + '"]') : null;
        if (trigger && window.bootstrap) bootstrap.Tab.getOrCreateInstance(trigger).show();
        window.setTimeout(function () { if(window.TopstarForms) window.TopstarForms.focusField(firstInvalid); else firstInvalid.focus(); }, trigger ? 120 : 0);
      }
    });

    $(document).on('input change', 'form input, form select, form textarea', function () {
      if (!$(this).hasClass('is-invalid')) return;
      var message = validationMessage(this);
      $(this).toggleClass('is-invalid', !!message).attr('aria-invalid', message ? 'true' : 'false');
      feedbackFor($(this).closest('form'), this, $(this))
        .toggleClass('field-validation-error', !!message).toggleClass('field-validation-valid', !message).text(message);
    });

    // Chuẩn hóa phân trang DataTables toàn Admin, kể cả bảng được khởi tạo sau admin.js.
    function normalizeAdminPagination(root) {
      var scope = root && root.querySelectorAll ? root : document;
      var selectors = [
        { selector: '.dt-paging-button .first, .dt-paging-button.first, .paginate_button.first', icon: 'fa-angles-left', label: 'Trang đầu' },
        { selector: '.dt-paging-button .previous, .dt-paging-button.previous, .paginate_button.previous', icon: 'fa-chevron-left', label: 'Trang trước' },
        { selector: '.dt-paging-button .next, .dt-paging-button.next, .paginate_button.next', icon: 'fa-chevron-right', label: 'Trang sau' },
        { selector: '.dt-paging-button .last, .dt-paging-button.last, .paginate_button.last', icon: 'fa-angles-right', label: 'Trang cuối' }
      ];

      selectors.forEach(function (item) {
        scope.querySelectorAll(item.selector).forEach(function (container) {
          var control = container.matches('button, a') ? container : container.querySelector('button, a');
          if (!control) return;
          control.innerHTML = '<i class="fa-solid ' + item.icon + '" aria-hidden="true"></i>';
          control.setAttribute('aria-label', item.label);
          control.setAttribute('title', item.label);
        });
      });
    }

    $(document).on('init.dt draw.dt page.dt', function () {
      window.requestAnimationFrame(function () { normalizeAdminPagination(document); });
    });

    var paginationObserver = new MutationObserver(function (mutations) {
      if (mutations.some(function (mutation) {
        return Array.from(mutation.addedNodes).some(function (node) {
          return node.nodeType === 1 && (node.matches?.('.dt-paging, .dataTables_paginate') || node.querySelector?.('.dt-paging, .dataTables_paginate'));
        });
      })) normalizeAdminPagination(document);
    });
    paginationObserver.observe(document.body, { childList: true, subtree: true });
    normalizeAdminPagination(document);

    // Quy chuẩn Admin: modal không tự đóng khi click backdrop hoặc nhấn Esc.
    $('.modal')
      .attr('data-bs-backdrop', 'static')
      .attr('data-bs-keyboard', 'false');

    $(document).on('click', '.admin-tab-toggle', function (e) {
      var $toggle = $(this);
      var $menu = $toggle.next('.admin-tab-menu');
      if (!$menu.length) return;

      e.preventDefault();
      e.stopPropagation();

      $('.admin-tab-menu.show').not($menu).removeClass('show');
      $menu.toggleClass('show');
      $toggle.attr('aria-expanded', $menu.hasClass('show') ? 'true' : 'false');
    });

    $(document).on('click', '.admin-nav-mobile-toggle', function () {
      var $button = $(this);
      var $nav = $('#admin-primary-nav');
      $nav.toggleClass('mobile-open');
      $button.attr('aria-expanded', $nav.hasClass('mobile-open') ? 'true' : 'false');
    });

    $(document).on('click', '#admin-primary-nav a', function () {
      if (!window.matchMedia('(max-width: 991.98px)').matches) return;
      $('#admin-primary-nav').removeClass('mobile-open');
      $('.admin-nav-mobile-toggle').attr('aria-expanded', 'false');
    });

    $(document).on('keydown', function (event) {
      if (event.key !== 'Escape') return;
      $('#admin-primary-nav').removeClass('mobile-open');
      $('.admin-nav-mobile-toggle').attr('aria-expanded', 'false');
      $('.admin-tab-menu.show').removeClass('show');
      $('.admin-tab-toggle[aria-expanded="true"]').attr('aria-expanded', 'false');
    });

    $(window).on('resize', function () {
      if (window.matchMedia('(min-width: 992px)').matches) {
        $('#admin-primary-nav').removeClass('mobile-open');
        $('.admin-nav-mobile-toggle').attr('aria-expanded', 'false');
      }
    });

    $(document).on('click', '.admin-tab-menu', function (e) {
      e.stopPropagation();
    });

    $(document).on('click', function () {
      $('.admin-tab-menu.show').removeClass('show');
      $('.admin-tab-toggle[aria-expanded="true"]').attr('aria-expanded', 'false');
    });
  });
})(jQuery);
