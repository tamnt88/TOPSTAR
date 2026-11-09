(function($){'use strict';const element=document.getElementById('brandFundingList');if(!element)return;
$.fn.dataTable.ext.search.push(function(settings,data,index){if(settings.nTable!==element)return true;const status=$('#fundingStatus').val();return !status||settings.aoData[index].nTr.dataset.campaignStatus===status;});
const table=$(element).DataTable(TopstarLists.adapt($(element),{pageLength:20,autoWidth:false,lengthChange:false,order:[[1,'asc']],columnDefs:[{targets:[0,6],orderable:false}],dom:"t<'funding-list-footer'ip>",language:{emptyTable:'Chưa có chiến dịch.',zeroRecords:'Không tìm thấy chiến dịch phù hợp.',info:'Hiển thị _START_–_END_ / _TOTAL_ kết quả',infoEmpty:'Hiển thị 0 / 0 kết quả',infoFiltered:''}}));
const apply=()=>table.search($('#fundingKeyword').val().trim()).page.len(Number($('#fundingPageLength').val())||20).draw();$('#fundingListFilters').on('submit',e=>{e.preventDefault();apply();});$('#fundingStatus,#fundingPageLength').on('change',apply);
if($('#fundingKeyword').val()||$('#fundingStatus').val())apply();
})(jQuery);
