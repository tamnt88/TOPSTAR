(function () {
  if (!location.pathname.toLowerCase().endsWith('/admin/settings/emailaccounts')) return;
  function enhanceActions() {
    document.querySelectorAll('.email-actions').forEach(function (actions) {
      if (actions.dataset.diagnosticsReady === 'true') return;
      var edit = Array.from(actions.querySelectorAll('a[href]')).find(function (link) {
        return link.getAttribute('href').toLowerCase().includes('/emailaccountform');
      });
      if (!edit) return;
      var id = new URL(edit.href, location.origin).searchParams.get('id');
      if (!id) return;
      [{ handler: 'TestServer', icon: 'fa-plug-circle-check', label: 'Kiểm tra máy chủ SMTP' },
       { handler: 'SendTest', icon: 'fa-paper-plane', label: 'Gửi email thử đến địa chỉ này' }]
        .forEach(function (item) {
          var button = document.createElement('button');
          button.type = 'button';
          button.className = 'email-diagnostic-action';
          button.title = item.label;
          button.setAttribute('aria-label', item.label);
          button.innerHTML = '<i class="fa-solid ' + item.icon + '"></i>';
          button.addEventListener('click', function () {
            var form = document.getElementById('emailDiagnosticForm');
            document.getElementById('emailDiagnosticId').value = id;
            form.action = location.pathname + '?handler=' + item.handler;
            form.requestSubmit();
          });
          actions.insertBefore(button, edit);
        });
      actions.dataset.diagnosticsReady = 'true';
    });
  }
  document.addEventListener('DOMContentLoaded', enhanceActions);
  document.addEventListener('draw.dt', enhanceActions);
  if (document.readyState !== 'loading') enhanceActions();
})();
