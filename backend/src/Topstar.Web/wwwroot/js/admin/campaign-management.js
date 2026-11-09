(function ($) {
    "use strict";
    const element = document.getElementById("adminCampaignTable");
    if (!element || !$.fn.DataTable) return;
    const labels = { Draft:["Bản nháp","draft","fa-pen-to-square"], Submitted:["Chờ duyệt","pending","fa-clock"], NeedEdit:["Cần cập nhật","attention","fa-pen"], Published:["Đang tuyển KOC","running","fa-users"], InProgress:["Đang thực hiện","running","fa-play"], Completed:["Đã hoàn thành","completed","fa-circle-check"], Rejected:["Bị từ chối","danger","fa-circle-xmark"], Paused:["Tạm ngừng","attention","fa-pause"], Cancelled:["Đã hủy","muted","fa-ban"] };
    let scope = "";
    const escape = value => $("<div>").text(value ?? "").html();
    const money = value => Number(value || 0).toLocaleString("vi-VN") + " đ";
    const status = value => { const item = labels[value] || [value,"muted","fa-circle-info"]; return `<span class="campaign-status is-${item[1]}"><i class="fa-solid ${item[2]}"></i>${item[0]}</span>`; };
    const table = $(element).DataTable(TopstarLists.adapt($(element), {
        processing:true, serverSide:true, pageLength:10, lengthChange:false, searching:false, autoWidth:false, order:[[1,"desc"]],
        ajax:{ url:location.pathname + "?handler=Data", data:data=>({ draw:data.draw,start:data.start,length:data.length,keyword:$("#adminCampaignKeyword").val().trim(),status:scope || $("#adminCampaignStatus").val(),orderColumn:data.order?.[0]?.column||0,orderDirection:data.order?.[0]?.dir||"desc" }), dataSrc:json=>{
            const s=json.summary||{};
            $("#adminCampaignTotal,#adminCampaignAllBadge").text(s.total||0);
            $("#adminCampaignDraft,#adminCampaignDraftBadge").text(s.draft||0);
            $("#adminCampaignPending,#adminCampaignQueueBadge").text(s.pendingReview||0);
            $("#adminCampaignRunning,#adminCampaignActiveBadge").text(s.running||0);
            $("#adminCampaignCompleted,#adminCampaignDoneBadge").text(s.completed||0);
            $("#adminCampaignAttention,#adminCampaignAttentionBadge").text(s.needsAttention||0);
            return json.data||[];
        }},
        columns:[
            {data:"rowNumber",orderable:false,searchable:false},
            {data:null,render:row=>`<div class="campaign-name"><i class="fa-solid fa-bullhorn"></i><div><strong>${escape(row.title)}</strong><small>${escape(row.packageCode)}</small></div></div>`},
            {data:"brandName",render:escape}, {data:"budgetAmount",className:"text-end",render:money},
            {data:null,render:row=>row.startDate||row.endDate?`<span class="campaign-dates"><i class="fa-regular fa-calendar"></i>${escape(row.startDate||"—")} <b>→</b> ${escape(row.endDate||"—")}</span>`:"—"},
            {data:"applicationCount",className:"text-center",render:value=>`<span class="campaign-count"><i class="fa-solid fa-users"></i>${value}</span>`},
            {data:"status",render:status},
            {data:null,orderable:false,searchable:false,className:"text-center",render:row=>`<div class="campaign-row-actions"><a class="campaign-view-button" href="/Admin/CampaignReview?id=${encodeURIComponent(row.id)}" title="${row.status==="Submitted"?"Xem và duyệt":"Xem chi tiết"}" aria-label="${row.status==="Submitted"?"Xem và duyệt":"Xem chi tiết"}"><i class="fa-solid ${row.status==="Submitted"?"fa-clipboard-check":"fa-eye"}"></i></a><button type="button" class="campaign-delete-button" data-campaign-delete data-id="${encodeURIComponent(row.id)}" data-name="${escape(row.title)}" title="Xóa chiến dịch" aria-label="Xóa chiến dịch ${escape(row.title)}"><i class="fa-solid fa-trash-can"></i></button></div>`}
        ],
        language:{processing:"Đang tải dữ liệu...",emptyTable:"Chưa có chiến dịch nào.",zeroRecords:"Không tìm thấy chiến dịch phù hợp.",info:"Hiển thị _START_–_END_ trong _TOTAL_ chiến dịch",infoEmpty:"Hiển thị 0 chiến dịch",paginate:{first:"Đầu",previous:"‹",next:"›",last:"Cuối"}}, dom:"t<'admin-campaign-footer'ip>"
    }));
    const reload=()=>table.page.len(Number($("#adminCampaignLength").val())||10).ajax.reload();
    $("#adminCampaignFilter").on("click",reload);
    $("#adminCampaignKeyword").on("keydown",event=>{if(event.key==="Enter")reload();});
    $("#adminCampaignStatus,#adminCampaignLength").on("change",reload);
    const applyScope = value => {
        scope=value||"";
        $("[data-campaign-scope]").removeClass("is-active");
        $(`[data-campaign-scope="${scope}"]`).addClass("is-active");
        $("#adminCampaignStatus").prop("disabled",!!scope).val(scope && !["Active","Attention"].includes(scope) ? scope : "");
        const descriptions={Draft:"Các chiến dịch Brand đang lưu nháp.",Submitted:"Các chiến dịch đang chờ Admin kiểm tra và ra quyết định.",Active:"Các chiến dịch đang tuyển KOC hoặc đang thực hiện.",Completed:"Các chiến dịch đã hoàn thành.",Attention:"Các chiến dịch cần cập nhật, bị từ chối hoặc đã hủy."};
        $("#adminCampaignScopeText").text(descriptions[scope]||"Tất cả chiến dịch trong hệ thống."); reload();
    };
    $("[data-campaign-scope]").on("click",function(){
        applyScope(this.dataset.campaignScope);
    });
    $("[data-campaign-card-scope]").on("click keydown",function(event){
        if(event.type==="keydown" && !["Enter"," "].includes(event.key)) return;
        event.preventDefault(); applyScope(this.dataset.campaignCardScope);
    });
    $(document).on("click","[data-campaign-delete]",function(){
        $("#campaignDeleteId").val(decodeURIComponent(this.dataset.id||""));
        $("#campaignDeleteName").text(this.dataset.name||"chiến dịch này");
        $("#campaignDeleteReason").val("");
        bootstrap.Modal.getOrCreateInstance(document.getElementById("campaignDeleteModal")).show();
    });
})(jQuery);

