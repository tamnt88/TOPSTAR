(function ($) {
  'use strict';
  $(function () {
    var $form = $('#campaignNeedsForm');
    if (!$form.length) return;
    var $budget = $('#Budget'), $display = $('#needsBudgetDisplay'), $count = $('#KocCount');
    var $selection = $('#SelfSelect'), $packageId = $('#PackageId'), $quote = $('#needs-quote');
    var timer, request, version = 0, applyingTemplate = false;
    var modalElement = document.getElementById('needsCustomModal');
    var modal = modalElement ? bootstrap.Modal.getOrCreateInstance(modalElement) : null;
    var customSnapshot, customApplied = false;
    function customSummary() {
      $('#needsCustomSummary').prop('hidden', !!$packageId.val() || !(Number($budget.val()) > 0))
        .text('Đã tùy chỉnh: ' + money(Number($budget.val())) + ' · ' + $count.val() + ' KOC · ' + ($selection.val() === 'true' ? 'Brand tự chọn' : 'TopStar phân bổ'));
    }
    function openCustom() {
      if (!modal) { custom(); $display.trigger('focus'); return; }
      customSnapshot = { budget:$budget.val(), count:$count.val(), self:$selection.val(), packageId:$packageId.val() };
      customApplied = false; modal.show();
    }
    if (modalElement) {
      modalElement.addEventListener('shown.bs.modal', function () { $display.trigger('focus'); });
      modalElement.addEventListener('hidden.bs.modal', function () {
        if (!customApplied && customSnapshot) {
          applyingTemplate = true;
          $budget.val(customSnapshot.budget); $display.val(Number(customSnapshot.budget) > 0 ? Number(customSnapshot.budget).toLocaleString('vi-VN') : '');
          $count.val(customSnapshot.count); $packageId.val(customSnapshot.packageId);
          updateSelectionAvailability(); $selection.val(customSnapshot.self).trigger('change.select2'); applyingTemplate = false;
          markMode($packageId.val() || !(Number($budget.val()) > 0) ? 'package' : 'custom'); errors(false); quote();
        }
        customSummary();
      });
    }
    $('#needsApplyCustom').on('click', function () {
      syncBudget(); var first = errors(true); if (first) { first.trigger('focus'); return; }
      custom(); customApplied = true; customSummary(); quote(); modal.hide();
    });
    $('#needsDetails').on('keydown', 'input', function(e) { if (e.key === 'Enter') { e.preventDefault(); $('#needsApplyCustom').trigger('click'); } });
    $form.on('click', '[data-needs-scroll]', function () {
      var track = $form.find('.needs-package-grid')[0]; if (!track) return;
      track.scrollBy({left:Number(this.dataset.needsScroll) * track.clientWidth, behavior:'smooth'});
    });

    var money = function (v) { return new Intl.NumberFormat('vi-VN').format(v) + ' đ'; };
    var selected = function () { return $form.find('[data-needs-package]').filter(function () { return this.dataset.needsPackage === $packageId.val(); }); };
    function markMode(mode) {
      $form.find('[data-needs-mode]').each(function () { $(this).toggleClass('is-active', this.dataset.needsMode === mode).attr('aria-pressed', this.dataset.needsMode === mode ? 'true' : 'false'); });
      $form.find('[data-needs-package]').each(function () {
        var active = mode === 'package' && this.dataset.needsPackage === $packageId.val();
        $(this).toggleClass('is-selected', active).attr('aria-pressed', active ? 'true' : 'false');
      });
      $('#needsSelectionLabel').text(mode === 'package' ? (selected().length ? 'Đã chọn: ' + selected().attr('data-name') : 'Chọn gói KOC phía trên để điền sẵn nhu cầu.') : 'Tùy chỉnh ngân sách, số KOC và cách tuyển phù hợp.');
    }
    function custom() { if (applyingTemplate) return; $packageId.val(''); markMode('custom'); }
    function syncBudget() {
      var raw = String($display.val() || '').replace(/[^0-9]/g, '').replace(/^0+(?=\d)/, '');
      $budget.val(raw || '0');
      $display.val(raw ? Number(raw).toLocaleString('vi-VN') : '');
    }
    function errors(show) {
      var budget = Number($budget.val()), count = Number($count.val()), first = null;
      var minimum = Number($form.attr('data-min-budget')), perKoc = Number($form.attr('data-per-koc'));
      var limit = Math.min(Number($form.attr('data-max-koc')), Math.floor(budget / perKoc));
      var values = [
        [$display, '#needsBudgetError', !Number.isSafeInteger(budget) || budget < minimum ? 'Ngân sách tối thiểu ' + money(minimum) + '.' : ''],
        [$count, '#needsCountError', !Number.isInteger(count) || count < 1 || count > limit ? 'Nhập từ 1 đến ' + Math.max(1, limit) + ' KOC theo ngân sách này.' : ''],
        [$selection, '#needsSelectionError', $selection.val() === 'true' && budget < Number($form.attr('data-self-min')) ? 'Ngân sách chưa đủ điều kiện tự chọn KOC.' : '']
      ];
      values.forEach(function (row) {
        var message = show ? row[2] : '';
        row[0].toggleClass('is-invalid', !!message).attr('aria-invalid', message ? 'true' : 'false');
        $(row[1]).text(message);
        if (row[2] && !first) first = row[0];
      });
      return first;
    }
    function updateSelectionAvailability() {
      var allowed = Number($budget.val()) >= Number($form.attr('data-self-min'));
      $selection.find('option[value="true"]').prop('disabled', !allowed);
      if (!allowed && $selection[0].value === 'true') $selection.val('false').trigger('change.select2');
    }
    function quote() {
      clearTimeout(timer);
      if (request) request.abort();
      var current = ++version;
      updateSelectionAvailability();
      if (!(Number($budget.val()) > 0)) {
        $quote.empty().append($('<div class="needs-quote-empty">').append($('<i class="fa-solid fa-calculator">'), $('<p>').text('Chọn gói hoặc nhập ngân sách để xem chi phí.')));
        return;
      }
      timer = setTimeout(function () {
        request = $.getJSON(location.pathname, { handler: 'Quote', budget: $budget.val(), count: $count.val(), selfSelect: $selection.val() });
        request.done(function (q) {
          if (current !== version) return;
          $quote.empty();
          if (q.error) { $('<p class="text-danger small">').text(q.error).appendTo($quote); return; }
          [['Thù lao KOC (trước khấu trừ)', q.budget], ['Phí nền tảng Brand', q.fee], ['Thuế GTGT' + (q.vatRate ? ' (' + q.vatRate + '%)' : '') + (q.vatMode === 'Included' ? ' · đã gồm trong giá' : ''), q.vat], ['Tổng dự kiến', q.total]].forEach(function (item, i) {
            var row = $('<div class="needs-quote-row">').toggleClass('is-total', i === 3).appendTo($quote);
            $('<span>').text(item[0]).appendTo(row); $('<strong>').text(money(item[1])).appendTo(row);
          });
          $('<small class="needs-quote-disclaimer">').text('Chưa gồm phí thanh toán nếu phương thức được chọn có thu phí.').appendTo($quote);
        }).fail(function (xhr, status) { if (status !== 'abort' && current === version) $quote.empty().append($('<p class="text-danger small">').text('Không tải được chi phí. Vui lòng thử lại.')); });
      }, 250);
    }
    $form.on('click', '[data-needs-package]', function () {
      applyingTemplate = true;
      $packageId.val(this.dataset.needsPackage); $budget.val(this.dataset.budget);
      $display.val(Number(this.dataset.budget).toLocaleString('vi-VN')); $count.val(this.dataset.count);
      updateSelectionAvailability(); $selection.val(this.dataset.self).trigger('change.select2');
      applyingTemplate = false; markMode('package'); customSummary(); errors(false); quote();
    });
    $form.on('click', '[data-needs-mode]', function () {
      if (this.dataset.needsMode === 'custom') { openCustom(); }
      else { markMode('package'); document.getElementById('needsPackages').scrollIntoView({ behavior: 'smooth', block: 'start' }); }
    });
    $display.on('input', function () { syncBudget(); custom(); errors(false); quote(); });
    $count.on('input', function () { custom(); errors(false); quote(); });
    $selection.on('change', function () { custom(); errors(false); quote(); });
    $form.on('submit.needs', function (e) {
      syncBudget(); var first = errors(true);
      if (first) { e.preventDefault(); e.stopImmediatePropagation(); if(modal) openCustom(); else first.trigger('focus'); }
    });
    markMode(selected().length || !(Number($budget.val()) > 0) ? 'package' : 'custom'); customSummary(); quote();
  });
})(jQuery);
