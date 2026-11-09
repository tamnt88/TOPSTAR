const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

const base = process.env.TOPSTAR_URL || 'http://localhost:5201';
const routes = ['/Koc/Dashboard','/Koc/Jobs','/Koc/Tasks','/Koc/Collaborations','/Koc/Bookings','/Koc/Shipments','/Koc/Profile','/Koc/Wallet','/Koc/Reviews','/Koc/Academy','/Koc/Notifications','/Koc/Disputes'];
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'topstar-koc-audit-'));
let chrome, socket, sequence = 0;
const pending = new Map();

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
async function main() {
  chrome = spawn(process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless=new','--no-first-run','--disable-background-networking','--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'
  ], { windowsHide: true });
  const endpoint = await new Promise((resolve, reject) => {
    chrome.on('error', reject);
    chrome.stderr.on('data', data => { const match = String(data).match(/DevTools listening on (ws:\/\/\S+)/); if (match) resolve(match[1]); });
  });
  const targets = await (await fetch(endpoint.replace(/^ws:/,'http:').replace(/\/devtools\/.*/, '/json/list'))).json();
  socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
  await new Promise(resolve => socket.onopen = resolve);
  socket.onmessage = event => { const message = JSON.parse(event.data); const task = pending.get(message.id); if (!task) return; pending.delete(message.id); message.error ? task.reject(message.error) : task.resolve(message.result); };
  const send = (method, params = {}) => new Promise((resolve, reject) => { const id = ++sequence; pending.set(id, { resolve, reject }); socket.send(JSON.stringify({ id, method, params })); });
  const evaluate = async expression => { const result = await send('Runtime.evaluate', { expression, returnByValue:true, awaitPromise:true }); if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails)); return result.result.value; };
  const navigate = async url => { await send('Page.navigate', { url }); await sleep(700); };
  await send('Page.enable'); await send('Runtime.enable');
  await navigate(`${base}/dang-nhap`);
  await evaluate(`document.querySelector('[name="Input.EmailOrPhone"]').value='tamnt.mcc@gmail.com';document.querySelector('[name="Input.Password"]').value='123456';document.querySelector('form').requestSubmit();void 0`);
  for (let i=0;i<50 && !String(await evaluate('location.pathname')).startsWith('/Koc/');i++) await sleep(100);
  const report = [];
  for (const viewport of [{name:'desktop',width:1440,height:900,mobile:false},{name:'mobile',width:390,height:844,mobile:true}]) {
    await send('Emulation.setDeviceMetricsOverride',{width:viewport.width,height:viewport.height,deviceScaleFactor:1,mobile:viewport.mobile});
    for (const route of routes) {
      await navigate(base + route);
      report.push(await evaluate(`(() => {const text=document.body.innerText;const bad=text.match(/.{0,30}(?:Ã.|Ä.|Æ.|áº|â€).{0,50}/g)||[];const shown=x=>{const r=x.getBoundingClientRect(),s=getComputedStyle(x);return r.width>0&&r.height>0&&s.visibility!=='hidden'};const actions=[...document.querySelectorAll('main a,main button')].filter(shown);const small='${viewport.name}'==='mobile'?actions.filter(x=>{const r=x.getBoundingClientRect();return r.width<36||r.height<36}):[];return {viewport:'${viewport.name}',route:'${route}',actual:location.pathname,title:document.title,h1:[...document.querySelectorAll('main h1')].map(x=>x.textContent.trim()),overflow:Math.max(0,Math.max(document.documentElement.scrollWidth,document.body.scrollWidth)-innerWidth),duplicateIds:[...document.querySelectorAll('[id]')].map(x=>x.id).filter((x,i,a)=>a.indexOf(x)!==i),brokenImages:[...document.images].filter(x=>x.complete&&x.naturalWidth===0).map(x=>x.src),mojibake:bad.slice(0,5),unnamedActions:actions.filter(x=>!(x.innerText.trim()||x.getAttribute('aria-label')||x.title)).length,smallTapTargets:small.map(x=>({text:(x.innerText||x.getAttribute('aria-label')||x.title).trim().slice(0,45),class:x.className})),visibleActions:actions.length}})()`));
    }
  }
  await send('Emulation.setDeviceMetricsOverride',{width:1440,height:900,deviceScaleFactor:1,mobile:false});
  await navigate(`${base}/Koc/Jobs?section=applications`);
  const detailHref = await evaluate(`document.querySelector('.koc-application-actions a')?.href || ''`);
  if (detailHref) { await navigate(detailHref); await sleep(500); }
  const detailInteraction = await evaluate(`({href:${JSON.stringify(detailHref)},actual:location.pathname,title:document.title,h1:document.querySelector('main h1')?.textContent?.trim()||'',serverError:/Internal Server Error|Unhandled exception|Đã xảy ra lỗi/i.test(document.body.innerText)})`);
  const failures = report.filter(x => x.actual !== x.route || x.overflow > 1 || x.duplicateIds.length || x.brokenImages.length || x.mojibake.length || x.unnamedActions || x.smallTapTargets.length || !x.h1.length);
  if (!detailHref || detailInteraction.actual.toLowerCase() !== '/koc/campaigndetail' || detailInteraction.serverError || !detailInteraction.h1) failures.push({interaction:'applications-detail',...detailInteraction});
  console.log(JSON.stringify({checked:report.length+1,failures,detailInteraction,report},null,2));
  process.exitCode = failures.length ? 1 : 0;
}
main().catch(error => { console.error(error); process.exitCode=1; }).finally(() => { try { socket?.close(); chrome?.kill(); fs.rmSync(profile,{recursive:true,force:true}); } catch {} });
