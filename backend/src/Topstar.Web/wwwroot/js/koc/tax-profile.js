(function($){$(function(){var form=document.getElementById('kocTaxForm');if(!form||!form.querySelector('button[type="submit"]'))return;
function check(input){var ok=!!input.value.trim();input.classList.toggle('is-invalid',!ok);input.setAttribute('aria-invalid',String(!ok));var error=input.parentElement.querySelector('.invalid-feedback');if(error)error.textContent=ok?'':input.name==='TaxNumber'?'Vui lòng nhập mã số thuế.':'Vui lòng nhập số CCCD.';return ok;}
form.addEventListener('submit',function(e){var valid=true;['TaxNumber','IdentityNumber'].forEach(function(name){if(!check(form.querySelector('[name="'+name+'"]')))valid=false;});if(!valid){e.preventDefault();e.stopImmediatePropagation();form.querySelector('.is-invalid').focus();}},true);
$(form).on('input','[name="TaxNumber"],[name="IdentityNumber"]',function(){if(this.classList.contains('is-invalid'))check(this);});
});})(jQuery);
