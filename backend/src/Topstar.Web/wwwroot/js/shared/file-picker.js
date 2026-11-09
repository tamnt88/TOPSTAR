(() => {
    'use strict';

    const customPickerSelector = [
        '.brand-file-picker',
        '.brand-payment-proof-file',
        '.academy-upload',
        '.homepage-upload',
        '.partner-upload',
        '.ck',
        '.ck-body-wrapper',
        '.tox',
        '.sun-editor',
        '.note-editor',
        '.ql-container',
        '[data-native-file-picker]'
    ].join(',');

    const formatBytes = bytes => {
        if (!Number.isFinite(bytes) || bytes <= 0) return '';
        if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(bytes < 10 * 1024 * 1024 ? 1 : 0)} MB`;
    };

    const describeSelection = input => {
        const files = Array.from(input.files || []);
        if (!files.length) return { name: 'Chưa chọn tệp', meta: 'Kéo thả hoặc chọn tệp từ thiết bị' };
        if (files.length === 1) return { name: files[0].name, meta: formatBytes(files[0].size) };
        const total = files.reduce((sum, file) => sum + file.size, 0);
        return { name: `Đã chọn ${files.length} tệp`, meta: formatBytes(total) };
    };

    const enhance = input => {
        if (!(input instanceof HTMLInputElement) || input.type !== 'file' || input.dataset.tsFilePicker === 'ready') return;
        // Rich-text editors own their hidden file input and its upload lifecycle.
        if (input.hidden || input.getAttribute('aria-hidden') === 'true'
            || input.style.display === 'none' || input.style.visibility === 'hidden') return;
        if (input.closest(customPickerSelector) || input.parentElement?.tagName === 'LABEL') return;

        input.dataset.tsFilePicker = 'ready';
        input.classList.add('ts-file-picker__input');

        const picker = document.createElement('div');
        picker.className = 'ts-file-picker';
        picker.innerHTML = `
            <div class="ts-file-picker__summary" aria-live="polite">
                <span class="ts-file-picker__icon"><i class="fa-solid fa-paperclip"></i></span>
                <span class="ts-file-picker__copy">
                    <span class="ts-file-picker__name">Chưa chọn tệp</span>
                    <span class="ts-file-picker__meta">Kéo thả hoặc chọn tệp từ thiết bị</span>
                </span>
            </div>
            <button type="button" class="ts-file-picker__button"><i class="fa-solid fa-cloud-arrow-up"></i><span>Chọn tệp</span></button>`;

        input.parentNode.insertBefore(picker, input);
        picker.prepend(input);

        const name = picker.querySelector('.ts-file-picker__name');
        const meta = picker.querySelector('.ts-file-picker__meta');
        const button = picker.querySelector('.ts-file-picker__button');

        const render = () => {
            const selection = describeSelection(input);
            name.textContent = selection.name;
            meta.textContent = selection.meta;
            picker.classList.toggle('has-file', (input.files?.length || 0) > 0);
            picker.classList.toggle('is-invalid', !input.checkValidity());
            picker.classList.toggle('is-disabled', input.disabled);
            button.disabled = input.disabled;
        };

        button.addEventListener('click', () => input.click());
        picker.querySelector('.ts-file-picker__summary').addEventListener('click', () => {
            if (!input.disabled) input.click();
        });
        input.addEventListener('change', render);
        input.addEventListener('invalid', () => picker.classList.add('is-invalid'));

        ['dragenter', 'dragover'].forEach(type => picker.addEventListener(type, event => {
            event.preventDefault();
            if (!input.disabled) picker.classList.add('is-dragging');
        }));
        ['dragleave', 'drop'].forEach(type => picker.addEventListener(type, event => {
            event.preventDefault();
            picker.classList.remove('is-dragging');
        }));
        picker.addEventListener('drop', event => {
            if (input.disabled || !event.dataTransfer?.files?.length) return;
            const transfer = new DataTransfer();
            Array.from(event.dataTransfer.files)
                .slice(0, input.multiple ? undefined : 1)
                .forEach(file => transfer.items.add(file));
            input.files = transfer.files;
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        input.form?.addEventListener('reset', () => setTimeout(render));
        render();
    };

    const scan = root => {
        if (root instanceof HTMLInputElement) enhance(root);
        root.querySelectorAll?.('input[type="file"]').forEach(enhance);
    };

    const start = () => {
        scan(document);
        new MutationObserver(records => records.forEach(record => record.addedNodes.forEach(node => {
            if (node instanceof Element) scan(node);
        }))).observe(document.body, { childList: true, subtree: true });
    };

    document.readyState === 'loading' ? document.addEventListener('DOMContentLoaded', start) : start();
})();

