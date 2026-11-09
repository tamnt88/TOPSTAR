document.addEventListener('DOMContentLoaded',function(){
 document.querySelectorAll('.js-package-cancellation-review').forEach(function(form){form.addEventListener('submit',function(e){var field=form.querySelector('textarea');var n=field.value.trim().length;field.classList.toggle('is-invalid',n<10||n>500);if(n<10||n>500){e.preventDefault();e.stopImmediatePropagation();field.focus();}},true);});
 var restore=document.getElementById('restorePackageCancellation');if(restore){var input=document.querySelector('.js-package-cancellation-review input[value="'+restore.dataset.id+'"]');var modal=input&&input.closest('.modal');if(modal)bootstrap.Modal.getOrCreateInstance(modal).show();}
});
