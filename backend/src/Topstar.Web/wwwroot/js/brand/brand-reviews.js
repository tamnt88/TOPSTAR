(function () {
    'use strict';
    document.querySelectorAll('.js-brand-review-form').forEach(function (form) {
        var inputs = Array.from(form.querySelectorAll('input[name="rating"]'));
        var error = form.querySelector('[data-rating-error]');
        function clear() { form.classList.remove('has-rating-error'); inputs.forEach(function (input) { input.setAttribute('aria-invalid', 'false'); }); }
        inputs.forEach(function (input) { input.addEventListener('change', clear); });
        form.addEventListener('submit', function (event) {
            if (!inputs.some(function (input) { return input.checked; })) {
                event.preventDefault();
                form.classList.add('has-rating-error');
                inputs.forEach(function (input) { input.setAttribute('aria-invalid', 'true'); });
                if (error) error.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        });
    });
})();
