(function ($) {
  'use strict';

  $(function () {
    var $table = $('#accountDataTable');
    if (!$table.length || !$.fn.DataTable) return;

    var accountType = String($table.data('account-type') || 'Brand').toUpperCase();
    var ajaxUrl = $table.data('ajax-url');
    var detailUrl = $table.data('detail-url');
    var $search = $('#accountSearch');
    var $profileStatus = $('#accountProfileStatus');
    var $userStatus = $('#accountUserStatus');
    var $location = $('#accountLocation');
    var $violation = $('#accountViolation');
    var $sort = $('#accountSort');
    var $pageSize = $('#accountPageSize');
    var initialQuery = new URLSearchParams(window.location.search).get('query');
    if (initialQuery) $search.val(initialQuery.slice(0, 120));

    function escapeHtml(value) {
      return $('<div>').text(value == null ? '' : String(value)).html();
    }

    function statusLabel(value) {
      var labels = {
        Approved: 'Đã duyệt',
        PendingReview: 'Chờ duyệt',
        NeedUpdate: 'Cần bổ sung',
        Rejected: 'Đã từ chối',
        Locked: 'Đã khóa',
        Active: 'Đang hoạt động'
      };
      return labels[value] || value || 'Không xác định';
    }

    function statusClass(value) {
      if (value === 'Approved' || value === 'Active') return 'account-status-active';
      if (value === 'PendingReview') return 'account-status-pending';
      if (value === 'NeedUpdate') return 'account-status-warning';
      if (value === 'Rejected' || value === 'Locked') return 'account-status-danger';
      return 'account-status-neutral';
    }

    function violationStatusLabel(value) {
      var labels = {
        Active: 'Đang hiệu lực',
        Revoked: 'Đã thu hồi',
        Resolved: 'Đã xử lý'
      };
      return labels[value] || value || 'Không xác định';
    }

    function formatDate(value) {
      var date = new Date(value);
      if (Number.isNaN(date.getTime())) return '—';
      return new Intl.DateTimeFormat('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      }).format(date);
    }

    function identityHtml(row) {
      var name = row.name || (accountType === 'KOC' ? 'KOC' : 'Brand');
      var avatar = accountType === 'KOC'
        ? escapeHtml(name.trim().charAt(0).toUpperCase() || 'K')
        : '<i class="fa-regular fa-building"></i>';
      var avatarClass = accountType === 'KOC' ? 'koc-avatar' : 'brand-avatar';
      return '<button type="button" class="account-identity account-identity-button js-account-view" data-user-id="' + escapeHtml(row.userId) + '">' +
        '<span class="account-avatar ' + avatarClass + '">' + avatar + '</span>' +
        '<div><strong>' + escapeHtml(name) + '</strong><small>' + escapeHtml(row.email) + '</small></div>' +
        '</button>';
    }

    function performanceHtml(row) {
      if (accountType === 'KOC') {
        return '<strong>' + Number(row.performanceCount || 0).toLocaleString('vi-VN') + ' hợp tác</strong>' +
          '<small>' + Number(row.followers || 0).toLocaleString('vi-VN') + ' followers · ' +
          Number(row.effectiveScore || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + ' điểm</small>';
      }
      return '<strong>' + Number(row.campaignCount || 0).toLocaleString('vi-VN') + '</strong>' +
        '<small>' + Number(row.activeCampaignCount || 0).toLocaleString('vi-VN') + ' đang chạy · ' + Number(row.performanceCount || 0).toLocaleString('vi-VN') + ' hoàn tất</small>';
    }

    function accountStateHtml(row) {
      var isLocked = row.userStatus === 'Locked';
      var canLogin = !isLocked && row.emailConfirmed;
      var accessLabel = isLocked ? 'Đã khóa — KOC không thể đăng nhập' : (!row.emailConfirmed ? 'Email chưa xác minh — KOC chưa thể đăng nhập' : 'Đang hoạt động — KOC có thể đăng nhập');
      var access = '<span class="account-state-icon ' + (isLocked ? 'is-danger' : (canLogin ? 'is-success' : 'is-warning')) +
        '" title="' + accessLabel + '" aria-label="' + accessLabel + '"><i class="fa-solid ' +
        (canLogin ? 'fa-lock-open' : 'fa-lock') + '"></i></span>';
      var activation = row.emailConfirmed
        ? '<span class="account-state-icon is-success" title="Email đã xác minh" aria-label="Email đã xác minh"><i class="fa-solid fa-envelope-circle-check"></i></span>'
        : '<span class="account-state-icon is-warning" title="Email chưa xác minh" aria-label="Email chưa xác minh"><i class="fa-solid fa-envelope"></i></span>';
      return '<div class="account-quick-state">' + access + activation + '</div>';
    }

    function profileStateHtml(row) {
      var icons = { Approved:'fa-circle-check', PendingReview:'fa-clock', NeedUpdate:'fa-pen-to-square', Rejected:'fa-circle-xmark', Locked:'fa-lock' };
      return '<div class="account-profile-state"><a class="account-state-icon ' + statusClass(row.profileStatus) +
        ' account-profile-review-link" href="/Admin/Accounts/Center/' + encodeURIComponent(accountType) + '/' +
        encodeURIComponent(row.userId) + '#profile" title="Hồ sơ: ' + escapeHtml(statusLabel(row.profileStatus)) +
        ' — Mở hồ sơ trong Trung tâm" aria-label="Mở hồ sơ ' + escapeHtml(statusLabel(row.profileStatus)) +
        ' trong Trung tâm"><i class="fa-solid ' + (icons[row.profileStatus] || 'fa-circle-info') + '"></i></a></div>';
    }

    function actionHtml(row) {
      return '<div class="account-row-actions"><a class="account-center-button" href="/Admin/Accounts/Center/' + encodeURIComponent(accountType) + '/' + encodeURIComponent(row.userId) + '" title="Mở trang quản trị ' + escapeHtml(row.name) + '" aria-label="Mở trang quản trị ' + escapeHtml(row.name) + '"><i class="fa-solid fa-user-gear"></i></a>' +
        (row.userStatus === 'Active' ? '<button type="button" class="account-center-button js-account-impersonate" data-user-id="' + escapeHtml(row.userId) + '" data-user-name="' + escapeHtml(row.name) + '" title="Hỗ trợ với vai trò ' + escapeHtml(row.name) + '"><i class="fa-solid fa-right-to-bracket"></i></button>' : '') +
        '<button type="button" class="account-delete-button js-account-delete" data-user-id="' + escapeHtml(row.userId) + '" data-user-name="' + escapeHtml(row.name) + '" title="Xóa tài khoản ' + escapeHtml(row.name) + '"><i class="fa-solid fa-trash-can"></i></button></div>';
    }

    function violationHtml(row) {
      var count = Number(row.activeViolationCount || 0);
      if (!count) return '<span class="account-state-icon is-success" title="Không có vi phạm hiệu lực" aria-label="Không có vi phạm hiệu lực"><i class="fa-solid fa-shield-halved"></i></span>';
      return '<a class="account-state-icon is-danger" title="' + count + ' vi phạm đang hiệu lực — Bấm để xem" aria-label="' + count + ' vi phạm đang hiệu lực" href="/Admin/Violations?accountUserId=' + encodeURIComponent(row.userId) + '&returnType=' + encodeURIComponent(accountType) + '&accountName=' + encodeURIComponent(row.name || '') + '&accountEmail=' + encodeURIComponent(row.email || '') + '"><i class="fa-solid fa-triangle-exclamation"></i><b>' + count + '</b></a>';
    }

    var table = $table.DataTable(TopstarLists.configure($table[0], {
      processing: true,
      serverSide: true,
      autoWidth: false,
      searching: false,
      ordering: false,
      pageLength: Number($pageSize.val()) || 20,
      lengthChange: false,
      ajax: {
        url: ajaxUrl,
        type: 'POST',
        headers: {
          RequestVerificationToken: $('.account-table-wrap input[name="__RequestVerificationToken"]').val()
        },
        data: function (data) {
          data.query = $search.val();
          data.profileStatus = $profileStatus.val();
          data.userStatus = $userStatus.val();
          data.location = $location.val();
          var violationValue = $violation.val();
          data.hasViolation = violationValue === '' ? null : violationValue === 'true';
          data.sort = $sort.val();
        },
        error: function () {
          $('.dt-processing').hide();
          window.AdminToast.show('Không thể tải dữ liệu tài khoản. Vui lòng thử lại.', 'error');
        }
      },
      columns: [
        {
          data: null,
          width: '58px',
          render: function (_data, _type, _row, meta) {
            return meta.settings._iDisplayStart + meta.row + 1;
          }
        },
        { data: null, render: function (_data, _type, row) { return identityHtml(row); } },
        { data: 'location', defaultContent: 'Chưa cập nhật', render: function (value) { return escapeHtml(value || 'Chưa cập nhật'); } },
        { data: null, render: function (_data, _type, row) { return performanceHtml(row); } },
        { data: 'createdAtUtc', render: function (value) { return formatDate(value); } },
        { data: null, render: function (_data, _type, row) { return accountStateHtml(row); } },
        { data: null, render: function (_data, _type, row) { return profileStateHtml(row); } },
        { data: null, render: function (_data, _type, row) { return violationHtml(row); } },
        { data: null, width: '132px', className: 'account-col-actions', orderable: false, searchable: false, render: function (_data, _type, row) { return actionHtml(row); } }
      ],
      language: {
        processing: 'Đang tải dữ liệu...',
        emptyTable: 'Chưa có tài khoản.',
        zeroRecords: 'Không tìm thấy tài khoản phù hợp.',
        info: 'Hiển thị _START_–_END_ trong _TOTAL_ tài khoản',
        infoEmpty: 'Không có dữ liệu',
        infoFiltered: '',
        paginate: {
          first: 'Đầu',
          previous: '<i class="fa-solid fa-chevron-left"></i>',
          next: '<i class="fa-solid fa-chevron-right"></i>',
          last: 'Cuối'
        }
      },
      layout: {
        topStart: null,
        topEnd: null,
        bottomStart: 'info',
        bottomEnd: 'paging'
      }
    }, { query: '#accountSearch', profileStatus: '#accountProfileStatus', userStatus: '#accountUserStatus', location: '#accountLocation', hasViolation: '#accountViolation', sort: '#accountSort' }, '#accountPageSize'));

    $table.on('xhr.dt', function (_event, _settings, json) {
      var summary = json && json.summary ? json.summary : {};
      $('#accountSummaryTotal').text(Number(summary.total || 0).toLocaleString('vi-VN'));
      $('#accountSummaryActive').text(Number(summary.active || 0).toLocaleString('vi-VN'));
      $('#accountSummaryPending').text(Number(summary.pending || 0).toLocaleString('vi-VN'));
      $('#accountSummaryLocked').text(Number(summary.locked || 0).toLocaleString('vi-VN'));
      $('#accountSummaryUnverified').text(Number(summary.unverified || 0).toLocaleString('vi-VN'));
      $('#accountSummaryViolations').text(Number(summary.withViolations || 0).toLocaleString('vi-VN'));
    });

    function reloadFromFirstPage() {
      table.ajax.reload(null, true);
    }

    $('#accountApplyFilter').on('click', reloadFromFirstPage);
    $search.on('keydown', function (event) {
      if (event.key === 'Enter') {
        event.preventDefault();
        reloadFromFirstPage();
      }
    });
    $profileStatus.add($userStatus).add($violation).add($sort).on('change', reloadFromFirstPage);
    $location.on('keydown', function (event) { if (event.key === 'Enter') { event.preventDefault(); reloadFromFirstPage(); } });
    $pageSize.on('change', function () {
      table.page.len(Number(this.value) || 20).draw();
    });
    $('#accountResetFilter').on('click', function () {
      $search.val('');
      $profileStatus.val('');
      $userStatus.val('');
      $location.val('');
      $violation.val('');
      $sort.val('newest');
      $pageSize.val('20');
      table.page.len(20);
      reloadFromFirstPage();
    });

    function money(value) { return Number(value || 0).toLocaleString('vi-VN') + ' đ'; }
    function dateTime(value) { return value ? new Date(value).toLocaleString('vi-VN') : '—'; }
    function renderDetail(response, row) {
      var account = response.account || {}, campaigns = response.campaigns || [], fundings = response.fundings || [], violations = response.violations || [];
      $('#accountDetailModalTitle').text(account.displayName || row.name || 'Chi tiết tài khoản');
      $('#accountDetailEmail').text('Trung tâm hỗ trợ ' + accountType + ' · ' + (account.email || row.email || 'Chưa có email'));
      $('[data-account-panel="overview"]').html('<div class="account-detail-summary">' +
        '<article><small>Trạng thái tài khoản</small><strong>' + escapeHtml(statusLabel(account.userStatus)) + '</strong></article>' +
        '<article><small>Trạng thái hồ sơ</small><strong>' + escapeHtml(statusLabel(account.profileStatus)) + '</strong></article>' +
        '<article><small>Ngành hàng</small><strong>' + escapeHtml((response.profile && response.profile.industry) || row.location || 'Chưa cập nhật') + '</strong></article>' +
        '<article><small>Ngày tham gia</small><strong>' + escapeHtml(dateTime(account.createdAtUtc)) + '</strong></article>' +
        '<article><small>Tổng chiến dịch</small><strong>' + Number(row.campaignCount || 0).toLocaleString('vi-VN') + '</strong></article>' +
        '<article><small>Đang vận hành</small><strong>' + Number(row.activeCampaignCount || 0).toLocaleString('vi-VN') + '</strong></article>' +
        '<article><small>Tổng ngân sách</small><strong>' + escapeHtml(money(row.totalCampaignBudget)) + '</strong></article>' +
        '<article><small>Vi phạm hiệu lực</small><strong>' + Number(row.activeViolationCount || 0).toLocaleString('vi-VN') + '</strong></article></div>' +
        '<div class="account-detail-actions"><button type="button" class="btn btn-admin-primary js-account-edit-from-detail"><i class="fa-solid fa-pen"></i> Sửa thông tin</button></div>');
      $('[data-account-panel="profile"]').html('<div class="account-detail-summary"><article><small>Trạng thái hồ sơ</small><strong>' + escapeHtml(statusLabel(account.profileStatus)) + '</strong></article><article><small>Khu vực / ngành hàng</small><strong>' + escapeHtml((response.profile && response.profile.industry) || row.location || 'Chưa cập nhật') + '</strong></article><article><small>Hiệu suất</small><strong>' + Number(row.performanceCount || 0).toLocaleString('vi-VN') + ' hợp tác</strong></article><article><small>Điểm hiệu quả</small><strong>' + Number(row.effectiveScore || 0).toLocaleString('vi-VN', {maximumFractionDigits:2}) + '</strong></article></div><div class="account-detail-actions"><a class="btn btn-outline-primary" href="/Admin/Accounts/Center/' + encodeURIComponent(accountType) + '/' + encodeURIComponent(row.userId) + '#profile"><i class="fa-regular fa-id-card"></i> Mở hồ sơ đầy đủ</a></div>');
      var canLock = row.userStatus === 'Active' && row.profileStatus === 'Approved', canUnlock = row.userStatus === 'Locked' && row.profileStatus === 'Locked';
      var supportActions = '<a class="btn btn-outline-primary" href="/Admin/Accounts/Security/' + encodeURIComponent(row.userId) + '"><i class="fa-solid fa-shield-halved"></i> Bảo mật nâng cao</a>' + (accountType === 'KOC' && !account.emailConfirmed ? '<button type="button" class="btn btn-outline-success js-account-activate" data-user-id="' + escapeHtml(row.userId) + '" data-user-name="' + escapeHtml(row.name) + '"><i class="fa-solid fa-envelope-circle-check"></i> Xác minh email thay KOC</button>' : '') + ((canLock || canUnlock) ? '<button type="button" class="btn ' + (canUnlock ? 'btn-outline-success' : 'btn-outline-danger') + ' js-account-lock" data-user-id="' + escapeHtml(row.userId) + '" data-user-name="' + escapeHtml(row.name) + '" data-locked="' + (canLock ? 'true' : 'false') + '"><i class="fa-solid ' + (canUnlock ? 'fa-lock-open' : 'fa-lock') + '"></i> ' + (canUnlock ? 'Khôi phục tài khoản' : 'Khóa tài khoản') + '</button>' : '');
      $('[data-account-panel="security"]').html('<div class="account-detail-summary"><article><small>Email xác minh</small><strong>' + (account.emailConfirmed ? 'Đã xác minh' : 'Chưa xác minh') + '</strong></article><article><small>Điện thoại xác minh</small><strong>' + (account.phoneConfirmed ? 'Đã xác minh' : 'Chưa xác minh') + '</strong></article><article><small>Đăng nhập gần nhất</small><strong>' + escapeHtml(dateTime(account.lastLoginAtUtc)) + '</strong></article><article><small>Phiên đăng nhập</small><strong>' + Number((account.sessions || []).filter(function (x) { return !x.revokedAtUtc; }).length) + ' đang hoạt động</strong></article></div><div class="account-detail-actions">' + supportActions + '</div>');
      var violationRows = violations.map(function (item) { return '<article class="account-detail-event"><span class="is-danger"><i class="fa-solid fa-triangle-exclamation"></i></span><div><strong>' + escapeHtml(item.category) + '</strong><small>' + escapeHtml(item.severity) + ' · ' + item.penaltyPoints + ' điểm · ' + dateTime(item.createdAtUtc) + '</small></div><b>' + escapeHtml(violationStatusLabel(item.status)) + '</b></article>'; }).join('');
      $('[data-account-panel="violations"]').html((violationRows || '<div class="account-detail-empty"><i class="fa-solid fa-circle-check"></i> Tài khoản chưa có vi phạm.</div>') + '<div class="account-detail-actions"><a class="btn btn-outline-danger" href="/Admin/Violations?accountUserId=' + encodeURIComponent(row.userId) + '&returnType=' + encodeURIComponent(accountType) + '&accountName=' + encodeURIComponent(row.name || '') + '&accountEmail=' + encodeURIComponent(row.email || '') + '"><i class="fa-solid fa-triangle-exclamation"></i> Quản lý vi phạm</a></div>');
      var historyRows = (account.auditLogs || []).map(function (item) { return '<article class="account-detail-event"><span><i class="fa-solid fa-clock-rotate-left"></i></span><div><strong>' + escapeHtml(item.actionName) + '</strong><small>' + escapeHtml(item.actorEmail || 'Hệ thống') + ' · ' + dateTime(item.createdAtUtc) + '</small></div></article>'; }).join('');
      $('[data-account-panel="history"]').html(historyRows || '<div class="account-detail-empty">Chưa có lịch sử audit.</div>');
      $('#accountDetailLoading').hide(); $('#accountDetailContent').prop('hidden', false);
      $('#accountDetailCenter').data('row', row);
    }

    $(document).on('click', '.js-account-view', function () {
      var userId = String($(this).data('user-id') || '');
      var row = table.rows().data().toArray().find(function (item) { return String(item.userId) === userId; }) || {};
      $('[data-account-tab]').removeClass('is-active').attr('aria-selected', 'false');
      $('[data-account-tab="overview"]').addClass('is-active').attr('aria-selected', 'true');
      $('[data-account-panel]').removeClass('is-active');
      $('[data-account-panel="overview"]').addClass('is-active');
      $('#accountDetailContent').prop('hidden', true);
      $('#accountDetailLoading').html('<span class="spinner-border spinner-border-sm"></span> Đang tải dữ liệu...').show();
      var center = document.getElementById('accountDetailCenter');
      center.hidden = false;
      window.setTimeout(function () { center.scrollIntoView({ behavior: 'smooth', block: 'start' }); }, 30);
      $.getJSON(detailUrl, { userId: userId }).done(function (response) { renderDetail(response, row); }).fail(function () { $('#accountDetailLoading').html('<span class="text-danger"><i class="fa-solid fa-circle-exclamation"></i> Không thể tải chi tiết tài khoản.</span>'); });
    });
    $(document).on('click', '.js-account-center-close', function () { document.getElementById('accountDetailCenter').hidden = true; });
    $(document).on('click', '[data-account-tab]', function () { var tab = this.dataset.accountTab; $('[data-account-tab]').removeClass('is-active').attr('aria-selected', 'false'); $(this).addClass('is-active').attr('aria-selected', 'true'); $('[data-account-panel]').removeClass('is-active'); $('[data-account-panel="' + tab + '"]').addClass('is-active'); });
    function showCommandModal(modalId) {
      bootstrap.Modal.getOrCreateInstance(document.getElementById(modalId)).show();
    }
    $(document).on('click', '.js-account-edit-from-detail', function () { var row = $('#accountDetailCenter').data('row') || {}; $('#accountEditUserId').val(row.userId); $('#accountEditName').val(row.name); $('#accountEditEmail').val(row.email); $('#accountEditLocation').val(row.location || ''); $('#accountEditReason').val(''); bootstrap.Modal.getOrCreateInstance(document.getElementById('accountEditModal')).show(); });

    $(document).on('click', '.js-account-edit', function () {
      var $button = $(this);
      $('#accountEditUserId').val($button.data('user-id'));
      $('#accountEditName').val($button.data('user-name'));
      $('#accountEditEmail').val($button.data('user-email'));
      $('#accountEditLocation').val($button.data('user-location'));
      $('#accountEditReason').val('');
      bootstrap.Modal.getOrCreateInstance(document.getElementById('accountEditModal')).show();
    });

    $(document).on('click', '.js-account-activate', function () {
      var $button = $(this);
      $('#accountActivationUserId').val($button.data('user-id'));
      $('#accountActivationReason').val('').removeClass('is-invalid');
      $('#accountActivationDescription').text('Xác minh email cho ' + ($button.data('user-name') || 'KOC này'));
      showCommandModal('accountActivationModal');
    });

    $('.js-account-activation-form').on('submit', function (event) {
      event.preventDefault();
      var $form = $(this);
      var $reason = $('#accountActivationReason');
      var $submit = $('#accountActivationSubmit');
      var reason = String($reason.val() || '').trim();
      $reason.val(reason);
      if (reason.length < 10 || reason.length > 1000) {
        $reason.addClass('is-invalid').trigger('focus');
        return;
      }
      $reason.removeClass('is-invalid');
      $submit.prop('disabled', true);
      $.ajax({
        url: $form.attr('action'), method: 'POST', data: $form.serialize(),
        headers: { RequestVerificationToken: $form.find('input[name="__RequestVerificationToken"]').val() }
      }).done(function (response) {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('accountActivationModal')).hide();
        table.ajax.reload(null, false);
        window.setTimeout(function () { window.AdminToast.show(response.message, 'success'); }, 100);
      }).fail(function (xhr) {
        var response = xhr.responseJSON || {};
        $('#accountActivationReasonError').text(response.message || 'Không thể xác minh email cho KOC.');
        $reason.addClass('is-invalid').trigger('focus');
      }).always(function () { $submit.prop('disabled', false); });
    });

    $(document).on('click', '.js-account-lock', function () {
      var $button = $(this);
      var shouldLock = String($button.data('locked')) === 'true';
      var displayName = $button.data('user-name') || 'tài khoản này';
      $('#accountStatusUserId').val($button.data('user-id'));
      $('#accountStatusLocked').val(shouldLock ? 'true' : 'false');
      $('#accountStatusReason').val('');
      $('#accountStatusReason').removeClass('is-invalid');
      $('#accountStatusModalTitle').html('<i class="fa-solid ' +
        (shouldLock ? 'fa-lock' : 'fa-lock-open') + ' me-2"></i>' +
        (shouldLock ? 'Xóa mềm tài khoản' : 'Khôi phục tài khoản'));
      $('#accountStatusDescription').text(
        (shouldLock ? 'Tài khoản sẽ bị khóa quyền truy cập nhưng toàn bộ lịch sử vẫn được giữ: ' : 'Khôi phục quyền truy cập cho ') + displayName + '.'
      );
      $('#accountStatusSubmit')
        .toggleClass('btn-danger', shouldLock)
        .toggleClass('btn-success', !shouldLock)
        .html('<i class="fa-solid ' + (shouldLock ? 'fa-lock' : 'fa-lock-open') +
          ' me-1"></i>' + (shouldLock ? 'Xác nhận xóa mềm' : 'Xác nhận khôi phục'));
      showCommandModal('accountStatusModal');
    });

    $(document).on('click', '.js-account-delete', function () {
      var $button=$(this);
      $('#accountDeleteUserId').val($button.data('user-id'));
      $('#accountDeleteReason').val('').removeClass('is-invalid');
      $('#accountDeleteDescription').text('Bạn sắp xóa tài khoản “'+($button.data('user-name')||'không xác định')+'”. Tài khoản sẽ không thể đăng nhập và không còn xuất hiện trong danh sách.');
      showCommandModal('accountDeleteModal');
    });
    $('.js-account-delete-form').on('submit',function(event){
      event.preventDefault();
      var $form=$(this),$reason=$('#accountDeleteReason'),$submit=$('#accountDeleteSubmit'),reason=String($reason.val()||'').trim();
      $reason.val(reason);
      if(reason.length<10||reason.length>1000){$reason.addClass('is-invalid').trigger('focus');return;}
      $reason.removeClass('is-invalid');$submit.prop('disabled',true);
      $.ajax({url:$form.attr('action'),method:'POST',data:$form.serialize(),headers:{RequestVerificationToken:$form.find('input[name="__RequestVerificationToken"]').val()}})
        .done(function(response){bootstrap.Modal.getOrCreateInstance(document.getElementById('accountDeleteModal')).hide();table.ajax.reload(null,false);window.setTimeout(function(){window.AdminToast.show(response.message,'success');},100);})
        .fail(function(xhr){var response=xhr.responseJSON||{};$('#accountDeleteReasonError').text(response.message||'Không thể xóa tài khoản.');$reason.addClass('is-invalid').trigger('focus');})
        .always(function(){$submit.prop('disabled',false);});
    });

    $('.js-account-status-form').on('submit', function (event) {
      event.preventDefault();
      var $form = $(this);
      var $submit = $('#accountStatusSubmit');
      var $reason = $('#accountStatusReason');
      var reason = String($reason.val() || '').trim();
      $reason.val(reason);
      if (reason.length < 10 || reason.length > 1000) {
        $reason.addClass('is-invalid').trigger('focus');
        return;
      }
      $reason.removeClass('is-invalid');
      $submit.prop('disabled', true);
      $.ajax({
        url: $form.attr('action'),
        method: 'POST',
        data: $form.serialize(),
        headers: {
          RequestVerificationToken: $form.find('input[name="__RequestVerificationToken"]').val()
        }
      }).done(function (response) {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('accountStatusModal')).hide();
        table.ajax.reload(null, false);
        window.setTimeout(function () { window.AdminToast.show(response.message, 'success'); }, 100);
      }).fail(function (xhr) {
        var response = xhr.responseJSON;
        if (!response && xhr.responseText) {
          try { response = JSON.parse(xhr.responseText); } catch (_error) { response = {}; }
        }
        response = response || {};
        var message = response.message;
        if (!message && xhr.status === 400) {
          message = 'Phiên xác thực đã hết hạn hoặc dữ liệu gửi lên không hợp lệ. Vui lòng tải lại trang.';
        } else if (!message && xhr.status === 403) {
          message = 'Bạn không có quyền thay đổi trạng thái tài khoản này.';
        } else if (!message && xhr.status >= 500) {
          message = 'Hệ thống gặp lỗi khi lưu trạng thái. Vui lòng thử lại hoặc kiểm tra nhật ký máy chủ.';
        }
        $('#accountStatusReasonError').text(message || 'Không thể cập nhật trạng thái tài khoản.');
        $reason.addClass('is-invalid').trigger('focus');
        table.ajax.reload(null, false);
      }).always(function () {
        $submit.prop('disabled', false);
      });
    });

    $('#accountStatusReason').on('input', function () {
      var value = String($(this).val() || '').trim();
      $(this).toggleClass('is-invalid', value.length > 0 && (value.length < 10 || value.length > 1000));
      $('#accountStatusReasonError').text('Vui lòng nhập lý do từ 10 đến 1.000 ký tự, không tính khoảng trắng thừa.');
    });

    $(document).on('click', '.js-account-impersonate', function () {
      var $button = $(this);
      $('#accountImpersonateUserId').val($button.data('user-id'));
      $('#accountImpersonateReason').val('').removeClass('is-invalid');
      $('#accountImpersonateDescription').text('Bạn sẽ mở cổng ' + accountType + ' dưới tài khoản “' + ($button.data('user-name') || 'không xác định') + '”.');
      bootstrap.Modal.getOrCreateInstance(document.getElementById('accountImpersonateModal')).show();
    });

    $('.js-account-impersonate-form').on('submit', function (event) {
      var reason = String($('#accountImpersonateReason').val() || '').trim();
      if (reason.length < 10 || reason.length > 500) {
        event.preventDefault();
        $('#accountImpersonateReason').addClass('is-invalid').trigger('focus');
        return;
      }
      $('#accountImpersonateSubmit').prop('disabled', true);
    });
  });
})(jQuery);
