(() => {
    'use strict';

    const init = (container) => {
        if (!window.jQuery?.fn?.colorbox) return;

        const root = container || document;
        window.jQuery(root)
            .find('[data-koc-colorbox]')
            .addBack('[data-koc-colorbox]')
            .each(function () {
                if (this.dataset.kocColorboxBound === 'true') return;

                const element = this;
                const gallery = element.dataset.gallery || 'koc-images';
                window.jQuery(element).colorbox({
                    rel: gallery,
                    photo: true,
                    className: 'koc-colorbox',
                    maxWidth: '92%',
                    maxHeight: '86%',
                    fixed: true,
                    transition: 'fade',
                    speed: 180,
                    fadeOut: 160,
                    overlayClose: false,
                    trapFocus: true,
                    returnFocus: true,
                    current: 'Ảnh {current}/{total}',
                    previous: 'Trước',
                    next: 'Sau',
                    close: 'Đóng',
                    title: () => element.dataset.caption || element.title || ''
                });
                element.dataset.kocColorboxBound = 'true';
            });
    };

    window.KocGallery = { init };
    window.jQuery(() => init(document));
})();
