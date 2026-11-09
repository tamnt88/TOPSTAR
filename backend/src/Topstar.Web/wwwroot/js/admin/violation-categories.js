document.addEventListener('DOMContentLoaded',()=>{
 const input=document.querySelector('.js-violation-form [name="category"]'),node=document.getElementById('violationCategoryData');
 if(!input||!node)return;const items=JSON.parse(node.textContent||'[]'),select=document.createElement('select');
 select.name='category';select.className='form-select';select.required=true;
 select.innerHTML='<option value="">Chọn loại vi phạm</option>'+items.map(x=>`<option value="${x.Code}" data-severity="${x.DefaultSeverity||''}" data-points="${x.DefaultPenaltyPoints??0}">${x.Name}</option>`).join('');
 input.replaceWith(select);select.addEventListener('change',()=>{const option=select.selectedOptions[0],form=select.form;if(option?.dataset.severity)form.querySelector('[name="severity"]').value=option.dataset.severity;if(option?.dataset.points)form.querySelector('[name="penaltyPoints"]').value=option.dataset.points;});
});
