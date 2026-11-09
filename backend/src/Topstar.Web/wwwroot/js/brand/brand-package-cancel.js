(function () {
    'use strict';
    var modal=document.getElementById('brandPackageCancelModal');
    if(!modal)return;
    var id=document.getElementById('brandPackageCancelId');
    var name=document.getElementById('brandPackageCancelName');
    var packageName=document.getElementById('brandPackageCancelPackageName');
    var reason=document.getElementById('brandPackageCancelReason');
    var count=document.getElementById('brandPackageCancelCount');
    var form=document.getElementById('brandPackageCancelForm');
    var submit=document.getElementById('brandPackageCancelSubmit');
    function prepare(button){if(!button)return;id.value=button.dataset.id||'';packageName.value=button.dataset.name||'';name.textContent=button.dataset.name||'';reason.value='';count.textContent='0';reason.classList.remove('is-invalid');reason.setAttribute('aria-invalid','false');}
    modal.addEventListener('show.bs.modal',function(e){prepare(e.relatedTarget&&e.relatedTarget.closest('.js-cancel-package'));});
    reason.addEventListener('input',function(){var length=reason.value.trim().length;count.textContent=String(reason.value.length);reason.classList.toggle('is-invalid',length>0&&length<10);reason.setAttribute('aria-invalid',length>0&&length<10?'true':'false');});
    form.addEventListener('submit',function(e){var length=reason.value.trim().length;var valid=length>=10&&length<=500;if(!id.value){e.preventDefault();bootstrap.Modal.getOrCreateInstance(modal).hide();window.location.reload();return;}if(!valid){e.preventDefault();reason.classList.add('is-invalid');reason.setAttribute('aria-invalid','true');reason.focus();return;}reason.classList.remove('is-invalid');reason.setAttribute('aria-invalid','false');submit.disabled=true;submit.innerHTML='<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang hủy...';});
    form.addEventListener('submit', function(event) { window.TopstarForms?.guardNativeSubmit(form, event); }, true);
    var restore=document.getElementById('brandPackageCancelRestore');
    if(restore&&restore.dataset.open==='true'){id.value=restore.dataset.id||'';packageName.value=restore.dataset.name||'';name.textContent=restore.dataset.name||'';reason.value=restore.dataset.reason||'';count.textContent=String(reason.value.length);reason.classList.add('is-invalid');reason.setAttribute('aria-invalid','true');var error=document.getElementById('brandPackageCancelReasonError');if(error&&restore.dataset.error)error.textContent=restore.dataset.error;bootstrap.Modal.getOrCreateInstance(modal).show();modal.addEventListener('shown.bs.modal',function(){reason.focus();},{once:true});}
}());
