(function () {
    'use strict';
    const tabs = [...document.querySelectorAll('[data-workspace-tab]')];
    const panels = [...document.querySelectorAll('[data-workspace-panel]')];
    if (!tabs.length) return;
    let sequence = 0, currentHost;
    const libraries = new Set([...document.scripts].filter(s => s.src).map(s => s.src));
    const workspaceUrl = location.origin + location.pathname + location.search;
    tabs.forEach(tab => {
        const name=tab.dataset.workspaceTab;
        tab.id='workspace-tab-'+name; tab.setAttribute('role','tab'); tab.setAttribute('aria-controls','workspace-panel-'+name);
        const panel=panels.find(p=>p.dataset.workspacePanel===name);
        if(panel) { panel.id='workspace-panel-'+name; panel.setAttribute('role','tabpanel'); panel.setAttribute('aria-labelledby',tab.id); }
    });

    async function refreshOverview(request) {
        try {
            const response=await fetch(workspaceUrl,{headers:{'X-Topstar-Workspace':'native'},credentials:'same-origin'});
            if (!response.ok || request!==sequence) return;
            const doc=new DOMParser().parseFromString(await response.text(),'text/html');
            if (request!==sequence) return;
            for (const selector of ['.campaign-workspace-hero','.campaign-workspace-status','.campaign-workspace-flow','[data-workspace-panel="overview"]']) {
                const source=doc.querySelector(selector),target=document.querySelector(selector);
                if(source&&target) { target.innerHTML=source.innerHTML; if(!selector.includes('panel'))target.className=source.className; }
            }
            for (const name of ['koc','support']) {
                const source=doc.querySelector('[data-workspace-tab="'+name+'"] span'),target=document.querySelector('[data-workspace-tab="'+name+'"] span');
                if(source&&target) target.textContent=source.textContent;
            }
        } catch (_) { /* Tab feedback remains visible; the overview refresh can be retried by opening it. */ }
    }

    function cleanup() {
        document.dispatchEvent(new CustomEvent('topstar:workspace-dispose'));
        document.querySelectorAll('.campaign-workspace-native .modal').forEach(m => {
            const instance = bootstrap.Modal.getInstance(m);
            if (instance && m.classList.contains('show')) {
                m.addEventListener('hidden.bs.modal',()=>instance.dispose(),{once:true});
                instance.hide();
            } else if (instance) instance.dispose();
        });
        if (window.jQuery?.fn.dataTable) document.querySelectorAll('.campaign-workspace-native table').forEach(t => {
            if (jQuery.fn.dataTable.isDataTable(t)) jQuery(t).DataTable().destroy();
        });
        document.querySelectorAll('.campaign-workspace-native').forEach(h => h.replaceChildren());
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
        document.body.classList.remove('modal-open');
        ['padding-right','overflow'].forEach(p => document.body.style.removeProperty(p));
    }

    async function scripts(nodes, request) {
        for (const node of nodes) {
            if (request !== sequence) return;
            if (node.type && !['text/javascript','application/javascript','module'].includes(node.type)) continue;
            const src = node.src, library = src && /\/lib\//.test(src);
            if (library && libraries.has(src)) continue;
            await new Promise((resolve, reject) => {
                const script = document.createElement('script');
                if (src) {
                    script.src = src; script.async = false;
                    script.onload = () => { if (library) libraries.add(src); script.remove(); resolve(); };
                    script.onerror = () => { script.remove(); reject(new Error('Không tải được chức năng.')); };
                } else script.textContent = node.textContent;
                document.body.appendChild(script);
                if (!src) { script.remove(); resolve(); }
            });
        }
        document.dispatchEvent(new CustomEvent('topstar:workspace-ready'));
    }

    async function load(host, url, options) {
        const request = ++sequence;
        cleanup(); currentHost = host;
        host.dataset.url = url;
        host.setAttribute('aria-busy','true');
        host.innerHTML = '<div class="workspace-loading" role="status"><span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang tải thông tin chiến dịch…</div>';
        try {
            const response = await fetch(url, Object.assign({}, options, {
                headers: { 'X-Topstar-Workspace':'native' }, credentials:'same-origin'
            }));
            if (request !== sequence) return;
            if (response.redirected && !new URL(response.url).pathname.toLowerCase().startsWith('/brand/')) { location.assign(response.url); return; }
            if (!response.ok) throw new Error(response.status === 403 ? 'Bạn không có quyền thực hiện thao tác này.' : 'Không tải được thông tin. Vui lòng thử lại.');
            const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
            if (request !== sequence) return;
            const scriptNodes = [...doc.querySelectorAll('[data-workspace-scripts] script')];
            doc.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
                if (![...document.querySelectorAll('link[rel="stylesheet"]')].some(l => l.href === link.href)) document.head.append(document.importNode(link,true));
            });
            doc.querySelectorAll('[data-workspace-assets],[data-workspace-scripts]').forEach(n=>n.remove());
            // Only page-owned initializer scripts run; JSON data stays in the native content.
            doc.querySelectorAll('script:not([type="application/json"])').forEach(n=>n.remove());
            host.replaceChildren(...[...doc.body.childNodes].map(n => document.importNode(n,true)));
            host.dataset.url = response.url;
            window.topstarWorkspaceUrl = response.url;
            host.querySelectorAll('form').forEach(form => {
                const action = form.getAttribute('action');
                const target = action ? new URL(action,response.url) : new URL(response.url);
                const source = new URL(response.url);
                if (target.pathname.toLowerCase() === source.pathname.toLowerCase()) {
                    for (const key of ['id','campaignId']) {
                        if (source.searchParams.has(key) && !target.searchParams.has(key)) target.searchParams.set(key,source.searchParams.get(key));
                    }
                }
                form.action = target.href;
            });
            await scripts(scriptNodes, request);
            if (options?.method === 'POST') {
                await refreshOverview(request);
                host.querySelector('.alert,[role="alert"]')?.scrollIntoView({behavior:'smooth',block:'center'});
            }
        } catch (error) {
            if (request !== sequence) return;
            host.replaceChildren();
            const message = document.createElement('div'); message.className='alert alert-danger'; message.setAttribute('role','alert');
            message.textContent=error instanceof TypeError?'Không kết nối được với hệ thống. Vui lòng thử lại.':error.message;
            const retry = document.createElement('button'); retry.type='button'; retry.className='btn btn-outline-secondary'; retry.textContent='Thử lại'; retry.onclick=()=>load(host,url);
            host.append(message,retry);
        } finally { if (request === sequence) host.removeAttribute('aria-busy'); }
    }

    function activate(name, subtab, updateHash) {
        const panel = panels.find(p => p.dataset.workspacePanel === name);
        if (!panel) return;
        tabs.forEach(t => { const active=t.dataset.workspaceTab===name; t.classList.toggle('active',active); t.setAttribute('aria-selected',String(active)); t.tabIndex=active?0:-1; });
        panels.forEach(p => p.classList.toggle('active',p===panel));
        const buttons = [...panel.querySelectorAll('[data-workspace-subtab]')];
        const button = buttons.find(b => b.dataset.workspaceSubtab===subtab) || buttons[0];
        buttons.forEach(b => b.classList.toggle('active',b===button));
        if (updateHash) history.replaceState(null,'','#tab-'+name+(button?'-'+button.dataset.workspaceSubtab:''));
        const host=panel.querySelector('.campaign-workspace-native');
        if (host) load(host,button?.dataset.contentSrc || host.dataset.src);
        else { ++sequence; cleanup(); currentHost=null; window.topstarWorkspaceUrl=null; refreshOverview(sequence); }
    }
    tabs.forEach(t => t.addEventListener('click',()=>activate(t.dataset.workspaceTab,null,true)));
    tabs.forEach((t,index)=>t.addEventListener('keydown',event => {
        const next=event.key==='Home'?0:event.key==='End'?tabs.length-1:event.key==='ArrowRight'?(index+1)%tabs.length:event.key==='ArrowLeft'?(index+tabs.length-1)%tabs.length:null;
        if(next===null)return; event.preventDefault(); tabs[next].focus(); activate(tabs[next].dataset.workspaceTab,null,true);
    }));
    document.addEventListener('click',event => {
        const b=event.target.closest('[data-open-workspace-tab]');
        if(b) activate(b.dataset.openWorkspaceTab,b.dataset.openWorkspaceSubtab,true);
    });
    document.querySelectorAll('[data-workspace-subtab]').forEach(b => b.addEventListener('click',()=>activate(b.closest('[data-workspace-panel]').dataset.workspacePanel,b.dataset.workspaceSubtab,true)));
    // Run after form and document validators, including delegated jQuery handlers.
    window.addEventListener('submit', async event => {
        const form=event.target;
        if (!form.closest('.campaign-workspace-native') || event.defaultPrevented) return;
        const action=new URL(event.submitter?.hasAttribute('formaction') ? event.submitter.formAction : form.action,location.href);
        if (action.origin !== location.origin || !action.pathname.toLowerCase().startsWith('/brand/')) return;
        // Payment gateway redirects require top-level navigation.
        const handler=action.searchParams.get('handler')?.toLowerCase();
        if (form.hasAttribute('data-native-navigation') || handler==='onepay' || handler==='vnpay') return;
        event.preventDefault();
        if (form.dataset.workspaceSubmitting) return;
        form.dataset.workspaceSubmitting='true';
        const body=new FormData(form);
        if (event.submitter?.name) body.append(event.submitter.name,event.submitter.value);
        if (form.method.toLowerCase()==='get') {
            for (const [key,value] of body) action.searchParams.set(key,value);
            await load(currentHost,action.href);
        } else await load(currentHost,action.href,{method:'POST',body});
    });
    document.addEventListener('click',event => {
        const anchor=event.target.closest('a[href]');
        if (!anchor?.closest('.campaign-workspace-native') || event.defaultPrevented || anchor.target || anchor.hasAttribute('download') || event.ctrlKey || event.metaKey) return;
        const url=new URL(anchor.href,location.href);
        if(url.origin===location.origin && url.pathname.toLowerCase()==='/brand/campaignworkspace' && url.searchParams.get('id')===new URL(workspaceUrl).searchParams.get('id') && url.hash.startsWith('#tab-')) {
            event.preventDefault(); const parts=url.hash.slice(5).split('-'); activate(parts.shift(),parts.join('-'),true); return;
        }
        if (anchor.getAttribute('aria-disabled') === 'true') { event.preventDefault(); return; }
        if (url.origin!==location.origin || !url.pathname.toLowerCase().startsWith('/brand/') || /\/(campaigns|campaignworkspace|billingreceipt)$/i.test(url.pathname)) return;
        event.preventDefault(); load(currentHost,url.href);
    });
    function fromHash() { const parts=(location.hash.startsWith('#tab-')?location.hash.slice(5):'overview').split('-'); activate(parts.shift(),parts.join('-'),false); }
    document.addEventListener('topstar:workspace-navigate',event => { if (currentHost) load(currentHost,event.detail); });
    window.addEventListener('hashchange',fromHash);
    fromHash();
}());
