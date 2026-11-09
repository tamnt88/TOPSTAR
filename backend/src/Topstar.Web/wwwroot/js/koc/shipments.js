(() => {
    'use strict';
    const tableElement = document.getElementById('kocShipmentTable');
    const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'})[c]);
    const formatDate = value => window.TopstarFormat.dateTime(value);
    const statuses = {
        Created:['Chờ Brand gửi','is-created','fa-clock'], InTransit:['Đang giao','is-transit','fa-truck-fast'],
        Delivered:['Đã nhận','is-delivered','fa-circle-check'], Issue:['Có sự cố','is-issue','fa-triangle-exclamation'],
        Returned:['Đã hoàn hàng','is-returned','fa-arrow-rotate-left']
    };
    const trackingText = code => ({PickedUp:'Đã lấy hàng',AtHub:'Đến kho trung chuyển',OutForDelivery:'Đang giao đến bạn',DeliveryAttempt:'Đã thử giao hàng',Exception:'Vận chuyển có sự cố'})[code] || 'Cập nhật hành trình';
    const badge = status => { const s=statuses[status]||['Chưa xác định','is-created','fa-circle-question']; return `<span class="shipment-status ${s[1]}"><i class="fa-solid ${s[2]}"></i>${s[0]}</span>`; };
    const detailHtml = row => {
        const tracking = [...(row.tracking||[])].sort((a,b)=>new Date(a.occurredAtUtc)-new Date(b.occurredAtUtc));
        const checkpoints = tracking.length ? `<section class="shipment-timeline"><h4><i class="fa-solid fa-location-arrow"></i> Hành trình vận đơn</h4><div class="shipment-timeline-track">${tracking.map(x=>`<div class="shipment-checkpoint"><span></span><div><strong>${escapeHtml(trackingText(x.eventCode))}</strong><time>${formatDate(x.occurredAtUtc)}</time><p>${escapeHtml(x.description)}</p>${x.location?`<small><i class="fa-solid fa-location-dot"></i> ${escapeHtml(x.location)}</small>`:''}</div></div>`).join('')}</div></section>` : '';
        const issue = row.issueNote ? `<div class="shipment-issue"><i class="fa-solid fa-triangle-exclamation"></i><div><strong>Sự cố đã báo cho Brand</strong><p>${escapeHtml(row.issueNote)}</p></div></div>` : '';
        const evidence = (row.evidence||[]).map(x=>{
            const label=x.actorRole==='BRAND'?'Ảnh Brand gửi':x.evidenceType==='KocReceiptProof'?'Ảnh KOC nhận':'Ảnh KOC gửi';
            const description=x.caption||label;
            return `<a href="${escapeHtml(x.fileUrl)}" target="_blank" rel="noopener" class="btn btn-outline-secondary btn-sm shipment-evidence-link" data-koc-colorbox data-gallery="shipment-evidence-${row.shipmentId}" data-caption="${escapeHtml(description)}" title="${escapeHtml(description)}" aria-label="Phóng to ${escapeHtml(description)}"><i class="fa-regular fa-image"></i> ${label}</a>`;
        }).join('');
        const modalData = `data-shipment-id="${escapeHtml(row.shipmentId)}" data-campaign-title="${escapeHtml(row.campaignTitle)}"`;
        const actions = row.status==='InTransit' ? `<button class="btn btn-outline-danger btn-sm" data-bs-toggle="modal" data-bs-target="#issueShipmentModal" ${modalData}><i class="fa-solid fa-triangle-exclamation"></i> Báo sự cố</button><button class="btn btn-success btn-sm" data-bs-toggle="modal" data-bs-target="#deliveredShipmentModal" ${modalData}><i class="fa-solid fa-box-open"></i> Đã nhận hàng</button>` : '';
        return `<div class="shipment-row-detail"><div class="shipment-info-grid"><div><span><i class="fa-solid fa-user"></i> Người nhận</span><strong>${escapeHtml(row.receiverName)}</strong><small>${escapeHtml(row.receiverPhone)}</small></div><div><span><i class="fa-solid fa-location-dot"></i> Địa chỉ nhận</span><strong>${escapeHtml(row.address)}</strong><small>Snapshot tại thời điểm tạo đơn</small></div><div><span><i class="fa-solid fa-clock-rotate-left"></i> Lịch sử</span><strong>${(row.history||[]).length} sự kiện</strong><small>${(row.evidence||[]).length} ảnh bằng chứng</small></div></div>${issue}${checkpoints}<div class="shipment-card-footer"><div class="shipment-secondary-actions">${evidence}<button class="btn btn-outline-primary btn-sm" data-bs-toggle="modal" data-bs-target="#evidenceShipmentModal" ${modalData}><i class="fa-solid fa-camera"></i> Thêm ảnh</button></div><div class="shipment-primary-actions">${actions}</div></div></div>`;
    };
    if (tableElement && window.jQuery?.fn?.DataTable) {
        const table = window.jQuery(tableElement).DataTable(TopstarLists.adapt(window.jQuery(tableElement), {
            processing:true, serverSide:true, searching:false, ordering:false, pageLength:10, lengthChange:false,
            ajax:{url:tableElement.dataset.ajaxUrl,type:'GET',data:d=>{d.query=document.getElementById('shipmentSearch')?.value||'';d.status=document.getElementById('shipmentStatus')?.value||'';},error:()=>window.KocToast?.show('Không thể tải danh sách vận đơn. Vui lòng thử lại.','error')},
            columns:[
                {data:null,width:'48px',render:(_d,_t,_r,m)=>m.settings._iDisplayStart+m.row+1},
                {data:null,render:(_d,_t,r)=>`<div class="shipment-table-campaign"><span><i class="fa-solid fa-box"></i></span><div><strong title="${escapeHtml(r.campaignTitle)}">${escapeHtml(r.campaignTitle)}</strong><small>Sản phẩm chiến dịch</small></div></div>`},
                {data:null,render:(_d,_t,r)=>`<div class="shipment-table-carrier"><strong>${escapeHtml(r.carrier||'Chưa bàn giao')}</strong><small><i class="fa-solid fa-barcode"></i> ${escapeHtml(r.trackingCode||'Chưa có mã vận đơn')}</small></div>`},
                {data:null,render:(_d,_t,r)=>{const latest=r.tracking?.[0];return `<div class="shipment-table-update"><strong>${latest?escapeHtml(trackingText(latest.eventCode)):'Chưa có checkpoint'}</strong><small>${latest?formatDate(latest.occurredAtUtc):formatDate(r.history?.at(-1)?.createdAtUtc)}</small></div>`;}},
                {data:'status',render:value=>badge(value)},
                {data:null,width:'64px',render:(_d,_t,r)=>`<button type="button" class="shipment-table-toggle" data-id="${r.shipmentId}" aria-expanded="false" title="Xem chi tiết"><i class="fa-solid fa-chevron-down"></i></button>`}
            ],
            language:{processing:'Đang tải dữ liệu...',emptyTable:'Chưa có vận đơn nào.',zeroRecords:'Không tìm thấy vận đơn phù hợp.',info:'Hiển thị _START_–_END_ trong _TOTAL_ vận đơn',infoEmpty:'Không có dữ liệu',infoFiltered:'',paginate:{first:'Đầu',previous:'<i class="fa-solid fa-chevron-left"></i>',next:'<i class="fa-solid fa-chevron-right"></i>',last:'Cuối'}},
            layout:{topStart:null,topEnd:null,bottomStart:'info',bottomEnd:'paging'}
        }));
        window.jQuery(tableElement).on('click','.shipment-table-toggle',function(){
            const tr=this.closest('tr'), row=table.row(tr), open=!row.child.isShown();
            if(open) {
                row.child(detailHtml(row.data()),'shipment-child-row').show();
                window.KocGallery?.init(tr.nextElementSibling);
            } else row.child.hide();
            this.classList.toggle('is-open',open);this.setAttribute('aria-expanded',String(open));
        });
    }
    document.querySelectorAll('.shipment-modal').forEach(modal=>{
        modal.addEventListener('show.bs.modal',event=>{
            const trigger=event.relatedTarget;
            const form=modal.querySelector('form');
            form?.reset();
            form?.querySelectorAll('.is-invalid').forEach(x=>x.classList.remove('is-invalid'));
            const id=form?.querySelector('input[name="id"]');
            if(id) id.value=trigger?.dataset.shipmentId||'';
            const campaign=modal.querySelector('[data-shipment-campaign]');
            if(campaign) campaign.textContent=trigger?.dataset.campaignTitle||'chiến dịch này';
        });
    });
    document.querySelectorAll('.shipment-validate-form').forEach(form=>{
        form.querySelectorAll('textarea[name="caption"]').forEach(field=>{
            field.dataset.minlength='10';
            field.setAttribute('aria-required','true');
            if(!field.nextElementSibling?.classList.contains('invalid-feedback')){
                const feedback=document.createElement('div');
                feedback.className='invalid-feedback';
                feedback.textContent='Vui lòng nhập ghi chú ảnh từ 10 đến 500 ký tự.';
                field.insertAdjacentElement('afterend',feedback);
            }
            const policy=form.querySelector('.form-text');
            if(policy) policy.innerHTML='<i class="fa-solid fa-lock"></i> Ảnh và ghi chú được lưu vào lịch sử, không thể chỉnh sửa hoặc xóa sau khi gửi.';
        });
        form.addEventListener('submit',e=>{let valid=true;form.querySelectorAll('[data-minlength]').forEach(f=>{const bad=f.value.trim().length<Number(f.dataset.minlength||0);f.classList.toggle('is-invalid',bad);f.setAttribute('aria-invalid',String(bad));valid&=!bad;});form.querySelectorAll('[data-required-file]').forEach(f=>{const file=f.files?.[0];const bad=!file||file.size>5242880||!['image/jpeg','image/png','image/webp'].includes(file.type);f.classList.toggle('is-invalid',bad);f.setAttribute('aria-invalid',String(bad));valid&=!bad;});if(!valid){e.preventDefault();form.querySelector('.is-invalid')?.focus();}});
        form.querySelectorAll('textarea,input').forEach(f=>{f.addEventListener('input',()=>f.classList.remove('is-invalid'));f.addEventListener('change',()=>f.classList.remove('is-invalid'));});
    });
})();
