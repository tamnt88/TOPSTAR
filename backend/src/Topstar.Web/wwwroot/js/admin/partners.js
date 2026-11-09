(() => {
  const form = document.querySelector('[data-partner-form]');
  const upload = form?.querySelector('[data-logo-upload]');
  const urlField = form?.querySelector('[data-logo-url]');
  const preview = form?.querySelector('[data-logo-preview]');
  const error = form?.querySelector('[data-upload-error]');

  upload?.addEventListener('change', async () => {
    const file = upload.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      error.textContent = 'Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.';
      upload.value = '';
      return;
    }

    const token = form.querySelector('input[name="__RequestVerificationToken"]');
    const body = new FormData();
    body.append('imageFile', file);
    if (token) body.append('__RequestVerificationToken', token.value);
    error.textContent = '';
    upload.disabled = true;
    upload.closest('label')?.classList.add('is-uploading');
    try {
      const response = await fetch('?handler=UploadImage', { method: 'POST', body });
      const result = await response.json();
      if (!response.ok || !result.success) throw new Error(result.message || 'Không thể tải logo lên.');
      urlField.value = result.url;
      preview.classList.remove('is-empty');
      preview.innerHTML = '';
      const image = document.createElement('img');
      image.src = result.url;
      image.alt = 'Xem trước logo';
      preview.append(image);
    } catch (exception) {
      error.textContent = exception.message || 'Không thể tải logo lên. Vui lòng thử lại.';
    } finally {
      upload.disabled = false;
      upload.value = '';
      upload.closest('label')?.classList.remove('is-uploading');
    }
  });

  document.querySelectorAll('[data-delete-partner]').forEach(formElement => {
    formElement.dataset.confirm = `Xóa logo đối tác “${formElement.dataset.deletePartner}”?`;
    formElement.dataset.confirmTitle = 'Xác nhận xóa đối tác';
    formElement.dataset.confirmButton = 'Xóa đối tác';
    formElement.dataset.confirmTone = 'danger';
  });
})();
