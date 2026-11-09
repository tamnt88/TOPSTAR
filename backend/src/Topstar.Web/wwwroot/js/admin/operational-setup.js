document.addEventListener('DOMContentLoaded',()=>{
 const buttons=[...document.querySelectorAll('[data-ops-tab]')],panels=[...document.querySelectorAll('[data-ops-panel]')];
 const activate=key=>{buttons.forEach(x=>x.classList.toggle('is-active',x.dataset.opsTab===key));panels.forEach(x=>{const active=x.dataset.opsPanel===key;x.hidden=!active;x.classList.toggle('is-active',active);});history.replaceState(null,'',`#${key}`);};
 buttons.forEach(x=>x.addEventListener('click',()=>activate(x.dataset.opsTab)));
 const requested=location.hash.slice(1);if(buttons.some(x=>x.dataset.opsTab===requested))activate(requested);
});
