// Run with NODE_PATH pointing to an installation of jsdom.
const { JSDOM } = require('jsdom');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../backend/src/Topstar.Web/wwwroot');
const dom = new JSDOM(`<!doctype html><body><form id="sample" novalidate>
<div><select name="category" required data-val="true" data-val-required="Required"><option value=""></option><option value="one">One</option></select></div>
<div style="display:none"><select name="inactive"><option value=""></option></select></div>
<input type="date" name="date" value="2026-10-26"><input type="datetime-local" name="time" value="2026-10-26T14:30">
<button type="submit" name="action" value="save">Save</button></form></body>`, { runScripts: 'outside-only', url: 'http://localhost/' });
const w = dom.window;
for (const file of ['lib/jquery/dist/jquery.min.js','lib/jquery-validation/dist/jquery.validate.min.js','lib/select2/select2.min.js','lib/jquery-ui/jquery-ui.min.js']) w.eval(fs.readFileSync(path.join(root,file),'utf8'));
const $ = w.jQuery, form = w.document.getElementById('sample');
const validator = $(form).validate({ ignore: ':hidden' });
w.eval(fs.readFileSync(path.join(root,'js/shared/form-controls.js'),'utf8'));
const wait = () => new Promise(resolve => setTimeout(resolve,30));
(async () => {
 await wait();
 const category = form.elements.category, inactive = form.elements.inactive;
 assert(category.classList.contains('select2-hidden-accessible'));
 assert.equal(validator.settings.ignore(0,category),false,'visible Select2 must be validated');
 assert.equal(validator.settings.ignore(0,inactive),true,'inactive tab must be ignored');
 $(category).val('one');
 assert.equal($(category).next().find('.select2-selection__rendered').text(),'One');
 const date = form.elements.date, time = form.elements.time;
 assert.equal($(date).next().find('.ts-date-display').val(),'26/10/2026');
 time.value='2026-11-03T16:45';
 assert.equal($(time).next().find('.ts-date-display').val(),'03/11/2026');
 assert.equal($(time).next().find('.ts-date-time').val(),'16:45');
 $(date).next().find('.ts-date-display').val('30/10/2026').trigger('change');
 assert.equal(date.value,'2026-10-30');
 $(date).next().find('.ts-date-display').val('bad date').trigger('change');
 assert.equal(date.value,'','invalid date must not retain old value');
 assert.equal(date.validity.customError,true);
 const button = form.querySelector('button');
 const event = new w.Event('submit',{cancelable:true});
 Object.defineProperty(event,'submitter',{value:button});
 w.TopstarForms.guardNativeSubmit(form,event);
 button.disabled=true; button.innerHTML='Saving'; event.preventDefault();
 await wait();
 assert.equal(button.disabled,false,'cancelled submit must restore button');
 assert.equal(button.textContent,'Save');
 assert.equal(form.querySelector('[data-ts-submitter]').value,'save','submitter value must survive disabling');
 form.reset(); await wait();
 assert.equal($(category).next().find('.select2-selection__rendered').text(),'');
 console.log('PASS: Select2 validation, inactive tabs, programmatic values, date/time round trips, invalid dates, cancelled submit, submitter preservation and reset.');
 dom.window.close();
})().catch(error => { console.error(error); dom.window.close(); process.exitCode=1; });
