// UI fixture only: real Select2, stylesheet and page script; no application database writes.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),os=require('node:os'),assert=require('node:assert/strict');
const {spawn}=require('node:child_process');
const root=path.resolve(__dirname,'../backend/src/Topstar.Web/wwwroot');
const profile=fs.mkdtempSync(path.join(os.tmpdir(),'topstar-needs-ui-'));
let server,chrome,socket,sequence=0;const pending=new Map();
const styles=['/lib/bootstrap/dist/css/bootstrap.min.css','/lib/fontawesome/css/all.min.css','/css/brand/brand.css','/lib/select2/select2.min.css','/css/shared/form-controls.css','/css/brand/campaign-needs.css'];
const scripts=['/lib/jquery/dist/jquery.min.js','/lib/bootstrap/dist/js/bootstrap.bundle.min.js','/lib/select2/select2.min.js','/lib/jquery-ui/jquery-ui.min.js','/js/shared/form-controls.js','/js/brand/campaign-needs.js'];
const cards=Array.from({length:6},(_,i)=>`<button type="button" class="needs-package" data-needs-package="p${i}" data-budget="${i<3?5000000:10000000}" data-count="${i%3+1}" data-self="${i>=3}" data-name="Gói KOC ${i+1}"><span class="needs-package-top"><span class="needs-package-icon"><i class="fa-solid fa-users"></i></span><span class="needs-package-tick"><i class="fa-solid fa-check"></i></span></span><strong class="needs-package-name">Gói KOC ${i+1}</strong><span class="needs-package-budget">${i<3?'5.000.000':'10.000.000'} đ</span><span class="needs-package-facts"><span><i class="fa-solid fa-users"></i>${i%3+1} KOC</span><span>${i>=3?'Brand tự chọn':'TopStar phân bổ'}</span></span><small class="needs-package-caption">Ngân sách gợi ý theo cấu hình hiện tại</small></button>`).join('');
const baseHtml=`<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">${styles.map(s=>`<link rel="stylesheet" href="${s}">`).join('')}</head><body class="dashboard-body dashboard-body-brand"><main class="container page-wrap campaign-needs-page"><header class="brand-page-heading needs-heading"><div><span class="brand-page-eyebrow">TẠO CHIẾN DỊCH</span><h1>Xác định nhu cầu</h1><p>Chọn gói KOC phù hợp hoặc tùy chỉnh theo kế hoạch của bạn.</p></div></header><ol class="needs-steps">${[1,2,3].map((n,i)=>`<li class="${i===0?'is-current':''}"><span>${n}</span><div><strong>${['Nhu cầu','Thông tin chiến dịch','Thanh toán'][i]}</strong><small>Kiểm tra thông tin</small></div><i class="fa-solid fa-chevron-right"></i></li>`).join('')}</ol><form id="campaignNeedsForm" class="needs-workspace" novalidate data-self-min="10000000" data-min-budget="5000000" data-per-koc="1000000" data-max-koc="100"><input id="Budget" name="Budget" value="0" type="hidden"><input id="PackageId" name="PackageId" type="hidden"><div class="needs-main"><section class="needs-panel" id="needsPackages"><div class="needs-section-heading"><span class="needs-section-icon"><i class="fa-solid fa-box-open"></i></span><div><h2>Chọn gói KOC</h2><p>Ngân sách tham khảo chưa gồm phí nền tảng và thuế.</p></div></div><div class="needs-mode-switch"><button type="button" data-needs-mode="package">Chọn gói</button><button type="button" data-needs-mode="custom">Tùy chỉnh</button></div><div class="needs-package-grid">${cards}</div></section><section class="needs-panel" id="needsDetails"><div class="needs-section-heading"><span class="needs-section-icon"><i class="fa-solid fa-sliders"></i></span><div><h2>Nhu cầu chiến dịch</h2><p id="needsSelectionLabel"></p></div></div><div class="needs-fields"><div><label>Ngân sách trả KOC</label><div class="needs-money-field"><input id="needsBudgetDisplay" class="form-control" inputmode="numeric" placeholder="Ví dụ: 7.000.000"><span>đ</span></div><span id="needsBudgetError" class="needs-field-error"></span></div><div><label>Số KOC</label><input id="KocCount" name="KocCount" value="1" type="number" class="form-control"><span id="needsCountError" class="needs-field-error"></span></div><div><label>Cách tuyển KOC</label><select id="SelfSelect" name="SelfSelect" class="form-select"><option value="false">TopStar phân bổ KOC</option><option value="true">Brand tự chọn KOC</option></select><span id="needsSelectionError" class="needs-field-error"></span></div></div><div class="needs-rule-note"><i class="fa-solid fa-circle-info"></i><span>Từ <strong>10.000.000 đ</strong> để tự chọn KOC.</span></div></section></div><aside class="needs-panel needs-summary"><div class="needs-section-heading"><span class="needs-section-icon"><i class="fa-solid fa-receipt"></i></span><div><h2>Chi phí dự kiến</h2><p>Cho nhu cầu bạn đã chọn</p></div></div><div id="needs-quote"></div><div class="needs-summary-note"><i class="fa-solid fa-shield-halved"></i><span>Kiểm tra tổng cuối cùng ở bước thanh toán.</span></div><button class="btn brand-btn-primary needs-continue" type="submit">Tiếp tục <i class="fa-solid fa-arrow-right"></i></button></aside></form></main>${scripts.map(s=>`<script src="${s}"></script>`).join('')}</body></html>`;
const html=baseHtml.replace('container page-wrap','container')
 .replace('<div class="needs-package-grid">','<button type="button" data-needs-scroll="1">Next</button><div class="needs-package-grid">')
 .replace('<section class="needs-panel" id="needsDetails">','<p id="needsCustomSummary" hidden></p><div class="modal fade" id="needsCustomModal" tabindex="-1"><div class="modal-dialog modal-lg"><div class="modal-content"><section class="modal-body" id="needsDetails">')
 .replace('</section></div><aside','</section><div class="modal-footer"><button type="button" data-bs-dismiss="modal">Cancel</button><button type="button" id="needsApplyCustom">Apply</button></div></div></div></div></div><aside');
async function main(){
server=http.createServer((req,res)=>{const url=new URL(req.url,'http://localhost');if(url.searchParams.get('handler')==='Quote'){res.setHeader('Content-Type','application/json');const budget=Number(url.searchParams.get('budget'));return res.end(JSON.stringify({budget,fee:budget*.05,vat:0,total:budget*1.05}));}if(url.pathname==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');return res.end(html);}const target=path.resolve(root,'.'+decodeURIComponent(url.pathname));if(!target.startsWith(root+path.sep)||!fs.existsSync(target)){res.statusCode=404;return res.end();}res.setHeader('Content-Type',target.endsWith('.js')?'text/javascript':target.endsWith('.css')?'text/css':'application/octet-stream');fs.createReadStream(target).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const endpoint=await new Promise((resolve,reject)=>{chrome=spawn('C:/Program Files/Google/Chrome/Application/chrome.exe',['--headless=new','--no-first-run','--disable-background-networking','--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'],{windowsHide:true});chrome.on('error',reject);chrome.stderr.on('data',d=>{const match=String(d).match(/DevTools listening on (ws:\/\/\S+)/);if(match)resolve(match[1]);});});
const targets=await(await fetch(endpoint.replace(/^ws:/,'http:').replace(/\/devtools\/.*/,'/json/list'))).json();socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);await new Promise(r=>socket.onopen=r);socket.onmessage=e=>{const m=JSON.parse(e.data);if(pending.has(m.id)){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(m.error):p.resolve(m.result);}};
const send=(method,params={})=>new Promise((resolve,reject)=>{const id=++sequence;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));});
const evaluate=async expression=>{const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;};
const wait=async expression=>{for(let i=0;i<100;i++){if(await evaluate(expression))return;await new Promise(r=>setTimeout(r,50));}throw Error('Timed out: '+expression);};

await send('Emulation.setDeviceMetricsOverride',{width:1440,height:1200,deviceScaleFactor:1,mobile:false});await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/`});
await wait('window.jQuery && document.querySelector("[data-needs-mode].is-active")');
assert.equal(await evaluate('document.querySelectorAll("[data-needs-package]").length'),6);
await evaluate('document.querySelector("[data-needs-package=p3]").click()');
await wait('document.querySelector(".needs-quote-row.is-total")');
assert.equal(await evaluate('document.querySelector("#Budget").value'),'10000000');
assert.equal(await evaluate('jQuery("#SelfSelect").val()'),'true');
assert.equal(await evaluate('document.querySelectorAll(".needs-package.is-selected").length'),1);
assert.equal(await evaluate('new Set([...document.querySelectorAll(".needs-package")].map(e=>Math.round(e.getBoundingClientRect().top))).size'),1);
await evaluate('document.querySelector("[data-needs-scroll]").click()');
await wait('document.querySelector(".needs-package-grid").scrollLeft>0');
await evaluate('document.querySelector("[data-needs-mode=custom]").click()');
await wait('document.querySelector("#needsCustomModal").classList.contains("show")');
await new Promise(r=>setTimeout(r,400));
await evaluate('jQuery("#needsBudgetDisplay").val("7000000").trigger("input");true');
await evaluate('document.querySelector("[data-bs-dismiss=modal]").click()');
await wait('!document.querySelector("#needsCustomModal").classList.contains("show") && document.querySelector("#Budget").value==="10000000"');
assert.equal(await evaluate('document.querySelector("#PackageId").value'),'p3');
await new Promise(r=>setTimeout(r,400));
await evaluate('document.querySelector("[data-needs-mode=custom]").click()');
await wait('document.querySelector("#needsCustomModal").classList.contains("show")');
await new Promise(r=>setTimeout(r,400));
await evaluate('jQuery("#needsBudgetDisplay").val("7000000").trigger("input");document.querySelector("#needsApplyCustom").click()');
await wait('!document.querySelector("#needsCustomModal").classList.contains("show")');
assert.equal(await evaluate('document.querySelector("#Budget").value'),'7000000');
assert.equal(await evaluate('document.querySelector("#needsCustomSummary").hidden'),false);
console.log('PASS: one-row carousel, arrow scrolling, modal cancel restores package and apply commits custom values.');

await evaluate('jQuery("#needsBudgetDisplay").val("7000000").trigger("input")');
await wait('document.querySelector("#Budget").value==="7000000"');
assert.equal(await evaluate('document.querySelector("#needsBudgetDisplay").value'),'7.000.000');
assert.equal(await evaluate('document.querySelector("#PackageId").value'),'');
assert.equal(await evaluate('jQuery("#SelfSelect").val()'),'false','lower budget updates selection method visibly');
await evaluate('jQuery("#KocCount").val("8").trigger("input");document.querySelector("#campaignNeedsForm").dispatchEvent(new Event("submit",{bubbles:true,cancelable:true}))');
assert.equal(await evaluate('document.querySelector("#KocCount").classList.contains("is-invalid")'),true);
await evaluate('document.querySelector("[data-needs-package=p0]").click()');
await new Promise(r=>setTimeout(r,450));
const desktop=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true});fs.writeFileSync(path.join(os.tmpdir(),'topstar-needs-desktop.png'),Buffer.from(desktop.data,'base64'));
assert.equal(await evaluate('document.documentElement.scrollWidth<=document.documentElement.clientWidth+1'),true,'desktop does not overflow');
await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
assert.equal(await evaluate('document.documentElement.scrollWidth<=document.documentElement.clientWidth+1'),true,'mobile does not overflow');
const mobile=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:true});fs.writeFileSync(path.join(os.tmpdir(),'topstar-needs-mobile.png'),Buffer.from(mobile.data,'base64'));
console.log('PASS: six package cards, selection, exact hidden values, formatted budget, self-selection threshold, custom mode, inline errors, quote and responsive layout.');
}
const timer=setTimeout(()=>{console.error('Browser test timed out');process.exitCode=1;socket?.close();chrome?.kill();server?.close();},30000);
main().catch(e=>{console.error(e);process.exitCode=1;}).finally(()=>{clearTimeout(timer);socket?.close();chrome?.kill();server?.close();});
