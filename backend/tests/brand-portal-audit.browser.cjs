const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

const base = process.env.TOPSTAR_URL || 'http://localhost:5201';
const routes = [
  '/Brand/Dashboard','/Brand/Profile','/Brand/EditProfile','/Brand/Packages',
  '/Brand/Billing','/Brand/Campaigns','/Brand/CampaignWorkspace','/Brand/Shortlist',
  '/Brand/CampaignFunding','/Brand/Bookings','/Brand/Shipments','/Brand/Deliverables',
  '/Brand/Reports','/Brand/Reviews','/Brand/Collaborations','/Brand/Notifications',
  '/Brand/Disputes'
];
if (process.env.TOPSTAR_BRAND_CAMPAIGN_ID) routes.push(`/Brand/CampaignWorkspace?id=${process.env.TOPSTAR_BRAND_CAMPAIGN_ID}`);
routes.push('/Brand/Reports?campaignId=b1000005-2026-4000-8000-000000000005');
if(process.env.TOPSTAR_NATIVE_ONLY) routes.splice(0,routes.length,`/Brand/CampaignWorkspace?id=${process.env.TOPSTAR_BRAND_CAMPAIGN_ID}`);
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'topstar-brand-audit-'));
let chrome, socket, sequence = 0;
const pending = new Map();
const runtimeErrors = [];
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function main() {
  chrome = spawn(process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless=new','--no-first-run','--disable-background-networking','--remote-debugging-port=0',
    `--user-data-dir=${profile}`,'about:blank'
  ], { windowsHide: true });
  const endpoint = await new Promise((resolve, reject) => {
    chrome.on('error', reject);
    chrome.stderr.on('data', data => {
      const match = String(data).match(/DevTools listening on (ws:\/\/\S+)/);
      if (match) resolve(match[1]);
    });
  });
  const targets = await (await fetch(endpoint.replace(/^ws:/,'http:').replace(/\/devtools\/.*/, '/json/list'))).json();
  socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
  await new Promise(resolve => socket.onopen = resolve);
  socket.onmessage = event => {
    const message = JSON.parse(event.data);
    if (message.method === 'Runtime.exceptionThrown') runtimeErrors.push(message.params.exceptionDetails?.text || 'Runtime exception');
    const task = pending.get(message.id);
    if (!task) return;
    pending.delete(message.id);
    message.error ? task.reject(message.error) : task.resolve(message.result);
  };
  const send = (method, params = {}) => new Promise((resolve, reject) => {
    const id = ++sequence;
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
  });
  const evaluate = async expression => {
    const result = await send('Runtime.evaluate', { expression, returnByValue:true, awaitPromise:true });
    if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
  };
  const navigate = async url => {
    await send('Page.navigate', { url });
    await sleep(900);
  };
  await send('Page.enable');
  await send('Runtime.enable');
  await navigate(`${base}/dang-nhap`);
  await evaluate(`document.querySelector('[name="Input.EmailOrPhone"]').value='brand@topstar.local';document.querySelector('[name="Input.Password"]').value='Topstar@123';document.querySelector('form').requestSubmit();void 0`);
  for (let i=0;i<60 && !String(await evaluate('location.pathname')).startsWith('/Brand/');i++) await sleep(100);
  if (!String(await evaluate('location.pathname')).startsWith('/Brand/')) throw new Error(`Brand login failed: ${await evaluate('location.href')}`);

  const report = [];
  for (const viewport of [{name:'desktop',width:1440,height:900,mobile:false},{name:'mobile',width:390,height:844,mobile:true}]) {
    await send('Emulation.setDeviceMetricsOverride',{width:viewport.width,height:viewport.height,deviceScaleFactor:1,mobile:viewport.mobile});
    for (const route of routes) {
      const errorStart = runtimeErrors.length;
      await navigate(base + route);
      report.push(await evaluate(`(() => {
        const text=document.body.innerText;
        const shown=x=>{const r=x.getBoundingClientRect(),s=getComputedStyle(x);return r.width>0&&r.height>0&&s.visibility!=='hidden'};
        const actions=[...document.querySelectorAll('main a,main button,main input[type="submit"]')].filter(shown);
        const small='${viewport.name}'==='mobile'?actions.filter(x=>{const r=x.getBoundingClientRect();return r.width<36||r.height<36}):[];
        const bodyText=text.replace(/\s+/g,' ').trim();
        return {viewport:'${viewport.name}',route:'${route}',actual:location.pathname,title:document.title,
          h1:[...document.querySelectorAll('main h1')].map(x=>x.textContent.trim()),
          overflow:Math.max(0,Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)-innerWidth),
          duplicateIds:[...document.querySelectorAll('[id]:not(input[type="hidden"])')].map(x=>x.id).filter(Boolean).filter((x,i,a)=>a.indexOf(x)!==i),
          brokenImages:[...document.images].filter(x=>x.complete&&x.naturalWidth===0).map(x=>x.src),
          mojibake:(bodyText.match(/.{0,30}(?:Ãƒ.|Ã„.|Ã†.|Ã¡Âº|Ã¢â‚¬).{0,50}/g)||[]).slice(0,5),
          serverError:/Unhandled exception|An error occurred while processing your request|Internal Server Error/i.test(bodyText),
          unnamedActions:actions.filter(x=>!(x.innerText.trim()||x.value||x.getAttribute('aria-label')||x.title)).length,
          smallTapTargets:small.map(x=>({text:(x.innerText||x.value||x.getAttribute('aria-label')||x.title).trim().slice(0,45),class:x.className})).slice(0,20),
          mobileNav:'${viewport.name}'==='mobile'?(()=>{const n=document.querySelector('.brand-mobile-bottom-nav');const r=n?.getBoundingClientRect();return {visible:!!n&&shown(n),items:n?.children.length||0,height:r?.height||0,bottomGap:parseFloat(getComputedStyle(document.body).paddingBottom)||0}})():null,
          missingIcons:[...document.querySelectorAll('i.fa-solid,i.fa-regular,i.fa-brands')].filter(x=>{const c=getComputedStyle(x,'::before').content;return !c||c==='none'||c==='normal'||c==='""'}).map(x=>x.className),
          reportScope:location.search.includes('campaignId=')&&location.pathname.toLowerCase()==='/brand/reports'?document.querySelector('.brand-report-kpis article strong')?.textContent.trim():null,
          visibleActions:actions.length,bodySample:bodyText.slice(0,180)};
      })()`));
      report.at(-1).runtimeErrors = runtimeErrors.slice(errorStart);
      report.at(-1).headerAlignment = await evaluate(`(() => {const b=document.querySelector('.brand-notification-bell')?.getBoundingClientRect(),a=document.querySelector('.brand-account-toggle')?.getBoundingClientRect();return b&&a?Math.abs(b.top+b.height/2-a.top-a.height/2):999})()`);
      if (route.startsWith('/Brand/CampaignWorkspace?id=')) {
        report.at(-1).nativeTabs = [];
        for (const [tab,subtab] of [['koc',''],['finance',''],['execution','bookings'],['execution','shipments'],['execution','deliverables'],['execution','reports'],['support','disputes'],['support','reviews'],['support','history'],['support','profile'],['koc','']]) {
          await evaluate(`document.querySelector('[data-workspace-tab="${tab}"]').click();void 0`);
          if (subtab) await evaluate(`document.querySelector('[data-workspace-panel="${tab}"] [data-workspace-subtab="${subtab}"]').click();void 0`);
          for (let i=0;i<80;i++) {
            await sleep(100);
            if (await evaluate(`!document.querySelector('[data-workspace-panel="${tab}"] .campaign-workspace-native[aria-busy="true"]')`)) break;
          }
          await sleep(150);
          report.at(-1).nativeTabs.push(await evaluate(`(() => {
            const host=document.querySelector('[data-workspace-panel="${tab}"] .campaign-workspace-native');
            const shown=e=>!!e&&e.getBoundingClientRect().height>0&&getComputedStyle(e).display!=='none';
            return {tab:'${tab}',subtab:'${subtab}',iframe:document.querySelectorAll('iframe').length,
              shell:host.querySelectorAll('header.dashboard-header,.portal-footer').length,
              loading:host.hasAttribute('aria-busy'),error:!!host.querySelector(':scope > .alert-danger'),
              fundingBlocked:host.querySelector('.brand-funding-blocked') ? {formCount:host.querySelectorAll('.js-funding-form,.brand-funding-confirm').length,cta:host.querySelector('.brand-funding-blocked a')?.href}:null,
              duplicateIds:[...document.querySelectorAll('[id]:not(input[type="hidden"])')].map(x=>x.id).filter(Boolean).filter((id,i,a)=>a.indexOf(id)!==i),
              missingIcons:[...host.querySelectorAll('i.fa-solid,i.fa-regular,i.fa-brands')].filter(i=>{const c=getComputedStyle(i,'::before').content;return !c||c==='none'||c==='normal'||c==='""'}).map(i=>i.className),
              overflow:Math.max(0,document.documentElement.scrollWidth-innerWidth),
              headings:[...host.querySelectorAll('h1')].filter(shown).map(h=>h.textContent),
              forms:[...host.querySelectorAll('form')].map(f=>f.action),sample:host.innerText.slice(0,120)};
          })()`));
        }
        await evaluate(`document.querySelector('[data-workspace-tab="overview"]').click();void 0`);
        await sleep(400);
        report.at(-1).executionCopy = await evaluate(`(() => {const f=document.querySelector('.campaign-workspace-flow'),s=f.querySelectorAll(':scope > div');return {label:s[2].querySelector('small').innerText,prerequisitesComplete:s[0].classList.contains('is-done')&&s[1].classList.contains('is-done'),genericAction:document.querySelector('.campaign-workspace-next button').innerText.includes('Thực hiện ngay')}})()`);
        // Never submit an eligible real campaign: verify the blocked state first.
        report.at(-1).fundingGuard=await evaluate(`(async()=>{const page=await fetch('/Brand/CampaignFunding?campaignId=${process.env.TOPSTAR_BRAND_CAMPAIGN_ID}',{headers:{'X-Topstar-Workspace':'native'}});const before=new DOMParser().parseFromString(await page.text(),'text/html');if(!before.querySelector('.brand-funding-blocked'))return {skipped:true};document.querySelector('[data-workspace-tab="koc"]').click();for(let i=0;i<80&&document.querySelector('.campaign-workspace-native[aria-busy="true"]');i++)await new Promise(r=>setTimeout(r,100));const token=document.querySelector('.campaign-workspace-native [name="__RequestVerificationToken"]').value;const body=new FormData();body.set('__RequestVerificationToken',token);body.set('campaignId','${process.env.TOPSTAR_BRAND_CAMPAIGN_ID}');body.set('allocationMode','Equal');const response=await fetch('/Brand/CampaignFunding?handler=Save',{method:'POST',headers:{'X-Topstar-Workspace':'native'},body});const doc=new DOMParser().parseFromString(await response.text(),'text/html');return {status:response.status,rejected:!!doc.querySelector('.alert-danger'),blocked:!!doc.querySelector('.brand-funding-blocked'),editable:!!doc.querySelector('.js-funding-form')};})()`);
        // GET filters stay in the native tab and retain the selected campaign.
        await evaluate(`document.querySelector('[data-workspace-tab="support"]').click();void 0`);
        for(let i=0;i<80;i++){await sleep(100);if(await evaluate(`!document.querySelector('.campaign-workspace-native[aria-busy="true"]')`))break;}
        await evaluate(`document.querySelector('[data-workspace-panel="support"] .list-window-filters').requestSubmit();void 0`);
        for(let i=0;i<80;i++){await sleep(100);if(await evaluate(`!document.querySelector('.campaign-workspace-native[aria-busy="true"]')`))break;}
        report.at(-1).nativeFilter=await evaluate(`({staysInWorkspace:location.pathname.toLowerCase()==='/brand/campaignworkspace',scope:new URL(window.topstarWorkspaceUrl).searchParams.get('campaignId')})`);
        await evaluate(`document.querySelector('[data-workspace-tab="koc"]').click();void 0`);
        for(let i=0;i<80;i++){await sleep(100);if(await evaluate(`!document.querySelector('.campaign-workspace-native[aria-busy="true"]')`))break;}
        // Negative POST targets Guid.Empty, which cannot match a real application.
        await evaluate(`(() => {const f=document.querySelector('.js-reject-proposed-koc-form');f.querySelector('[name="applicationId"]').value='00000000-0000-0000-0000-000000000000';f.querySelector('[name="rejectionReason"]').value='Kiểm tra phản hồi cho hồ sơ không tồn tại';f.requestSubmit();})()`);
        for(let i=0;i<80;i++){await sleep(100);if(await evaluate(`!document.querySelector('.campaign-workspace-native[aria-busy="true"]')`))break;}
        report.at(-1).negativePost = await evaluate(`({staysInWorkspace:location.pathname.toLowerCase()==='/brand/campaignworkspace',feedback:document.querySelector('[data-workspace-panel="koc"] .brand-shortlist-message')?.innerText,scope:new URL(window.topstarWorkspaceUrl).searchParams.get('id')})`);
        await evaluate(`bootstrap.Modal.getOrCreateInstance(document.getElementById('rejectProposedKocModal')).show();void 0`);
        await sleep(350);
        report.at(-1).nativeModal = await evaluate(`(() => {const m=document.querySelector('#rejectProposedKocModal .modal-dialog'),r=m.getBoundingClientRect();return {visible:document.getElementById('rejectProposedKocModal').classList.contains('show'),inBounds:r.left>=0&&r.right<=innerWidth,backdrops:document.querySelectorAll('.modal-backdrop').length}})()`);
        await evaluate(`bootstrap.Modal.getInstance(document.getElementById('rejectProposedKocModal')).hide();void 0`);
        await sleep(350);
        report.at(-1).fundingUi=await evaluate(`(async()=>{
          const fixture=document.createElement('div');document.body.append(fixture);
          fixture.innerHTML='<form action="javascript:void(0)" class="js-funding-form" data-budget="101"><input type="radio" name="allocationMode" value="Equal" checked><input type="radio" name="allocationMode" value="Custom"><div class="brand-money-input"><input data-equal-value="51" data-custom-value=""></div><div class="brand-money-input"><input data-equal-value="50" data-custom-value=""></div><span data-allocated></span><span data-remaining></span><div class="brand-funding-error"></div><button type="button" data-funding-action="equal">Chia đều</button><button type="button" data-funding-action="remaining">Phân bổ số dư</button><button type="button" data-funding-action="clear">Xóa</button><button type="submit">Lưu</button></form>';
          const script=document.createElement('script');script.src='/js/brand/brand-campaign-funding.js';await new Promise((resolve,reject)=>{script.onload=resolve;script.onerror=reject;document.body.append(script);});
          const form=fixture.querySelector('form'),save=form.querySelector('[type="submit"]'),inputs=[...form.querySelectorAll('.brand-money-input input')];
          const result={equalValid:!save.disabled&&inputs.map(i=>Number(i.value)).reduce((a,b)=>a+b,0)===101};
          const mode=form.querySelector('[value="Custom"]');mode.checked=true;mode.dispatchEvent(new Event('change'));
          result.emptyBlocked=save.disabled;
          const set=(i,value)=>{inputs[i].value=value;inputs[i].dispatchEvent(new Event('input'));};
          set(0,'51');set(1,'50');result.customValid=!save.disabled;
          set(1,'51');result.overBudgetBlocked=save.disabled&&inputs[1].value==='51';
          set(1,'0');result.zeroBlocked=save.disabled;
          set(1,'-1');result.negativeBlocked=save.disabled&&inputs[1].value==='-1';
          form.querySelector('[data-funding-action="equal"]').click();result.equalModePreserved=form.querySelector('[value="Equal"]').checked&&!save.disabled;
          form.querySelector('[data-funding-action="clear"]').click();result.clearBlocked=save.disabled;
          set(0,'51');form.querySelector('[data-funding-action="remaining"]').click();result.remainingValid=!save.disabled&&inputs[1].value==='50';
          fixture.remove();script.remove();return result;
        })()`);
        report.at(-1).runtimeErrors = runtimeErrors.slice(errorStart);
      }
      if (route === '/Brand/Campaigns') {
        await evaluate(`document.querySelector('.brand-row-actions > button')?.click();void 0`);
        await sleep(200);
        report.at(-1).actionMenu = await evaluate(`(() => {const m=document.querySelector('.brand-row-actions .dropdown-menu.show'),r=m?.getBoundingClientRect();return {visible:!!m,inBounds:!!r&&r.left>=0&&r.right<=innerWidth}})()`);
        await evaluate(`document.querySelector('.brand-row-actions > button')?.click();void 0`);
      }
    }
  }
  const failures = report.filter(x => (!x.actual.startsWith('/Brand/') && !(x.route === '/Brand/CampaignWorkspace' && x.actual.toLowerCase() === '/brand/campaigns')) || x.overflow > 1 || x.duplicateIds.length || x.brokenImages.length || x.mojibake.length || x.serverError || x.unnamedActions || x.smallTapTargets.length || !x.h1.length || x.runtimeErrors.length || (x.viewport==='mobile'&&(!x.mobileNav?.visible||x.mobileNav.items!==4||x.mobileNav.bottomGap<x.mobileNav.height)));
  report.filter(x=>x.missingIcons.length||x.headerAlignment>2||(x.actionMenu&&(!x.actionMenu.visible||!x.actionMenu.inBounds))).forEach(x=>failures.push({route:x.route,viewport:x.viewport,missingIcons:x.missingIcons,headerAlignment:x.headerAlignment,actionMenu:x.actionMenu}));
  report.filter(x=>x.reportScope&&Number(x.reportScope.split('/')[1])>1).forEach(x=>failures.push({route:x.route,reportScope:x.reportScope}));
  report.filter(x=>x.nativeTabs?.some(t=>t.iframe||t.shell||t.loading||t.error||t.overflow>1||t.duplicateIds.length||t.missingIcons.length)).forEach(x=>failures.push({route:x.route,viewport:x.viewport,nativeTabs:x.nativeTabs}));
  report.filter(x=>x.negativePost&&(!x.negativePost.staysInWorkspace||!x.negativePost.feedback?.includes('Không tìm thấy')||x.negativePost.scope!==process.env.TOPSTAR_BRAND_CAMPAIGN_ID||!x.nativeModal?.visible||!x.nativeModal.inBounds||x.nativeModal.backdrops!==1)).forEach(x=>failures.push({viewport:x.viewport,negativePost:x.negativePost,nativeModal:x.nativeModal}));
  report.filter(x=>x.nativeFilter&&(!x.nativeFilter.staysInWorkspace||x.nativeFilter.scope!==process.env.TOPSTAR_BRAND_CAMPAIGN_ID)).forEach(x=>failures.push({viewport:x.viewport,nativeFilter:x.nativeFilter}));
  report.filter(x=>x.executionCopy&&(x.executionCopy.genericAction||(x.executionCopy.label==='Đang triển khai'&&!x.executionCopy.prerequisitesComplete))).forEach(x=>failures.push({viewport:x.viewport,executionCopy:x.executionCopy}));
  report.filter(x=>x.nativeTabs?.some(t=>t.fundingBlocked?.formCount>0)||x.fundingGuard&&(!x.fundingGuard.rejected||!x.fundingGuard.blocked||x.fundingGuard.editable)).forEach(x=>failures.push({viewport:x.viewport,fundingGuard:x.fundingGuard}));
  report.filter(x=>x.fundingUi&&Object.values(x.fundingUi).some(value=>!value)).forEach(x=>failures.push({viewport:x.viewport,fundingUi:x.fundingUi}));
  console.log(JSON.stringify({checked:report.length,failures,report},null,2));
  process.exitCode = failures.length ? 1 : 0;
}
main().catch(error => { console.error(error); process.exitCode=1; }).finally(() => {
  try { socket?.close(); chrome?.kill(); fs.rmSync(profile,{recursive:true,force:true}); } catch {}
});
