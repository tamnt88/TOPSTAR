(() => {
  const tabs = [...document.querySelectorAll('[data-home-tab]')];
  const panels = [...document.querySelectorAll('[data-home-panel]')];
  const form = document.querySelector('[data-homepage-form]');
  const labels = { Features: 'Lợi ích', ProcessSteps: 'Bước', Creators: 'KOC/KOL', Partners: 'Đối tác' };
  const activate = key => { tabs.forEach(x => x.classList.toggle('active', x.dataset.homeTab === key)); panels.forEach(x => x.classList.toggle('active', x.dataset.homePanel === key)); history.replaceState(null, '', `#${key}`); };
  tabs.forEach(x => x.addEventListener('click', () => activate(x.dataset.homeTab)));
  const initial = location.hash.slice(1); if (tabs.some(x => x.dataset.homeTab === initial)) activate(initial);
  const revealError = field => {
    if (!field) return;
    const panel = field.closest('[data-home-panel]');
    if (panel) activate(panel.dataset.homePanel);
    const target = field.type === 'hidden'
      ? field.closest('.homepage-image-field')?.querySelector('[data-image-upload]') || field.parentElement
      : field;
    target?.scrollIntoView?.({ behavior: 'smooth', block: 'center' });
    if (window.TopstarForms && target?.matches('input,select,textarea')) window.TopstarForms.focusField(target);
    else target?.focus?.();
  };
  const firstError = () => form?.querySelector('.is-invalid,.input-validation-error,[aria-invalid="true"]');
  if (firstError()) setTimeout(() => revealError(firstError()), 0);
  if (window.jQuery) window.jQuery(form).on('invalid-form.validate', (event, validator) => {
    revealError(validator.errorList?.[0]?.element || firstError());
  });

  const showListMessage = (list, message) => {
    let node = list.parentElement.querySelector(':scope > [data-list-message]');
    if (!node) { node = document.createElement('p'); node.dataset.listMessage = ''; node.className = 'dynamic-list-message'; list.after(node); }
    node.textContent = message; window.setTimeout(() => { node.textContent = ''; }, 3500);
  };
  const reindex = list => {
    const collection = list.dataset.dynamicList;
    [...list.querySelectorAll(':scope > [data-list-item]')].forEach((item, index) => {
      item.querySelectorAll('[name]').forEach(field => field.name = field.name.replace(new RegExp(`Input\\.${collection}\\[\\d+\\]`), `Input.${collection}[${index}]`));
      item.querySelectorAll('[id]').forEach(field => field.id = field.id.replace(new RegExp(`Input_${collection}_\\d+__`), `Input_${collection}_${index}__`));
      item.querySelectorAll('label[for]').forEach(label => label.htmlFor = label.htmlFor.replace(new RegExp(`Input_${collection}_\\d+__`), `Input_${collection}_${index}__`));
      const order = item.querySelector('[data-display-order]'); if (order) order.value = index + 1;
      const title = item.querySelector('[data-item-title]'); if (title) title.textContent = `${labels[collection] || 'Mục'} ${index + 1}`;
    });
  };
  const resetClone = (item, collection) => {
    item.querySelectorAll('input, textarea').forEach(field => {
      if (field.matches('[data-display-order]')) return;
      if (field.type === 'checkbox') { field.checked = true; return; }
      if (field.type === 'hidden' && field.value === 'false') return;
      field.value = '';
    });
    item.querySelectorAll('[data-upload-error]').forEach(x => x.textContent = '');
    const preview = item.querySelector('[data-image-preview]');
    if (preview?.tagName === 'IMG') { preview.removeAttribute('src'); preview.classList.add('is-empty'); }
    else if (preview) preview.replaceChildren(Object.assign(document.createElement('i'), { className: 'fa-solid fa-image' }));
    if (collection === 'Features') { const icon = item.querySelector('[data-image-url]'); if (icon) icon.value = 'fa-solid fa-star'; }
  };

  document.addEventListener('click', event => {
    const add = event.target.closest('[data-list-add]');
    if (add) {
      const list = document.querySelector(`[data-dynamic-list="${add.dataset.listAdd}"]`); const items = list ? [...list.querySelectorAll(':scope > [data-list-item]')] : [];
      if (!list || !items.length) return;
      const max = Number(list.dataset.maxItems || 20); if (items.length >= max) return showListMessage(list, `Chỉ được thêm tối đa ${max} mục.`);
      const clone = items.at(-1).cloneNode(true); resetClone(clone, add.dataset.listAdd); list.append(clone); reindex(list); clone.querySelector('input:not([type=hidden]), textarea')?.focus(); return;
    }
    const remove = event.target.closest('[data-list-remove]');
    if (remove) {
      const list = remove.closest('[data-dynamic-list]'); const items = list ? [...list.querySelectorAll(':scope > [data-list-item]')] : [];
      if (!list) return; if (items.length <= 1) return showListMessage(list, 'Danh sách phải còn ít nhất 1 mục.');
      remove.closest('[data-list-item]').remove(); reindex(list);
    }
  });

  document.addEventListener('change', async event => {
    const input = event.target.closest('[data-image-upload]'); if (!input) return;
    const file = input.files?.[0]; if (!file) return;
    const field = input.closest('.homepage-image-field'); const error = field?.querySelector('[data-upload-error]'); const urlInput = field?.querySelector('[data-image-url]'); const preview = field?.querySelector('[data-image-preview]'); const uploadLabel = input.closest('.homepage-upload'); const labelText = uploadLabel?.querySelector('span'); const originalText = labelText?.textContent || 'Chọn ảnh';
    if (!['image/jpeg','image/png','image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) { if (error) error.textContent = 'Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.'; input.value = ''; return; }
    const token = form?.querySelector('input[name="__RequestVerificationToken"]'); const body = new FormData(); body.append('imageFile', file); if (token) body.append('__RequestVerificationToken', token.value);
    if (error) error.textContent = ''; uploadLabel?.classList.add('is-uploading'); if (labelText) labelText.textContent = 'Đang tải ảnh...'; input.disabled = true;
    try {
      const response = await fetch('?handler=UploadImage', { method:'POST', body }); const result = await response.json(); if (!response.ok || !result.success) throw new Error(result.message || 'Không thể tải ảnh lên.');
      if (urlInput) urlInput.value = result.url;
      if (preview?.tagName === 'IMG') { preview.src = result.url; preview.classList.remove('is-empty'); }
      else if (preview) { const image = document.createElement('img'); image.src = result.url; image.alt = 'Icon đã tải lên'; preview.replaceChildren(image); }
    } catch (ex) { if (error) error.textContent = ex.message || 'Không thể tải ảnh lên. Vui lòng thử lại.'; }
    finally { input.disabled = false; input.value = ''; uploadLabel?.classList.remove('is-uploading'); if (labelText) labelText.textContent = originalText; }
  });

  form?.addEventListener('submit', () => {
    document.querySelectorAll('[data-dynamic-list]').forEach(reindex);
    setTimeout(() => {
      const invalid = firstError();
      if (!invalid) return;
      revealError(invalid);
      window.TopstarNotify?.warning('Chưa thể lưu. Vui lòng kiểm tra mục được đánh dấu.');
    }, 0);
  });
})();
