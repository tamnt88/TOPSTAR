(function ($) {
  'use strict';
  $(function () {
    var $form = $('.package-editor-card');
    if (!$form.length) return;
    var benefitsEditor = null;
    var $benefits = $('#Input_Benefits');
    var $money = $('#packageSalePriceDisplay');
    var usesReferenceBudget = !$money.length;
    if (usesReferenceBudget) $money = $('#referenceBudgetDisplay');
    var $moneyValue = $('#Input_SalePrice');

    function activateInvalidTab($field) {
      var pane = $field.closest('.tab-pane')[0];
      if (!pane) return;
      var trigger = document.querySelector('[data-bs-target="#' + pane.id + '"]');
      if (trigger) bootstrap.Tab.getOrCreateInstance(trigger).show();
    }
    function refreshTabErrors() {
      $form.find('.package-form-tabs [data-bs-toggle="tab"]').each(function () {
        var pane = document.querySelector($(this).attr('data-bs-target'));
        var hasError = !!pane && Array.prototype.some.call(pane.querySelectorAll('.field-validation-error'), function (message) {
          return message.textContent.trim().length > 0;
        });
        $(this).toggleClass('has-error', hasError);
      });
    }

    function syncSelectionQuota() {
      var enabled = $('#Input_BrandSelectionEnabled').is(':checked');
      $('.package-quota-hint').text(enabled
        ? 'Brand được xác nhận tối đa số KOC này trong một chiến dịch.'
        : 'Topstar phân công tối đa số KOC này cho một chiến dịch.');
    }
    $('#Input_BrandSelectionEnabled').on('change', syncSelectionQuota);
    syncSelectionQuota();

    function digits(value) { return String(value || '').replace(/[^\d]/g, '').replace(/^0+(?=\d)/, ''); }
    function syncMoney() {
      var raw = digits($money.val());
      if (usesReferenceBudget) $('#Input_ReferenceBudget').val(raw || '');
      if (!usesReferenceBudget || Number($moneyValue.val()) <= 0) $moneyValue.val(raw || '0');
      $money.val(raw ? Number(raw).toLocaleString('vi-VN') : '');
      return Number(raw || 0);
    }
    $money.on('input', function () {
      var caretAtEnd = this.selectionStart === this.value.length;
      syncMoney();
      if (caretAtEnd) this.setSelectionRange(this.value.length, this.value.length);
      $(this).removeClass('is-invalid');
      $('[data-valmsg-for="'+(usesReferenceBudget?'Input.ReferenceBudget':'Input.SalePrice')+'"]').removeClass('field-validation-error').empty();
    }).on('blur', syncMoney);
    syncMoney();

    $('#packageImageFile').on('change', function () {
      var file = this.files && this.files[0];
      var $preview = $('#packageImagePreview');
      if (!file) return;
      if (!/^image\/(jpeg|png|webp)$/.test(file.type) || file.size > 5 * 1024 * 1024) {
        $(this).addClass('is-invalid').val('');
        $('[data-valmsg-for="ImageFile"]').addClass('field-validation-error').text('Ảnh phải là JPG, PNG hoặc WEBP và không vượt quá 5 MB.');
        return;
      }
      $(this).removeClass('is-invalid');
      $('[data-valmsg-for="ImageFile"]').removeClass('field-validation-error').empty();
      $preview.attr('src', URL.createObjectURL(file)).prop('hidden', false);
    });

    function ServerUploadAdapter(loader) {
      this.loader = loader;
      this.controller = new AbortController();
    }
    ServerUploadAdapter.prototype.upload = function () {
      var self = this;
      return this.loader.file.then(function (file) {
        var data = new FormData();
        data.append('upload', file);
        data.append('__RequestVerificationToken', $form.find('input[name="__RequestVerificationToken"]').val());
        return fetch(window.location.pathname + '?handler=EditorImage', {
          method: 'POST',
          body: data,
          signal: self.controller.signal,
          headers: { RequestVerificationToken: $form.find('input[name="__RequestVerificationToken"]').val() }
        }).then(function (response) {
          return response.json().then(function (body) {
            if (!response.ok || !body.url) throw new Error(body.error && body.error.message ? body.error.message : 'Không thể tải ảnh lên.');
            return { default: body.url };
          });
        });
      });
    };
    ServerUploadAdapter.prototype.abort = function () { this.controller.abort(); };
    function ServerUploadAdapterPlugin(editor) {
      editor.plugins.get('FileRepository').createUploadAdapter = function (loader) { return new ServerUploadAdapter(loader); };
    }

    if (window.CKEDITOR && $benefits.length) {
      var C = window.CKEDITOR;
      C.ClassicEditor.create($benefits[0], {
        licenseKey: 'GPL',
        language: 'vi',
        plugins: [
          C.Essentials, C.Paragraph, C.Heading, C.Bold, C.Italic, C.Underline, C.Strikethrough,
          C.Subscript, C.Superscript, C.Code, C.RemoveFormat, C.FontFamily, C.FontSize, C.FontColor,
          C.FontBackgroundColor, C.Highlight, C.Alignment, C.List, C.ListProperties, C.TodoList,
          C.Indent, C.IndentBlock, C.Link, C.Table, C.TableToolbar, C.BlockQuote, C.CodeBlock,
          C.HorizontalLine, C.SpecialCharacters, C.SpecialCharactersEssentials, C.Image, C.ImageToolbar,
          C.ImageCaption, C.ImageStyle, C.ImageResize, C.ImageUpload, C.MediaEmbed, C.SourceEditing,
          C.GeneralHtmlSupport, ServerUploadAdapterPlugin
        ],
        toolbar: {
          items: [
            'undo', 'redo', '|', 'heading', '|', 'fontFamily', 'fontSize', 'fontColor',
            'fontBackgroundColor', 'highlight', '|', 'bold', 'italic', 'underline', 'strikethrough',
            'subscript', 'superscript', 'code', 'removeFormat', '|', 'alignment', '|',
            'bulletedList', 'numberedList', 'todoList', 'outdent', 'indent', '|', 'link',
            'insertTable', 'blockQuote', 'codeBlock', 'horizontalLine', 'specialCharacters', '|',
            'uploadImage', 'mediaEmbed', '|', 'sourceEditing'
          ],
          shouldNotGroupWhenFull: true
        },
        heading: { options: [{ model: 'paragraph', title: 'Đoạn văn', class: 'ck-heading_paragraph' }, { model: 'heading2', view: 'h2', title: 'Tiêu đề 1', class: 'ck-heading_heading2' }, { model: 'heading3', view: 'h3', title: 'Tiêu đề 2', class: 'ck-heading_heading3' }] },
        image: { toolbar: ['imageTextAlternative', 'toggleImageCaption', '|', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side', '|', 'resizeImage'] },
        table: { contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'] },
        link: { addTargetToExternalLinks: true, defaultProtocol: 'https://' },
        htmlSupport: { allow: [{ name: /^(p|br|ul|ol|li|span|h2|h3|h4|blockquote|pre|code|a|figure|figcaption|img|table|thead|tbody|tr|th|td|oembed)$/, attributes: true, classes: true, styles: true }] },
        placeholder: 'Nhập và định dạng các quyền lợi của gói'
      }).then(function (editor) {
        benefitsEditor = editor;
        editor.model.document.on('change:data', function () {
          $benefits.val(editor.getData()).removeClass('is-invalid');
          $('.package-benefits-field .ck-editor').removeClass('is-invalid');
          $('[data-valmsg-for="Input.Benefits"]').removeClass('field-validation-error').empty();
        });
      }).catch(function (error) {
        window.AdminToast.show('Không thể khởi tạo CKEditor: ' + (error.message || 'Lỗi không xác định.'), 'error');
      });
    }

    function syncServerErrors() {
      var $first = $();
      $form.find('[data-valmsg-for]').each(function () {
        var $message = $(this);
        if (!$message.hasClass('field-validation-error') || !$.trim($message.text())) return;
        var name = $message.attr('data-valmsg-for');
        var $field = name === (usesReferenceBudget ? 'Input.ReferenceBudget' : 'Input.SalePrice') ? $money : $form.find('[name="' + name.replace(/"/g, '\\"') + '"]').first();
        $field.addClass('is-invalid').attr('aria-invalid', 'true');
        if (!$first.length) $first = $field;
      });
      if ($first.length) activateInvalidTab($first);
      refreshTabErrors();
    }

    $form.on('submit.packageTabs', function (event) {
      syncMoney();
      if (benefitsEditor) $benefits.val(benefitsEditor.getData());
      var $first = $();
      if (Number(usesReferenceBudget ? $("#Input_ReferenceBudget").val() : $moneyValue.val()) <= 0) {
        $money.addClass('is-invalid');
        $('[data-valmsg-for="'+(usesReferenceBudget?'Input.ReferenceBudget':'Input.SalePrice')+'"]').addClass('field-validation-error').text(usesReferenceBudget?'Vui lòng nhập ngân sách tham khảo.':'Giá bán phải lớn hơn 0.');
        $first = $money;
      }
      var benefitsText = $('<div>').html($benefits.val()).text().trim();
      if (!benefitsText) {
        $('.package-benefits-field .ck-editor').addClass('is-invalid');
        $('[data-valmsg-for="Input.Benefits"]').addClass('field-validation-error').text('Vui lòng nhập ít nhất một quyền lợi.');
        if (!$first.length) $first = $benefits;
      }
      if ($first.length) {
        event.preventDefault();
        activateInvalidTab($first);
        window.setTimeout(function () { ($first.is($benefits) ? $('.ck-editor__editable') : $first).trigger('focus'); }, 120);
      }
      window.setTimeout(refreshTabErrors, 0);
    });

    $form.on('input change', '.form-control, .form-select', function () {
      var $field = $(this), name = $field.attr('name');
      if (!$field.hasClass('is-invalid') || !name) return;
      $form.find('[data-valmsg-for="' + name.replace(/"/g, '\\"') + '"]').removeClass('field-validation-error').addClass('field-validation-valid').empty();
      $field.removeClass('is-invalid input-validation-error').removeAttr('aria-invalid');
      refreshTabErrors();
    });
    syncServerErrors();
  });
})(jQuery);
