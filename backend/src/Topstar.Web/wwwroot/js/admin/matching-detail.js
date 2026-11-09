(() => {
    'use strict';
    const applicationForm = document.querySelector('.js-application-koc-form');
    if (applicationForm) {
        const required = Number(applicationForm.dataset.requiredCount || 0);
        const checks = [...applicationForm.querySelectorAll('.js-application-koc-check')];
        const selected = applicationForm.querySelector('.js-application-koc-selected');
        const refreshApplications = () => {
            const count = checks.filter(x => x.checked).length;
            if (selected) selected.textContent = String(count);
            checks.forEach(x => x.closest('label')?.classList.toggle('is-selected', x.checked));
            applicationForm.classList.remove('is-invalid');
        };
        checks.forEach(check => check.addEventListener('change', () => {
            if (checks.filter(x => x.checked).length > required) check.checked = false;
            refreshApplications();
        }));
        applicationForm.addEventListener('submit', event => {
            const valid = checks.filter(x => x.checked).length === required;
            applicationForm.classList.toggle('is-invalid', !valid);
            if (!valid) event.preventDefault();
        });
        refreshApplications();
    }

    const form = document.querySelector('.js-direct-koc-form');
    if (!form) return;
    const required = Number(form.dataset.requiredCount || 0);
    const checks = [...form.querySelectorAll('.js-direct-koc-check')];
    const selected = form.querySelector('.js-direct-koc-selected');
    const search = form.querySelector('.js-direct-koc-search');

    const refresh = () => {
        const count = checks.filter(x => x.checked).length;
        selected.textContent = String(count);
        checks.forEach(x => x.closest('tr')?.classList.toggle('is-selected', x.checked));
        form.classList.toggle('is-invalid', count > required);
    };
    checks.forEach(check => check.addEventListener('change', () => {
        if (checks.filter(x => x.checked).length > required) check.checked = false;
        refresh();
    }));
    search?.addEventListener('input', () => {
        const keyword = search.value.trim().toLocaleLowerCase('vi');
        form.querySelectorAll('tbody tr').forEach(row => row.hidden = keyword !== '' && !row.dataset.search.includes(keyword));
    });
    form.addEventListener('submit', event => {
        const count = checks.filter(x => x.checked).length;
        const valid = count > 0 && count <= required;
        form.classList.toggle('is-invalid', !valid);
        if (!valid) event.preventDefault();
    });
    refresh();
})();
