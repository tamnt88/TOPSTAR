(() => {
    const field = document.getElementById('Input_PageContent'), C = window.CKEDITOR;
    if (!field || !C?.ClassicEditor) return;
    function ServerUploadAdapter(loader) {
        this.loader = loader;
        this.controller = new AbortController();
    }
    ServerUploadAdapter.prototype.upload = function () {
        return this.loader.file.then(file => {
            const data = new FormData();
            const token = field.closest('form')?.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            data.append('upload', file);
            data.append('__RequestVerificationToken', token);
            return fetch(`${new URL(window.topstarWorkspaceUrl || window.location.href).pathname}?handler=EditorImage`, {
                method: 'POST',
                body: data,
                signal: this.controller.signal,
                headers: { RequestVerificationToken: token }
            }).then(async response => {
                const body = await response.json();
                if (!response.ok || !body.url) throw new Error(body.error?.message || 'Không thể tải ảnh lên.');
                return { default: body.url };
            });
        });
    };
    ServerUploadAdapter.prototype.abort = function () { this.controller.abort(); };
    function ServerUploadAdapterPlugin(editor) {
        editor.plugins.get('FileRepository').createUploadAdapter = loader => new ServerUploadAdapter(loader);
    }

    C.ClassicEditor.create(field, {
        licenseKey: 'GPL',
        language: 'vi',
        plugins: [
            C.Essentials, C.Paragraph, C.Heading, C.Bold, C.Italic, C.Underline, C.Strikethrough,
            C.Subscript, C.Superscript, C.Code, C.RemoveFormat, C.FontFamily, C.FontSize, C.FontColor,
            C.FontBackgroundColor, C.Highlight, C.Alignment, C.List, C.ListProperties, C.TodoList,
            C.Indent, C.IndentBlock, C.Link, C.Table, C.TableToolbar, C.BlockQuote,
            C.Image, C.ImageToolbar, C.ImageCaption, C.ImageStyle, C.ImageResize, C.ImageUpload,
            C.GeneralHtmlSupport, ServerUploadAdapterPlugin
        ],
        toolbar: {
            items: [
                'undo', 'redo', '|', 'heading', '|', 'fontFamily', 'fontSize', 'fontColor',
                'fontBackgroundColor', 'highlight', '|', 'bold', 'italic', 'underline', 'strikethrough',
                'subscript', 'superscript', 'code', 'removeFormat', '|', 'alignment', '|',
                'bulletedList', 'numberedList', 'todoList', 'outdent', 'indent', '|', 'link',
                'insertTable', 'blockQuote', '|', 'uploadImage'
            ],
            shouldNotGroupWhenFull: false
        },
        heading: {
            options: [
                { model: 'paragraph', title: 'Đoạn văn', class: 'ck-heading_paragraph' },
                { model: 'heading2', view: 'h2', title: 'Tiêu đề 1', class: 'ck-heading_heading2' },
                { model: 'heading3', view: 'h3', title: 'Tiêu đề 2', class: 'ck-heading_heading3' },
                { model: 'heading4', view: 'h4', title: 'Tiêu đề 3', class: 'ck-heading_heading4' }
            ]
        },
        table: { contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'] },
        image: { toolbar: ['imageTextAlternative', 'toggleImageCaption', '|', 'imageStyle:inline', 'imageStyle:block', 'imageStyle:side', '|', 'resizeImage'] },
        link: { addTargetToExternalLinks: true, defaultProtocol: 'https://' },
        htmlSupport: {
            allow: [{
                name: /^(p|br|ul|ol|li|span|h2|h3|h4|blockquote|pre|code|a|figure|figcaption|img|table|thead|tbody|tr|th|td)$/,
                attributes: true,
                classes: true,
                styles: true
            }]
        },
        placeholder: 'Nhập nội dung chi tiết của trang'
    }).then(editor => {
        editor.model.document.on('change:data', () => { field.value = editor.getData(); });
        field.form.addEventListener('submit', () => { field.value = editor.getData(); }, true);
    }).catch(() => window.TopstarNotify?.error('Không thể mở trình soạn thảo. Bạn có thể nhập nội dung trong ô văn bản.'));
})();
