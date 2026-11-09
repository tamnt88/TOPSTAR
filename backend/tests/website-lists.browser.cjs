// Runs the real local DataTables and shared list adapter in headless Chrome.
// No database, user accounts or external requests are used.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const os = require('node:os');
const { spawn } = require('node:child_process');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../src/Topstar.Web/wwwroot');
const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'topstar-lists-'));
let chrome, socket, server;
let sequence = 0;
const pending = new Map();
async function main() {
    const html = `<!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css"><link rel="stylesheet" href="/lib/datatables/css/dataTables.bootstrap5.min.css"><link rel="stylesheet" href="/css/shared/lists.css"></head><body>
        <input id="search"><select id="status"><option value="">Tất cả</option><option value="Active">Active</option></select><select id="size"></select><button id="clear">Xóa lọc</button>
        <table id="table" data-clear-filters="#clear"><thead><tr><th>Name</th></tr></thead></table>
        <script src="/lib/jquery/dist/jquery.min.js"></script><script src="/lib/datatables/js/dataTables.min.js"></script><script src="/lib/datatables/js/dataTables.bootstrap5.min.js"></script><script src="/js/shared/lists.js"></script>
        <script>window.requests=[]; window.table=$('#table').DataTable(TopstarLists.configure('#table', {serverSide:true,ordering:false,columns:[{data:'name'}],ajax:{url:'/data',data:d=>{d.query=$('#search').val();d.status=$('#status').val();requests.push({...d});}}}, {query:'#search',status:'#status'}, '#size'));</script></body></html>`;
    server = http.createServer((req, res) => {
        const url = new URL(req.url, 'http://localhost');
        if (url.pathname === '/' && !url.searchParams.has('handler')) {
            let content = html;
            if (url.searchParams.get('fixture') === 'brand') content = html.replace(/<input id="search">[\s\S]*?<script src=/, `<form id="notificationAntiForgery"><input value="test-token"></form><button id="brandMarkAll"></button><span id="brandUnreadCount"></span><form id="brandNotificationFilters"><input id="brandNotificationSearch"><select id="brandNotificationFilter"><option value="all">All</option><option value="unread">Unread</option></select><select id="brandNotificationSize"></select><button id="brandNotificationClear" type="button"></button></form><div id="brandNotificationFeedback" hidden></div><table id="brandNotificationTable" data-clear-filters="#brandNotificationClear"><thead><tr>${'<th>Column</th>'.repeat(6)}</tr></thead></table><script src=`).replace(/<script>window.requests=[\s\S]*?<\/script>/, '<script src="/js/brand/brand-notifications.js"></script>');
            if (url.searchParams.get('fixture') === 'koc') content = html.replace('</head>', '<link rel="stylesheet" href="/css/koc/koc-theme.css"><link rel="stylesheet" href="/css/koc/koc-foundation.css"></head>').replace('<body>', '<body class="dashboard-body-koc">').replace(/<input id="search">[\s\S]*?<script src=/, `<form id="notificationAntiForgery"><input name="__RequestVerificationToken" value="test-token"></form><script id="notificationData" type="application/json">[]</script><button id="markAllNotificationsRead"></button><span data-notification-count="unreadCount"></span><input id="notificationSearch"><input id="notificationFilter" type="hidden" value="all"><select id="notificationPageLength"></select><button id="notificationApply"></button><button id="notificationClear"></button><div class="koc-notification-table-wrap"><table id="kocNotificationTable" data-clear-filters="#notificationClear"><thead><tr>${'<th>Column</th>'.repeat(4)}</tr></thead></table></div><script src=`).replace(/<script>window.requests=[\s\S]*?<\/script>/, '<script src="/js/koc/notifications.js"></script>');
            res.setHeader('Content-Type', 'text/html; charset=utf-8'); return res.end(content);
        }
        if (url.pathname === '/data' || url.searchParams.get('handler') === 'Data') {
            if (url.searchParams.get('query') === 'fail') { res.statusCode = 500; return res.end('test failure'); }
            const total = url.searchParams.get('query') === 'empty' ? 0 : 63;
            const start = Number(url.searchParams.get('start')), length = Number(url.searchParams.get('length'));
            res.setHeader('Content-Type', 'application/json');
            return res.end(JSON.stringify({draw:Number(url.searchParams.get('draw')),recordsTotal:63,recordsFiltered:total,summary:{unreadCount:63,activeCount:63,archivedCount:0},data:Array.from({length:Math.max(0,Math.min(length,total-start))}, (_,i)=>({name:'Row '+(start+i+1),id:'id-'+i,title:'Notice '+(start+i+1),body:'Body',isVisible:true,isRead:false,createdAtUtc:'2026-09-08T00:00:00Z',history:[]}))}));
        }
        const target = path.resolve(root, '.' + decodeURIComponent(url.pathname));
        if (!target.startsWith(root + path.sep) || !fs.existsSync(target)) { res.statusCode = 404; return res.end(); }
        res.setHeader('Content-Type', target.endsWith('.js') ? 'text/javascript' : 'text/css');
        fs.createReadStream(target).pipe(res);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const endpoint = await new Promise((resolve, reject) => {
        chrome = spawn(process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe', ['--headless=new','--no-first-run','--disable-background-networking','--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'], {windowsHide:true});
        chrome.on('error', reject);
        chrome.stderr.on('data', data => { const match=String(data).match(/DevTools listening on (ws:\/\/\S+)/); if(match) resolve(match[1]); });
    });
    const targets = await (await fetch(endpoint.replace(/^ws:/,'http:').replace(/\/devtools\/.*/, '/json/list'))).json();
    socket = new WebSocket(targets.find(target=>target.type==='page').webSocketDebuggerUrl);
    await new Promise(resolve => socket.onopen=resolve);
    socket.onmessage = event => { const message=JSON.parse(event.data); if(pending.has(message.id)){const {resolve,reject}=pending.get(message.id);pending.delete(message.id);message.error?reject(message.error):resolve(message.result);} };
    const send = (method,params={}) => new Promise((resolve,reject)=>{const id=++sequence;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));});
    const evaluate = async expression => {const result=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(result.exceptionDetails)throw new Error(JSON.stringify(result.exceptionDetails));return result.result.value;};
    const wait = async expression => { for(let attempt=0;attempt<100;attempt++){if(await evaluate(expression))return;await new Promise(resolve=>setTimeout(resolve,50));}throw new Error('Timed out: '+expression); };
    await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/?query=hello&status=Active&page=2&pageSize=20&focus=keep`});
    await wait('window.table && typeof table.rows === "function" && table.rows().count()===20');
    assert.deepEqual(await evaluate('({start:requests[0].start,length:requests[0].length,query:requests[0].query,status:requests[0].status})'),{start:20,length:20,query:'hello',status:'Active'});
    assert.deepEqual(await evaluate('[...document.querySelector("#size").options].map(x=>x.value)'),['10','20','50','100']);
    assert.equal(await evaluate('document.querySelector(".dt-paging .next").getAttribute("aria-label")'),'Trang sau');
    await evaluate('table.page(2).draw("page");void 0');
    await wait('new URL(location.href).searchParams.get("page")==="3"');
    assert.equal(await evaluate('new URL(location.href).searchParams.get("focus")'),'keep');
    await evaluate('document.querySelector("#clear").click()');
    await wait('table.page.info().page===0 && requests.at(-1).query===""');
    await evaluate('document.querySelector("#search").value="fail";table.ajax.reload(null,false);void 0');
    await wait('!document.querySelector(".list-feedback").hidden');
    assert.equal(await evaluate('table.rows().count()'),20);
    await evaluate('document.querySelector("#search").value="hello";document.querySelector(".list-feedback button").click()');
    await wait('document.querySelector(".list-feedback").hidden && requests.at(-1).query==="hello"');
    await evaluate('table.page(2).draw("page");void 0');
    await wait('table.page.info().page===2 && table.rows().count()===20');
    await evaluate('document.querySelector("#search").value="empty";table.ajax.reload(null,false);void 0');
    await wait('table.page.info().page===0 && table.rows().count()===0');
    await send('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
    assert.equal(await evaluate('document.querySelector(".dt-paging").scrollWidth <= document.documentElement.clientWidth'),true);
    for (const portal of ['brand','koc']) {
        await send('Page.navigate',{url:`http://127.0.0.1:${server.address().port}/?fixture=${portal}`});
        await wait('window.jQuery && jQuery.fn.DataTable && jQuery.fn.DataTable.isDataTable(document.querySelector("table")) && jQuery("table").DataTable().rows().count()===20');
        assert.equal(await evaluate('jQuery("table").DataTable().page.info().recordsTotal'),63);
        assert.equal(await evaluate('document.querySelector(".dt-paging .next").getAttribute("aria-label")'),'Trang sau');
        if (portal === 'koc') {
            const mobileCardMetrics = await evaluate(`(() => {
                const row = document.querySelector('#kocNotificationTable tbody tr');
                const cell = row?.querySelector('td');
                const summary = row?.querySelector('.koc-notification-summary');
                return { row: row?.getBoundingClientRect().width || 0, cell: cell?.getBoundingClientRect().width || 0, display: cell ? getComputedStyle(cell).display : '', summary: summary?.getBoundingClientRect().width || 0 };
            })()`);
            assert.equal(mobileCardMetrics.summary >= mobileCardMetrics.row - 28, true, JSON.stringify(mobileCardMetrics));
            assert.equal(await evaluate(`(() => {
                const buttons = [...document.querySelector('.koc-notification-actions')?.querySelectorAll('button') || []];
                return buttons.length > 1 && Math.max(...buttons.map(x => x.getBoundingClientRect().top)) - Math.min(...buttons.map(x => x.getBoundingClientRect().top)) < 3;
            })()`), true);
        }
    }
    console.log('PASS: URL restore, page sizes, pagination labels, state preservation, clear, load failure/retry, empty-page recovery, mobile pager, full-width KOC notification cards/actions, Brand/KOC notification scripts.');
}
const timeout=setTimeout(()=>{console.error('Browser test timeout');process.exitCode=1;socket?.close();chrome?.kill();server?.close();},30000);
main().catch(error=>{console.error(error);process.exitCode=1;}).finally(()=>{clearTimeout(timeout);socket?.close();chrome?.kill();server?.close();});
