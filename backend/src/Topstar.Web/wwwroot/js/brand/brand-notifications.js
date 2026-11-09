(function ($) {
 'use strict';
 const element=document.getElementById('brandNotificationTable'); if(!element||!$.fn.DataTable)return;
 const esc=v=>$('<div>').text(v==null?'':String(v)).html(), date=v=>window.TopstarFormat.dateTime(v);
 const icon=t=>({Campaign:'fa-bullhorn',Wallet:'fa-wallet',Payout:'fa-money-bill-transfer',Dispute:'fa-scale-balanced',Support:'fa-headset'}[t]||'fa-bell');
 const labels={Created:'Đã gửi',Read:'Đã đọc',Opened:'Đã mở công việc',Hidden:'Đã lưu trữ',Restored:'Đã khôi phục'};
 const button=(r,a,l,i,s='outline-primary')=>`<button class="btn btn-${s} btn-sm" type="button" data-notification-action="${a}" data-id="${esc(r.id)}" title="${l}"><i class="fa-solid ${i}"></i><span>${l}</span></button>`;
 const summary=s=>{const u=Number(s?.unreadCount||0),a=Number(s?.activeCount||0),h=Number(s?.archivedCount||0),r=Math.max(0,a-u);$('#brandNotificationTotal').text((a+h).toLocaleString('vi-VN'));$('#brandUnreadCount,#brandUnreadTabCount').text(u.toLocaleString('vi-VN'));$('#brandReadCount,#brandReadTabCount').text(r.toLocaleString('vi-VN'));$('#brandArchivedCount,#brandArchivedTabCount').text(h.toLocaleString('vi-VN'));$('#brandActiveTabCount').text(a.toLocaleString('vi-VN'));document.getElementById('brandMarkAll').hidden=u===0;};
 const table=$(element).DataTable(TopstarLists.configure(element,{
  serverSide:true,searching:false,ordering:false,autoWidth:false,
  ajax:{url:location.pathname+'?handler=Data',data:d=>{d.query=$('#brandNotificationSearch').val();d.filter=$('#brandNotificationFilter').val();},dataSrc:j=>{summary(j.summary);return j.data||[];}},
  createdRow:(row,data)=>{if(data.isVisible&&!data.isRead)row.classList.add('is-unread');},
  columns:[
   {data:null,className:'text-center',render:(_d,_t,_r,m)=>m.settings._iDisplayStart+m.row+1},
   {data:null,render:r=>`<div class="brand-notification-name"><span class="brand-notification-icon"><i class="fa-solid ${icon(r.type)}"></i></span><div><strong>${esc(r.title)}</strong><small>${esc(r.type||'Thông báo')}</small></div></div>`},
   {data:null,render:r=>`<div class="brand-notification-body">${esc(r.body)}</div>`+(r.history?.length?`<details class="brand-notification-history"><summary><i class="fa-solid fa-clock-rotate-left"></i> Lịch sử (${r.history.length})</summary><div>${r.history.map(e=>`<p><i></i><span>${esc(labels[e.eventType]||'Đã cập nhật')}<time>${esc(date(e.createdAtUtc))}</time></span></p>`).join('')}</div></details>`:'')},
   {data:'createdAtUtc',render:v=>`<div class="brand-notification-date"><strong>${esc(date(v))}</strong></div>`},
   {data:null,render:r=>!r.isVisible?'<span class="brand-notification-status is-archived"><i class="fa-solid fa-box-archive"></i> Đã lưu trữ</span>':r.isRead?'<span class="brand-notification-status is-read"><i class="fa-solid fa-check"></i> Đã đọc</span>':'<span class="brand-notification-status is-unread"><i class="fa-solid fa-circle"></i> Chưa đọc</span>'},
   {data:null,orderable:false,render:r=>`<div class="brand-notification-actions">${r.isVisible&&r.targetUrl?button(r,'open','Mở công việc','fa-arrow-up-right-from-square','primary'):''}${r.isVisible&&!r.isRead?button(r,'read','Đã đọc','fa-check'):''}${button(r,r.isVisible?'archive':'restore',r.isVisible?'Lưu trữ':'Khôi phục',r.isVisible?'fa-box-archive':'fa-rotate-left','outline-secondary')}</div>`}
  ],language:{emptyTable:'Chưa có thông báo trong nhóm này.'},layout:{topStart:null,topEnd:null,bottomStart:'info',bottomEnd:'paging'}
 },{query:'#brandNotificationSearch'},'#brandNotificationSize'));
 $('#brandNotificationFilters').on('submit',e=>{e.preventDefault();table.ajax.reload(null,true);});
 $('#brandNotificationSize').on('change',function(){table.page.len(Number(this.value)).draw();});
 $('[data-notification-filter]').on('click',function(){$('[data-notification-filter]').removeClass('is-active');this.classList.add('is-active');const f=this.dataset.notificationFilter;$('#brandNotificationFilter').val(f);$('#brandNotificationDescription').text({all:'Các thông báo đang hoạt động.',unread:'Các thông báo Brand chưa đọc.',read:'Các thông báo Brand đã đọc.',archived:'Các thông báo đã được lưu trữ.'}[f]);table.ajax.reload(null,true);});
 $('#brandNotificationClear').on('click',()=>{$('#brandNotificationSearch').val('');table.ajax.reload(null,true);});
 $(document).on('click','[data-notification-action]',async function(){const c=this,f=document.getElementById('brandNotificationFeedback');c.disabled=true;try{const body=new URLSearchParams({action:c.dataset.notificationAction,id:c.dataset.id||'00000000-0000-0000-0000-000000000000',__RequestVerificationToken:$('#notificationAntiForgery input').val()});const response=await fetch(location.pathname+'?handler=Action',{method:'POST',body});if(!response.ok)throw new Error('Không thể cập nhật thông báo. Vui lòng thử lại.');const result=await response.json();if(!result.success)throw new Error(result.message);if(result.targetUrl){const target=new URL(result.targetUrl,location.origin);if(target.origin===location.origin){location.href=target.href;return;}}f.textContent=result.message;f.hidden=false;table.ajax.reload(null,false);}catch(e){f.textContent=e.message||'Không thể kết nối máy chủ.';f.hidden=false;}finally{c.disabled=false;}});
})(jQuery);
