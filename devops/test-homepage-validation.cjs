const { JSDOM } = require('jsdom');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const source = fs.readFileSync(path.resolve(__dirname, '../backend/src/Topstar.Web/wwwroot/js/admin/homepage-content.js'), 'utf8');
const wait = () => new Promise(resolve => setTimeout(resolve, 20));
function setup(serverError = false) {
    const dom = new JSDOM('<button data-home-tab="hero"></button><button data-home-tab="creators"></button><form data-homepage-form><section data-home-panel="hero"><input id="hero" class="' + (serverError ? 'input-validation-error' : '') + '"></section><section data-home-panel="creators" class="active"><input id="creator"></section><textarea id="reason"></textarea></form>', {
        runScripts: 'outside-only', url: 'http://localhost/Admin/Settings/HomepageContent#creators'
    });
    dom.window.HTMLElement.prototype.scrollIntoView = function () {};
    dom.window.TopstarNotify = { warning() {} };
    dom.window.eval(source);
    return dom;
}
(async () => {
    const server = setup(true);
    await wait();
    assert.equal(server.window.document.querySelector('[data-home-panel="hero"]').classList.contains('active'), true, 'Server error must open the relevant tab');
    server.window.close();
    const client = setup();
    const doc = client.window.document;
    doc.addEventListener('submit', event => {
        doc.getElementById('hero').classList.add('is-invalid');
        event.preventDefault();
        event.stopImmediatePropagation();
    });
    doc.querySelector('form').dispatchEvent(new client.window.SubmitEvent('submit', { bubbles: true, cancelable: true }));
    await wait();
    assert.equal(doc.querySelector('[data-home-panel="hero"]').classList.contains('active'), true, 'Admin validation error must reveal hidden panel');
    assert.equal(doc.activeElement.id, 'hero');
    client.window.close();
    console.log('Homepage validation checks passed.');
})().catch(error => { console.error(error); process.exitCode = 1; });
