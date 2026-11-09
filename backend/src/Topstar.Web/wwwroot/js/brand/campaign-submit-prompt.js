((window, $) => {
    'use strict';
    $(async () => {
        const form = document.getElementById('campaign-submit-review');
        if (!form || !window.TopstarNotify) return;
        const approved = await window.TopstarNotify.confirm(
            'Thông tin chiến dịch đã được lưu. Bạn muốn gửi Topstar xét duyệt ngay?',
            {
                title: 'Gửi chiến dịch xét duyệt ngay?',
                confirmText: 'Gửi xét duyệt',
                cancelText: 'Tiếp tục chỉnh sửa'
            });
        if (!approved) return;
        form.dataset.confirmApproved = 'true';
        form.requestSubmit();
    });
})(window, window.jQuery);
