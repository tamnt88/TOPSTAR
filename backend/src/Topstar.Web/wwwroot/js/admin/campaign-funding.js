(function ($) {
    'use strict';

    $(function () {
        const query=new URL(location.href);
        const activeQueue=query.searchParams.get('queue')||'all';
        const buttons=$('[data-funding-queue]');
        buttons.each(function(){const active=this.dataset.fundingQueue===activeQueue;$(this).toggleClass('is-active',active).attr('aria-selected',String(active));});
        const settlement=activeQueue==='settlement';
        $('[data-funding-panel]').each(function(){this.hidden=this.dataset.fundingPanel!==(settlement?'settlement':'reconcile');this.classList.toggle('is-active',!this.hidden);});
        buttons.on('click',function(e){e.preventDefault();const url=new URL(location.href);url.searchParams.set('queue',this.dataset.fundingQueue);url.searchParams.set('list.campaignFundingTable.page','1');url.searchParams.delete('list.campaignFundingTable.status');location.assign(url);});
        $(document).on('click','.js-open-settlement',function(){const url=new URL(location.href);url.searchParams.set('queue','settlement');url.searchParams.set('list.campaignFundingTable.page','1');location.assign(url);});

        const paidModalElement = document.getElementById('confirmPaidModal');
        const paidModal = paidModalElement && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(paidModalElement) : null;
        $(document).on('click', '.js-open-paid', function () {
            $('#paidFundingId').val(this.dataset.id || '');
            $('#paidModalTitle').text(this.dataset.title || '');
            $('#paidModalOrder').text(this.dataset.order || '');
            $('#paidModalTotal').text(this.dataset.total || '');
            $('#paidReference').val('').removeClass('is-invalid');
            paidModal?.show();
        });

        const proofModalElement = document.getElementById('proofModal');
        const proofModal = proofModalElement && window.bootstrap ? window.bootstrap.Modal.getOrCreateInstance(proofModalElement) : null;
        $(document).on('click', '.js-proof-view', function () {
            const url = this.dataset.url || '';
            $('#proofImage').attr('src', url); $('#proofLink').attr('href', url);
            $('#proofNote').text(this.dataset.note || 'Không có ghi chú.');
            $('#proofTime').text(this.dataset.time || ''); proofModal?.show();
        });

        $(document).on('submit', '.js-validated-form, #confirmPaidForm', function (event) {
            $(this).find('[required]').each(function () { this.classList.toggle('is-invalid', !this.checkValidity()); });
            if (!this.checkValidity()) { event.preventDefault(); event.stopPropagation(); }
        });
        $(document).on('input change', '.admin-validation-field .form-control', function () {
            this.classList.toggle('is-invalid', !this.checkValidity());
        });
        $(document).on('change', '.admin-evidence-form select[name="evidenceType"]', function () {
            const form = this.closest('form');
            const amount = form && form.querySelector('.js-evidence-amount');
            if (!amount) return;
            if (this.value === 'Refund') amount.max = form.dataset.refundMax || '';
            else amount.removeAttribute('max');
            amount.classList.toggle('is-invalid', amount.value !== '' && !amount.checkValidity());
        });
    });
})(window.jQuery);
