(function ($) {
  'use strict';
  $(function () {
    var $table = $('#packagePolicyDataTable');
    if (!$table.length || !$.fn.DataTable) return;
    var esc = function (value) { return $('<div>').text(value == null ? '' : String(value)).html(); };
    var money = function (value) { return Number(value || 0).toLocaleString('vi-VN') + ' ₫'; };
    var table = $table.DataTable(TopstarLists.adapt($table, {
      ajax: { url: $table.data('ajax-url'), dataSrc: 'data', error: function () { window.AdminToast.show('Không thể tải danh sách gói KOC.', 'error'); } },
      processing: true, searching: true, ordering: true, lengthChange: false, pageLength: 20, autoWidth: false,
      order: [[1, 'asc']],
      columnDefs: [
        { targets: 0, width: '52px' },
        { targets: 1, width: '230px' },
        { targets: 2, width: '145px' },
        { targets: 4, width: '180px' },
        { targets: 5, width: '150px' },
        { targets: 6, width: '90px' }
      ],
      columns: [
        { data: null, width: '55px', orderable: false, render: function (_d,_t,_r,m) { return m.row + m.settings._iDisplayStart + 1; } },
        { data: null, render: function (_d,_t,r) { var image=r.imageUrl?'<img class="package-table-image" src="'+esc(r.imageUrl)+'" alt="">':'<span class="package-table-image is-empty"><i class="fa-regular fa-image"></i></span>';return '<div class="package-table-identity">'+image+'<div><strong class="d-block">'+esc(r.name)+(r.isFeatured?' <i class="fa-solid fa-star text-warning" title="Gói nổi bật"></i>':'')+'</strong></div></div>'; } },
        { data: 'referenceBudget', render: function (v) { return Number(v)>0?'<strong class="package-sale-price">'+money(v)+'</strong>':'<span class="badge text-bg-warning">Chưa cấu hình ngân sách</span>'; } },
        { data: null, render: function (_d,_t,r) { return '<span class="package-table-description">'+esc(r.description)+'</span>'; } },
        { data: null, render: function (_d,_t,r) { var quota=r.selectionQuota==null?'Chưa cấu hình':Number(r.selectionQuota).toLocaleString('vi-VN')+' KOC tối đa'; return '<span class="badge '+(r.brandSelectionEnabled?'text-bg-primary':'text-bg-secondary')+'">'+(r.brandSelectionEnabled?'Brand tự chọn':'Topstar phân công')+'</span><small class="d-block mt-1 text-muted">'+quota+'</small>'; } },
        { data: 'isActive', render: function (v) { return '<span class="account-status '+(v?'account-status-active':'account-status-danger')+'">'+(v?'Đang áp dụng':'Tạm dừng')+'</span>'; } },
        { data: null, orderable: false, render: function (_d,_t,r) { return '<div class="dropdown account-action-dropdown"><button class="btn btn-sm btn-outline-secondary account-action-trigger" data-bs-toggle="dropdown" data-bs-display="static" aria-label="Mở menu thao tác"><i class="fa-solid fa-ellipsis-vertical"></i></button><ul class="dropdown-menu dropdown-menu-end"><li><a class="dropdown-item" href="/Admin/Settings/PackagePolicyEdit/'+encodeURIComponent(r.id)+'"><i class="fa-solid fa-pen"></i><span>Sửa gói</span></a></li><li><hr class="dropdown-divider"></li><li><button class="dropdown-item account-menu-danger js-package-delete" data-id="'+esc(r.id)+'" data-name="'+esc(r.name)+'"><i class="fa-solid fa-trash-can"></i><span>Xóa gói</span></button></li></ul></div>'; } }
      ],
      language: { processing:'Đang tải dữ liệu...',emptyTable:'Chưa có gói KOC.',zeroRecords:'Không tìm thấy gói phù hợp.',info:'Hiển thị _START_–_END_ trong _TOTAL_ gói',infoEmpty:'Không có dữ liệu',infoFiltered:'',paginate:{first:'Đầu',previous:'<i class="fa-solid fa-chevron-left"></i>',next:'<i class="fa-solid fa-chevron-right"></i>',last:'Cuối'} },
      layout: { topStart:null,topEnd:null,bottomStart:'info',bottomEnd:'paging' }
    }));
    function filter(){var status=$('#packageStatus').val();table.search($('#packageSearch').val()).column(5).search(status==='active'?'Đang áp dụng':(status==='inactive'?'Tạm dừng':'')).draw();}
    $('#packageApplyFilter').on('click',filter);
    $('#packageSearch').on('keydown',function(e){if(e.key==='Enter'){e.preventDefault();filter();}});
    $('#packageStatus').on('change',filter);
    $('#packagePageSize').on('change',function(){table.page.len(Number(this.value)||20).draw();});
    $('#packageResetFilter').on('click',function(){$('#packageSearch,#packageStatus').val('');$('#packagePageSize').val('20');table.search('').columns().search('');table.page.len(20).draw();});
    $(document).on('click','.js-package-delete',function(){$('#packageDeleteId').val($(this).data('id'));$('#packageDeleteReason').val('').removeClass('is-invalid');$('#packageDeleteDescription').text('Bạn đang xóa gói “'+$(this).data('name')+'”.');bootstrap.Modal.getOrCreateInstance(document.getElementById('packageDeleteModal')).show();});
    $('#packageDeleteForm').on('submit',function(e){e.preventDefault();var $reason=$('#packageDeleteReason'),reason=$.trim($reason.val());if(reason.length<10||reason.length>1000){$reason.addClass('is-invalid').focus();return;}$reason.removeClass('is-invalid');$.ajax({url:window.location.pathname+'?handler=Delete',method:'POST',data:{id:$('#packageDeleteId').val(),reason:reason,__RequestVerificationToken:$(this).find('input[name="__RequestVerificationToken"]').val()}}).done(function(r){bootstrap.Modal.getInstance(document.getElementById('packageDeleteModal')).hide();window.AdminToast.show(r.message,'success');table.ajax.reload(null,false);}).fail(function(xhr){var message=xhr.responseJSON&&xhr.responseJSON.message?xhr.responseJSON.message:'Không thể xóa gói KOC.';window.AdminToast.show(message,'error');});});
  });
})(jQuery);
