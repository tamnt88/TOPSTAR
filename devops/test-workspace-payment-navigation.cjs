const { JSDOM } = require('jsdom');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const dom = new JSDOM('<button data-workspace-tab="finance"></button><section data-workspace-panel="finance"><div class="campaign-workspace-native" data-src="/Brand/CampaignFunding?campaignId=test"></div></section>', {
    runScripts: 'outside-only', url: 'http://localhost:7161/Brand/CampaignWorkspace?id=test#tab-finance'
});
const w = dom.window, requests = [];
w.fetch = (url, options) => { requests.push({ url, options }); return new Promise(() => {}); };
w.eval(fs.readFileSync(path.resolve(__dirname, '../backend/src/Topstar.Web/wwwroot/js/brand/campaign-workspace.js'), 'utf8'));
const host = w.document.querySelector('.campaign-workspace-native');
function submit(action, native = false) {
    const form = w.document.createElement('form');
    form.action = action; form.method = 'post';
    if (native) form.setAttribute('data-native-navigation', '');
    host.appendChild(form);
    const event = new w.SubmitEvent('submit', { bubbles: true, cancelable: true });
    form.dispatchEvent(event);
    return event;
}
for (const action of ['/Brand/CampaignFunding?handler=VnPay', '/Brand/CampaignFunding?handler=OnePay', '/Brand/CampaignFunding?handler=VNPAY']) {
    const before = requests.length;
    assert.equal(submit(action).defaultPrevented, false, 'Payment must retain browser navigation');
    assert.equal(requests.length, before, 'Payment must not use fetch');
}
const before = requests.length;
assert.equal(submit('/Brand/PackageCheckout', true).defaultPrevented, false);
assert.equal(requests.length, before, 'Package checkout may redirect externally');
assert.equal(submit('/Brand/CampaignFunding?handler=Save').defaultPrevented, true);
assert.equal(requests.at(-1).options.method, 'POST', 'Ordinary workspace saves still use AJAX');
console.log('Workspace payment navigation checks passed.');
dom.window.close();
