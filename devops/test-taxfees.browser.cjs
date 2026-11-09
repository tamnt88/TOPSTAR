// UI fixture only: real Select2, stylesheet and page script; no application database writes.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),os=require('node:os'),assert=require('node:assert/strict');
const {spawn}=require('node:child_process');
const root=path.resolve(__dirname,'../backend/src/Topstar.Web/wwwroot');
const profile=fs.mkdtempSync(path.join(os.tmpdir(),'topstar-needs-ui-'));
let server,chrome,socket,sequence=0;const pending=new Map();
const styles=['/lib/bootstrap/dist/css/bootstrap.min.css','/lib/fontawesome/css/all.min.css','/css/brand/brand.css','/lib/select2/select2.min.css','/css/shared/form-controls.css','/css/brand/campaign-needs.css'];
const scripts=['/lib/jquery/dist/jquery.min.js','/lib/select2/select2.min.js','/lib/jquery-ui/jquery-ui.min.js','/js/shared/form-controls.js','/js/brand/campaign-needs.js'];
const cards=Array.from({length:6},(_,i)=>`<button type="button" class="needs-package" data-needs-package="p${i}" data-budget="${i<3?5000000:10000000}" data-count="${i%3+1}" data-self="${i>=3}" data-name="Gói KOC ${i+1}"><span class="needs-package-top"><span class="needs-package-icon"><i class="fa-solid fa-users"></i></span><span class="needs-package-tick"><i class="fa-solid fa-check"></i></span></span><strong class="needs-package-name">Gói KOC ${i+1}</strong><span class="needs-package-budget">${i<3?'5.000.000':'10.000.000'} đ</span><span class="needs-package-facts"><span><i class="fa-solid fa-users"></i>${i%3+1} KOC</span><span>${i>=3?'Brand tự chọn':'TopStar phân bổ'}</span></span><small class="needs-package-caption">Ngân sách gợi ý theo cấu hình hiện tại</small></button>`).join('');
const brandHtml=fs.readFileSync(path.join(os.tmpdir(),'topstar-taxfees-brand.html'),'utf8');
const creationHtml=fs.readFileSync(path.join(os.tmpdir(),'topstar-creation-config.html'),'utf8');
const kocHtml=fs.readFileSync(path.join(os.tmpdir(),'topstar-taxfees-koc.html'),'utf8');
async function main(){
server=http.createServer((req,res)=>{const url=new URL(req.url,'http://localhost');if(url.pathname==='/campaign-simple'){res.setHeader('Content-Type','text/html; charset=utf-8');return res.end(fs.readFileSync(path.join(os.tmpdir(),'topstar-campaign-simple.html'),'utf8'));}if(url.pathname==='/creation'){res.setHeader('Content-Type','text/html; charset=utf-8');return res.end(creationHtml);}if(url.pathname==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');return res.end(url.searchParams.get('tab')==='Koc'?kocHtml:brandHtml);}const target=path.resolve(root,'.'+decodeURIComponent(url.pathname));if(!target.startsWith(root+path.sep)||!fs.existsSync(target)){res.statusCode=404;return res.end();}res.setHeader('Content-Type',target.endsWith('.js')?'text/javascript':target.endsWith('.css')?'text/css':'application/octet-stream');fs.createReadStream(target).pipe(res);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const endpoint=await new Promise((resolve,reject)=>{chrome=spawn('C:/Program Files/Google/Chrome/Application/chrome.exe',['--headless=new','--no-first-run','--disable-background-networking','--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'],{windowsHide:true});chrome.on('error',reject);chrome.stderr.on('data',d=>{const match=String(d).match(/DevTools listening on (ws:\/\/\S+)/);if(match)resolve(match[1]);});});
const targets=await(await fetch(endpoint.replace(/^ws:/,'http:').replace(/\/devtools\/.*/,'/json/list'))).json();socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);await new Promise(r=>socket.onopen=r);socket.onmessage=e=>{const m=JSON.parse(e.data);if(pending.has(m.id)){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(m.error):p.resolve(m.result);}};
const send=(method,params={})=>new Promise((resolve,reject)=>{const id=++sequence;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));});
const evaluate=async expression=>{const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;};
const wait=async expression=>{for(let i=0;i<100;i++){if(await evaluate(expression))return;await new Promise(r=>setTimeout(r,50));}throw Error('Timed out: '+expression);};

await send('Emulation.setDeviceMetricsOverride',{width:1440,height:1200,deviceScaleFactor:1,mobile:false});await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/`});
await wait('window.jQuery && document.querySelector("#VatMode + .select2")');
assert.equal(await evaluate('document.querySelectorAll(".tax-fees-tabs .nav-link").length'),2);
assert.equal(await evaluate('document.querySelector("#Scope").value'),'Funding');
assert.equal(await evaluate('document.querySelector("#VatRate").value'),'8');
assert.equal(await evaluate('document.querySelectorAll(".tax-fees-tabs .active").length'),1);
assert.equal(await evaluate('new Set([...document.querySelectorAll(".tax-brand-fee-grid>div")].map(e=>Math.round(e.getBoundingClientRect().top))).size'),1);
assert.equal(await evaluate('new Set([...document.querySelectorAll(".tax-vat-grid>div")].map(e=>Math.round(e.getBoundingClientRect().bottom))).size'),1);
assert.equal(await evaluate('document.querySelector(".tax-fees-tabs").classList.contains("ts-tabs")'),true);

await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false}).then(r=>fs.writeFileSync(path.join(os.tmpdir(),'topstar-taxfees-brand.png'),Buffer.from(r.data,'base64')));
assert.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth'),true);
await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/?tab=Koc`});
await wait('window.jQuery && document.querySelector("#KocFeeRate")');
assert.equal(await evaluate('document.querySelectorAll(".tax-fees-page form").length'),3);
assert.equal(await evaluate('document.querySelectorAll(".ts-date-icon").length>0'),true);
assert.equal(await evaluate('document.querySelectorAll(".tax-fees-block .card-x").length'),0);
assert.equal(await evaluate('document.querySelectorAll(".tax-fees-page form[data-confirm]").length'),0);
assert.equal(await evaluate('new Set([...document.querySelectorAll(".tax-koc-tax-grid>div")].map(e=>Math.round(e.getBoundingClientRect().bottom))).size'),1);
assert.equal(await evaluate('new Set([...document.querySelectorAll(".tax-payout-grid>div")].map(e=>Math.round(e.getBoundingClientRect().bottom))).size'),2);
assert.equal(await evaluate('document.querySelector("#Rate").value'),'10');
assert.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth'),true);
await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false}).then(r=>fs.writeFileSync(path.join(os.tmpdir(),'topstar-taxfees-koc.png'),Buffer.from(r.data,'base64')));
await evaluate('document.querySelector(".ts-date-icon").click()');
await wait('getComputedStyle(document.getElementById("ui-datepicker-div")).display!=="none"');
assert.equal(await evaluate('document.querySelectorAll("#ui-datepicker-div .ui-datepicker-calendar").length'),1);
await evaluate('jQuery(".ts-date-display").datepicker("hide");true');
await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/creation`});
await wait('document.querySelector("#Input_MinimumBudget").classList.contains("ts-money-original")');
assert.equal(await evaluate('jQuery("#Input_MinimumBudget").next().find("input").val()'),'5.000.000');
assert.equal(await evaluate('jQuery("#Input_MinimumBudget").next().find(".ts-money-unit").text()'),'đ');
await evaluate('jQuery("#Input_MinimumBudget").next().find("input").val("7.500.000").trigger("input");true');
assert.equal(await evaluate('document.querySelector("#Input_MinimumBudget").value'),'7500000');
assert.equal(await evaluate('new FormData(document.querySelector("#Input_MinimumBudget").form).get("Input.MinimumBudget")'),'7500000');
await evaluate('document.querySelector("#Input_MinimumBudget").value="8000000.00"');
assert.equal(await evaluate('jQuery("#Input_MinimumBudget").next().find("input").val()'),'8.000.000');
await evaluate('jQuery("#Input_MinimumBudget").next().find("input").val("7,5").trigger("input");true');
assert.equal(await evaluate('document.querySelector("#Input_MinimumBudget").validity.customError'),true);
assert.equal(await evaluate('document.querySelector("#Input_MinimumBudget").value'),'');
await evaluate('jQuery("#Input_MinimumBudget").next().find("input").val("9000000").trigger("input");true');
assert.equal(await evaluate('document.querySelector("#Input_MinimumBudget").validity.customError'),false);
// Real keystrokes must work across every grouping boundary, not just pasted values.
for(const fieldId of ['Input_MinimumBudget','Input_SelfSelectionMinimumBudget','Input_MinimumBudgetPerKoc']) {
 await evaluate(`(()=>{const e=jQuery('#${fieldId}').next().find('input')[0];e.value='';e.dispatchEvent(new Event('input',{bubbles:true}));e.focus();})()`);
 for(const digit of '123456789')await send('Input.insertText',{text:digit});
 assert.equal(await evaluate(`document.getElementById('${fieldId}').value`),'123456789');
 assert.equal(await evaluate(`jQuery('#${fieldId}').next().find('input').val()`),'123.456.789');
 await send('Input.dispatchKeyEvent',{type:'keyDown',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8});
 await send('Input.dispatchKeyEvent',{type:'keyUp',key:'Backspace',code:'Backspace',windowsVirtualKeyCode:8});
 assert.equal(await evaluate(`document.getElementById('${fieldId}').value`),'12345678');
 await evaluate(`(()=>{const e=jQuery('#${fieldId}').next().find('input')[0];e.setSelectionRange(1,1);})()`);
 await send('Input.insertText',{text:'9'});
 assert.equal(await evaluate(`document.getElementById('${fieldId}').value`),'192345678');
 assert.equal(await evaluate(`new FormData(document.getElementById('${fieldId}').form).get(document.getElementById('${fieldId}').name)`),'192345678');
}
console.log('PASS: real typing, backspace, middle insertion and raw submission for all 3 budget fields.');
assert.equal(await evaluate('getComputedStyle(document.querySelector("a[href*=TaxFees]")).textDecorationLine.includes("underline")'),false);
await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false}).then(r=>fs.writeFileSync(path.join(os.tmpdir(),'topstar-creation-standard.png'),Buffer.from(r.data,'base64')));
await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
assert.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth'),true);
await send('Emulation.setDeviceMetricsOverride',{width:1440,height:1200,deviceScaleFactor:1,mobile:false});
await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/campaign-simple`});
await wait('document.querySelector("#campaignAdditional") && window.jQuery && document.querySelector(".ts-date-icon")');
assert.equal(await evaluate('document.querySelector("#campaignAdditional").open'),false);

assert.equal(await evaluate('document.querySelector("#Input_Title").getBoundingClientRect().height>0'),true);
assert.equal(await evaluate('document.querySelector("#Input_AllocationMode").type'),'hidden');
assert.equal(await evaluate('document.querySelector("#campaignBudgetDisplay")'),null);
assert.equal(await evaluate('document.querySelector("#Input_DesiredKocCount").type'),'hidden');
assert.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth'),true);
await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false}).then(r=>fs.writeFileSync(path.join(os.tmpdir(),'topstar-campaign-simple.png'),Buffer.from(r.data,'base64')));
await evaluate('document.querySelector("#campaignAdditional>summary").click()');
assert.equal(await evaluate('document.querySelector("#campaignAdditional").open'),true);
await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
assert.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth'),true);
console.log('PASS: actual simplified campaign Razor has collapsed optional fields, preserved hidden needs, and no desktop/mobile overflow.');
console.log('PASS: actual Razor Brand/KOC tabs, active configuration values, form targets, Select2 and responsive widths.');

}
main().catch(e=>{console.error(e);process.exitCode=1;}).finally(()=>{socket?.close();chrome?.kill();server?.close();});
