(function ($) {
    'use strict';
    const root=document.querySelector('.balance-admin');
    if(!root || !document.getElementById('withdrawalTable'))return;
    const escape=value=>$('<div>').text(value??'').html().replace(/"/g,'&quot;').replace(/'/g,'&#39;');
    const money=value=>Number(value||0).toLocaleString('vi-VN')+' đ';
    const date=value=>value?new Date(value).toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh',hour:'2-digit',minute:'2-digit',day:'2-digit',month:'2-digit',year:'numeric'}):'—';
    const labels={Pending:['Chờ duyệt','fa-clock'],Approved:['Chờ chuyển tiền','fa-money-bill-transfer'],Paid:['Đã chuyển tiền','fa-circle-check'],Rejected:['Đã từ chối','fa-circle-xmark'],Cancelled:['Đã hủy','fa-ban']};
    const tables={};let approvals=[];let detailRequest=0;
    const notification=(message,ok)=>window.TopstarNotify?.[ok?'success':'error'](message);
    function summary(values){if(!values)return;Object.entries(values).forEach(([key,value])=>root.querySelectorAll('[data-balance-stat="'+key+'"]').forEach(e=>e.textContent=['pending','approved'].includes(key)?String(value):money(value)));}
    function table(prefix,scope,columns){
        return $('#'+prefix+'Table').DataTable(TopstarLists.adapt($('#'+prefix+'Table'),{
            serverSide:true,processing:true,pageLength:20,lengthChange:false,searching:false,autoWidth:false,order:[[1,'desc']],
            ajax:{url:location.pathname+'?handler=Data',data:d=>({draw:d.draw,start:d.start,length:d.length,scope,keyword:$('#'+prefix+'Keyword').val().trim(),status:scope==='withdrawals'?$('#withdrawalStatus').val():'',orderColumn:d.order?.[0]?.column??1,orderDirection:d.order?.[0]?.dir??'desc'}),dataSrc:json=>{summary(json.summary);return json.data||[];}},
            columns,createdRow:(row)=>{const headings=[...document.querySelectorAll('#'+prefix+'Table thead th')].map(th=>th.textContent.trim());row.querySelectorAll('td').forEach((td,i)=>td.dataset.label=headings[i]||'');},dom:"t<'balance-table-footer d-flex justify-content-between align-items-center gap-3 flex-wrap pt-3'ip>",
            language:{emptyTable:scope==='refunds'?'Chưa có khoản đủ điều kiện hoàn vào số dư.':'Chưa có yêu cầu rút tiền.',zeroRecords:'Không tìm thấy kết quả phù hợp.',processing:'Đang tải dữ liệu…',info:'Hiển thị _START_–_END_ / _TOTAL_ kết quả',infoEmpty:'Hiển thị 0 / 0 kết quả',infoFiltered:'',paginate:{first:'«',previous:'‹',next:'›',last:'»'}}
        }));
    }
    tables.withdrawals=table('withdrawal','withdrawals',[
        {data:'rowNumber',orderable:false},
        {data:null,render:r=>`<strong>${escape(r.brandName)}</strong><small>${escape(date(r.createdAtUtc))}</small>`},
        {data:null,orderable:false,render:r=>`${escape(r.bankName)}<small>•••• ${escape(r.accountTail)}</small>`},
        {data:null,className:'text-end',render:r=>`<strong>${money(r.amount)}</strong><small>Thực nhận: ${money(r.netAmount)}</small>`},
        {data:null,orderable:false,render:r=>{const item=labels[r.status]||[r.status,'fa-circle-info'];return `<span class="balance-status is-${escape(r.status.toLowerCase())}"><i class="fa-solid ${item[1]} me-1"></i>${escape(item[0])}</span>${r.firstApprovedBy&&r.status==='Pending'?'<small>Đang chờ duyệt lần hai</small>':''}`;}},
        {data:null,orderable:false,className:'text-center',render:r=>`<button type="button" class="balance-row-action" data-review="${escape(r.id)}" title="Xem và xử lý" aria-label="Xem và xử lý yêu cầu của ${escape(r.brandName)}"><i class="fa-solid fa-eye"></i></button>`}
    ]);
    function ensureRefundTable(){if(tables.refunds)return;tables.refunds=table('refund','refunds',[
        {data:'rowNumber',orderable:false},{data:'brandName',render:escape},
        {data:null,orderable:false,render:r=>`<strong>${escape(r.campaignTitle)}</strong><small>${escape(r.orderNumber)}</small>`},
        {data:'refundable',className:'text-end',render:v=>`<strong>${money(v)}</strong>`},
        {data:null,orderable:false,className:'text-center',render:r=>`<button type="button" class="balance-row-action" data-credit="${escape(r.id)}" title="Duyệt hoàn vào số dư" aria-label="Duyệt hoàn vào số dư của ${escape(r.brandName)}"><i class="fa-solid fa-rotate-left"></i></button>`}
    ]);}
    function setTab(name){
        if(!['withdrawals','refunds','policy'].includes(name))name='withdrawals';
        $('[data-balance-tab]').each(function(){const active=this.dataset.balanceTab===name;$(this).toggleClass('is-active',active).attr('aria-selected',String(active));this.tabIndex=active?0:-1;});
        $('[data-balance-panel]').each(function(){this.hidden=this.dataset.balancePanel!==name;});
        const url=new URL(location.href);url.searchParams.set('tab',name);history.replaceState(history.state,'',url);
        if(name==='refunds'){ensureRefundTable();loadApprovals();}tables[name]?.columns.adjust();
    }
    $('[data-balance-tab]').on('click',function(){setTab(this.dataset.balanceTab);}).on('keydown',function(e){if(!['ArrowRight','ArrowLeft','Home','End'].includes(e.key))return;e.preventDefault();const tabs=[...document.querySelectorAll('[data-balance-tab]')];const index=e.key==='Home'?0:e.key==='End'?tabs.length-1:(tabs.indexOf(this)+(e.key==='ArrowRight'?1:-1)+tabs.length)%tabs.length;setTab(tabs[index].dataset.balanceTab);tabs[index].focus();});
    ['withdrawal','refund'].forEach(prefix=>{
        const reload=()=>{const t=tables[prefix==='refund'?'refunds':'withdrawals'];if(t)t.page.len(Number($('#'+prefix+'PageLength').val())||20).ajax.reload(null,true);};
        $('#'+prefix+'Filters').on('submit',e=>{e.preventDefault();reload();});
        $('#'+prefix+'PageLength').on('change',reload);
        if(prefix==='withdrawal')$('#withdrawalStatus').on('change',reload);
    });
    const withdrawalModal=bootstrap.Modal.getOrCreateInstance(document.getElementById('withdrawalModal'));
    const creditModal=bootstrap.Modal.getOrCreateInstance(document.getElementById('creditModal'));
    async function openDetail(id){const request=++detailRequest;$('#withdrawalDetail').html('<div class="text-center text-muted py-5"><i class="fa-solid fa-spinner fa-spin me-2"></i>Đang tải yêu cầu…</div>');withdrawalModal.show();try{const html=await $.get(location.pathname,{handler:'Detail',id});if(request!==detailRequest)return;$('#withdrawalDetail').html(html);prepareFields(document.getElementById('withdrawalDetail'));}catch(_){if(request!==detailRequest)return;$('#withdrawalDetail').html('<div class="balance-form-error">Không tải được yêu cầu. Vui lòng đóng và thử lại.</div>');}}
    $(root).on('click','[data-review]',function(){openDetail(this.dataset.review);});
    function openCredit(row,approval){
        const form=document.querySelector('#creditModal form');form.reset();$(form).find('.balance-form-error').remove();$(form).find('.is-invalid').removeClass('is-invalid');$(form).find('.invalid-feedback,.balance-error-icon').remove();
        $('#creditBrand').text(row.brandName);$('#creditCampaign').text(row.campaignTitle);$('#creditMaximum').text(money(approval?.amount??row.refundable));
        $('#creditFundingId').val(approval?.fundingId??row.id);$('#creditOperation').val(approval?.operation??crypto.randomUUID());
        $('#creditAmount').val(Number(approval?.amount??row.refundable).toLocaleString('vi-VN')).prop('readOnly',!!approval).attr('data-maximum',approval?.amount??row.refundable);
        creditModal.show();
    }
    $(root).on('click','[data-credit]',function(){const row=tables.refunds.rows().data().toArray().find(r=>r.id===this.dataset.credit);if(row)openCredit(row);});
    async function loadApprovals(){
        try{approvals=await $.getJSON(location.pathname,{handler:'Approvals'});$('#creditApprovals').html(approvals.map((a,i)=>`<div class="balance-approval"><div><strong><i class="fa-solid fa-user-shield me-2"></i>Chờ duyệt lần hai · ${money(a.amount)}</strong><small>${escape(a.brandName)} · ${escape(a.campaignTitle)}</small></div><button type="button" class="btn btn-outline-primary" data-credit-approval="${i}" ${a.canApprove?'':'disabled'}>${a.canApprove?'Duyệt lần hai':'Cần quản trị viên khác'}</button></div>`).join(''));}
        catch(_){$('#creditApprovals').html('<div class="balance-form-error">Không tải được hàng đợi duyệt lần hai. <button type="button" class="btn btn-sm btn-outline-danger" data-retry-approvals>Thử lại</button></div>');}
    }
    $(root).on('click','[data-credit-approval]',function(){const a=approvals[Number(this.dataset.creditApproval)];if(a?.canApprove)openCredit({brandName:a.brandName,campaignTitle:a.campaignTitle},a);}).on('click','[data-retry-approvals]',loadApprovals);
    function prepareFields(scope){scope.querySelectorAll('[data-balance-form] input:not([type=hidden]):not([type=checkbox]),[data-balance-form] textarea').forEach(field=>{if(!field.closest('.balance-field')){const wrapper=document.createElement('div');wrapper.className='balance-field';field.before(wrapper);wrapper.append(field);}});}
    function validate(field){
        field.setCustomValidity('');
        if(field.matches('[data-money]')){const raw=field.value.replace(/\./g,'');if(!/^\d+$/.test(raw)||Number(raw)<=0||Number(raw)>Number(field.dataset.maximum))field.setCustomValidity(field.dataset.error);}
        if(field.matches('[data-vnd]')){const raw=field.value.replace(/\./g,'');if(!/^\d+$/.test(raw)||Number(raw)<Number(field.min)||raw.length>18)field.setCustomValidity(field.dataset.error);}
        if(field.type==='file'&&field.files[0]&&(field.files[0].size>5*1024*1024||!/(\.jpe?g|\.png|\.webp)$/i.test(field.files[0].name)))field.setCustomValidity(field.dataset.error);
        const valid=field.checkValidity();const wrapper=field.closest('.balance-field');
        field.classList.toggle('is-invalid',!valid);field.setAttribute('aria-invalid',String(!valid));
        if(wrapper){$(wrapper).find('.invalid-feedback,.balance-error-icon').remove();if(!valid){const feedback=document.createElement('div');feedback.className='invalid-feedback';feedback.id=(field.id||field.name)+'-error';feedback.textContent=field.dataset.error||'Vui lòng kiểm tra thông tin đã nhập.';wrapper.append(feedback);$(wrapper).append('<i class="fa-solid fa-circle-exclamation balance-error-icon" aria-hidden="true"></i>');field.setAttribute('aria-describedby',feedback.id);}else field.removeAttribute('aria-describedby');}
        return valid;
    }
    prepareFields(root);
    root.addEventListener('submit',async function(e){
        const form=e.target;if(!form.matches('[data-balance-form]'))return;
        e.preventDefault();e.stopImmediatePropagation();if(form.dataset.saving==='true')return;
        prepareFields(form);let firstInvalid;
        form.querySelectorAll('input:not([type=hidden]):not([type=checkbox]),textarea').forEach(field=>{if(!field.disabled&&!validate(field))firstInvalid??=field;});if(firstInvalid){firstInvalid.focus();return;}
        $(form).find('.balance-form-error').remove();const data=new FormData(form);if(data.has('amount'))data.set('amount',String(data.get('amount')).replace(/\./g,''));form.querySelectorAll('[data-vnd]').forEach(f=>data.set(f.name,f.value.replace(/\./g,'')));
        const buttons=[...form.querySelectorAll('button[type=submit],button:not([type])')];const old=buttons.map(b=>b.innerHTML);buttons.forEach(b=>{b.disabled=true;b.innerHTML='<i class="fa-solid fa-spinner fa-spin me-2"></i>Đang xử lý';});form.dataset.saving='true';
        try{const result=await $.ajax({url:form.action,method:'POST',data,processData:false,contentType:false,dataType:'json'});notification(result.message,result.success);
            if(result.success){const modal=form.closest('.modal');if(modal)bootstrap.Modal.getInstance(modal)?.hide();Object.values(tables).forEach(t=>t.ajax.reload(null,false));loadApprovals();}
            else{$('<div class="balance-form-error" role="alert">').text(result.message||'Không thể thực hiện yêu cầu.').prependTo(form.querySelector('.modal-body')||form);}
        }catch(xhr){const message=xhr.status===401||xhr.status===403?'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền xử lý.':'Chưa xác nhận được kết quả. Hãy tải lại danh sách để kiểm tra trước khi gửi lại.';notification(message,false);$('<div class="balance-form-error" role="alert">').text(message).prependTo(form.querySelector('.modal-body')||form);}
        finally{delete form.dataset.saving;buttons.forEach((b,i)=>{b.disabled=false;b.innerHTML=old[i];});}
    },true);
    $(root).on('input change','[data-balance-form] input,[data-balance-form] textarea',function(){if(this.matches('[data-money],[data-vnd]')){const digits=this.value.replace(/\D/g,'');this.value=digits.replace(/\B(?=(\d{3})+(?!\d))/g,'.');}if(this.classList.contains('is-invalid'))validate(this);});
    setTab(new URL(location.href).searchParams.get('tab')||'withdrawals');
})(jQuery);
