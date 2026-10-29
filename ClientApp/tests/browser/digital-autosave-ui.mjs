// React/Edge reales; HTTP sintético con latencia y fallos controlados, sin bases institucionales.
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';

const baseline=process.argv.includes('--baseline');
const output=path.resolve('../.artifacts/digital-autosave-20261029',baseline?'before':'after');await mkdir(output,{recursive:true});
const instrument=JSON.parse(await readFile('../tdv2/Contracts/competencias-digitales-v2.json','utf8'));
const base='/competencias-digitales',fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true});
const profile=Object.fromEntries(instrument.profile.map(f=>[f.id,f.multiple?[f.options[0]]:f.options[0]]));
const steps=[{id:'perfil',name:'Perfil'},...instrument.modules.map(b=>({id:b.id,name:b.n}))];
const unit={id:'A',code:'100',description:'Unidad sintética',level:2};
let view,writes,receipts,delay=0,failure=0,active=0,maxActive=0,concurrent=false;
function reset(){view={version:1,generation:'11111111-1111-4111-8111-111111111111',completedAt:null,accepted:true,instrument,profile:structuredClone(profile),answers:{},institution:{status:'available',name:'Persona Sintética',employee:'0001',unit,level2:unit,level3:null},navigation:{current:'m365',resume:'m365',steps:steps.map((s,i)=>({...s,complete:i===0,accessible:i<2,pending:i===0?[]:['m-word','m-excel']}))},evaluation:{answered:6,total:53,progress:11,report:{}}};writes=[];receipts=new Set();delay=0;failure=0;active=0;maxActive=0;}
fixture.pages={[base+'/encuesta']:()=>({component:'CompetenciasDigitales',url:base+'/encuesta',props:{name:'TDV2',csrfToken:'synthetic',photoContext:'synthetic',photoApplication:'competencias-digitales',auth:{user:{id:1,name:'Persona Sintética',email:'persona@example.test'},roles:[],modules:[]},simulacion:null,representacion:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:base,logout:'/logout',connect:'/connect'},digital:{page:2,generation:view.generation,user:'Persona Sintética',actor:'Persona Sintética',readOnly:false,notice:instrument.notice,content:structuredClone(view),navigation:[{path:base+'/encuesta',name:'Encuesta'}]}}})};
fixture.handleRequest=async(req,url,json)=>{
 if(url.pathname==='/user/photo'){json({photo:null,estado:'sin_foto',contexto:'synthetic'});return true;}
 if(url.pathname===base+'/api/generacion'){json({generation:view.generation});return true;}
 if(url.pathname===base+'/api/encuesta'&&req.method==='GET'){json(view);return true;}
 if(!url.pathname.startsWith(base+'/api/'))return false;
 let raw='';for await(const c of req)raw+=c;const body=JSON.parse(raw||'{}');
 const entry={method:req.method,path:url.pathname,body,started:performance.now()};writes.push(entry);active++;maxActive=Math.max(maxActive,active);
 try{
  if(delay)await new Promise(r=>setTimeout(r,delay));
  if(failure){json({message:'Fallo sintético: conserva tus respuestas y reintenta.'},failure);return true;}
  if(req.method==='PUT'){
   const same=JSON.stringify(body.profile)===JSON.stringify(view.profile)&&JSON.stringify(body.answers)===JSON.stringify(view.answers);
   if(body.version!==view.version&&!same){json({message:'Conflicto de revisión sintético.'},409);return true;}
   if(!same)view.version++;
   view.profile=structuredClone(body.profile);view.answers=structuredClone(body.answers);
   if(concurrent){view.answers['m-word']={state:'level',level:2};view.version++;}
  }else{assert.equal(url.pathname,base+'/api/encuesta/guardar');if(!receipts.has(body.operation)){assert.equal(body.version,view.version);receipts.add(body.operation);}}
  json(view);return true;
 }finally{entry.durationMs=performance.now()-entry.started;active--;}
};
const autos=()=>writes.filter(w=>w.method==='PUT'),manuals=()=>writes.filter(w=>w.method==='POST');
const report={baseline,syntheticTransport:true,realPostgreSql:false,checks:[],measurements:[],errors:[]};
try{
 for(const width of [1440,390]){
  const context=await browser.newContext({viewport:{width,height:960},reducedMotion:'reduce'});
  await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
  await context.addInitScript(()=>{
   window.__commits=0;window.__REACT_DEVTOOLS_GLOBAL_HOOK__={supportsFiber:true,inject:()=>1,onCommitFiberRoot:()=>window.__commits++,onCommitFiberUnmount:()=>{}};
   window.__answerChanges=[];document.addEventListener('change',e=>{if(e.target?.name==='m-word')window.__answerChanges.push(performance.now());});
  });
  const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
  const cdp=await context.newCDPSession(page);await cdp.send('Performance.enable');
  const metric=async()=>Object.fromEntries((await cdp.send('Performance.getMetrics')).metrics.map(m=>[m.name,m.value]));
  const save=page.getByRole('button',{name:'Guardar avance',exact:true});
  const radio=n=>page.locator(`[id="m-word"] input[value="${n}"]`);
  async function fresh(){reset();await page.goto(fixture.origin+base+'/encuesta');await expect(radio(0)).toBeEnabled();await page.evaluate(()=>document.fonts.ready);await radio(0).scrollIntoViewIfNeeded();}
  async function settled(){
   if(baseline){await expect.poll(()=>active).toBe(0);await expect(page.locator('.cd-save-actions')).toContainText('Avance guardado');}
   else {await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado',{timeout:15000});await expect.poll(()=>active).toBe(0);}
  }
  await fresh();delay=2000;
  await page.evaluate(()=>{
   const input=document.querySelector('[id="m-word"] input[value="0"]'),content=document.getElementById('cd-step-content');
   window.__probe={input,content,disabledMs:0,maxShift:0,samples:0,start:performance.now(),last:performance.now(),top:content.getBoundingClientRect().top+scrollY,remounts:0};
   window.__probe.timer=setInterval(()=>{const p=window.__probe,now=performance.now();if(p.input.matches(':disabled'))p.disabledMs+=now-p.last;p.last=now;p.samples++;p.maxShift=Math.max(p.maxShift,Math.abs(p.content.getBoundingClientRect().top+scrollY-p.top));if(!p.input.isConnected)p.remounts++;},10);
  });
  const metrics=[];
  for(const n of [1,2,0,3,1]){
   const before=await metric(),commits=await page.evaluate(()=>window.__commits);
   const interaction=await radio(n).evaluate(input=>new Promise(resolve=>{const start=performance.now();input.focus();input.click();requestAnimationFrame(()=>requestAnimationFrame(()=>resolve({ms:performance.now()-start,checked:input.checked})));}));
   const after=await metric();metrics.push({...interaction,scriptMs:(after.ScriptDuration-before.ScriptDuration)*1000,layoutMs:(after.LayoutDuration-before.LayoutDuration)*1000,commits:await page.evaluate(()=>window.__commits)-commits});
  }
  await expect.poll(()=>autos().length).toBe(1);
  const whilePending={enabled:await radio(3).isEnabled(),saveEnabled:await save.isEnabled()};
  await page.screenshot({path:path.join(output,`saving-${width}.png`)});
  if(!baseline){await radio(3).check();await radio(0).check();await expect(radio(0)).toBeChecked();}
  const scrollBefore=await page.evaluate(()=>scrollY);
  await settled();
  const scrollChange=await page.evaluate(y=>scrollY-y,scrollBefore);
  const probe=await page.evaluate(()=>{const p=window.__probe;clearInterval(p.timer);return {disabledMs:p.disabledMs,maxShift:p.maxShift,remounts:p.remounts,sameInput:p.input===document.querySelector('[id="m-word"] input[value="0"]'),focus:document.activeElement?.getAttribute('name')};});
  const durations=await page.evaluate(()=>performance.getEntriesByType('resource').filter(e=>e.name.endsWith('/api/encuesta')).map(e=>e.duration));
  const beforeSendMs=await page.evaluate(()=>performance.getEntriesByType('resource').filter(e=>e.name.endsWith('/api/encuesta')).map(e=>e.startTime-Math.max(...window.__answerChanges.filter(t=>t<=e.startTime))));
  report.measurements.push({width,controlledDelayMs:delay,metrics,whilePending,...probe,scrollChange,beforeSendMs,requestMs:durations,requests:autos().length,requestBytes:autos().map(w=>Buffer.byteLength(JSON.stringify(w.body))),maxActive});
  await page.screenshot({path:path.join(output,`survey-${width}.png`),fullPage:false});
  if(baseline){assert.equal(whilePending.enabled,false);await context.close();continue;}
  assert.equal(probe.remounts,0);assert.equal(probe.sameInput,true);assert.equal(probe.disabledMs,0);assert.equal(probe.maxShift,0);assert.equal(scrollChange,0);assert.equal(probe.focus,'m-word');assert.equal(maxActive,1);assert.equal(view.answers['m-word'].level,0);assert.equal(manuals().length,0);
  report.checks.push({width,case:'selecciones durante red lenta, última respuesta, sin bloqueo/remontaje/desplazamiento ni evento manual'});
  delay=0;await page.reload();await expect(radio(0)).toBeChecked();report.checks.push({width,case:'recarga conserva último cero válido'});
  await fresh();delay=1800;await radio(1).check();await expect.poll(()=>autos().length).toBe(1);await radio(2).check();await save.click();await settled();
  assert.equal(view.answers['m-word'].level,2);assert.equal(manuals().length,1);assert.equal(receipts.size,1);assert.equal(manuals()[0].body.version,view.version);assert.equal(maxActive,1);
  report.checks.push({width,case:'guardar manual durante autoguardado drena última respuesta, un evento'});
  await fresh();failure=503;delay=1800;
  const stoppedTop=await page.locator('#cd-step-content').evaluate(e=>e.getBoundingClientRect().top+scrollY);
  await radio(1).check();await expect.poll(()=>autos().length).toBe(1);await radio(2).check();
  await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');await expect(radio(2)).toBeFocused();delay=0;
  for(const level of [2,0,3]){await radio(level).check();await page.waitForTimeout(1650);assert.equal(autos().length,1);}
  failure=0;await page.evaluate(()=>{dispatchEvent(new Event('online'));dispatchEvent(new Event('focus'));document.dispatchEvent(new Event('visibilitychange'));});
  await page.waitForTimeout(1700);assert.equal(autos().length,1);assert.equal(receipts.size,0);
  await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');await expect(radio(3)).toBeChecked();
  assert.equal(await page.locator('#cd-step-content').evaluate(e=>e.getBoundingClientRect().top+scrollY),stoppedTop);
  await page.locator('.cd-save-status').evaluate(e=>e.scrollIntoView({block:'center',behavior:'instant'}));
  await page.screenshot({path:path.join(output,`stopped-${width}.png`)});
  await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();assert.equal(autos().length,1);
  await save.click();await settled();assert.equal(autos().length,2);assert.equal(view.answers['m-word'].level,3);assert.equal(receipts.size,1);
  await radio(2).check();await expect.poll(()=>autos().length).toBe(3);await settled();assert.equal(receipts.size,1);
  report.checks.push({width,case:'fallo detiene selecciones, transición, online/foco; manual actual sin cola y reactiva sólo futuros cambios'});
  // Volver al valor ya confirmado dentro del debounce no produce una petición.
  await radio(1).check();await radio(2).check();await settled();assert.equal(autos().length,3);
  report.checks.push({width,case:'cambios revertidos al valor guardado: cero PUT redundantes'});
  await fresh();await page.route('**/api/encuesta',async route=>{if(route.request().method()!=='PUT')return route.continue();await route.fetch();await route.abort('failed');},{times:1});
  await radio(1).check();await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');await radio(3).check();
  await page.waitForTimeout(1700);assert.equal(autos().length,1);assert.equal(view.answers['m-word'].level,1);
  await save.click();await settled();assert.equal(view.answers['m-word'].level,3);assert.equal(receipts.size,1);assert.equal(autos().length,2);assert.equal(autos()[1].body.answers['m-word'].level,3);
  report.checks.push({width,case:'commit sin confirmación: cero replay; manual consulta y escribe una vez los datos actuales'});
  await fresh();await page.route('**/api/encuesta',async route=>{await route.fetch();await route.abort('failed');},{times:1});
  await radio(1).check();await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');await radio(3).check();await page.waitForTimeout(1700);
  assert.equal(autos().length,1);await page.reload();await expect(radio(1)).toBeChecked();assert.equal(autos().length,1);
  report.checks.push({width,case:'recarga tras confirmación perdida recupera commit real, no lo que sólo estaba en pantalla'});
  await fresh();failure=503;await radio(1).check();await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');
  await radio(3).check();await save.click();await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');assert.equal(autos().length,2);
  await radio(2).check();await page.waitForTimeout(1700);assert.equal(autos().length,2);assert.equal(receipts.size,0);
  failure=0;await page.reload();await expect(radio(2)).not.toBeChecked();assert.equal(autos().length,2);
  report.checks.push({width,case:'manual fallido tampoco reintenta; recarga no recupera cambios sin commit'});
  await fresh();failure=409;await radio(2).check();await save.click();await expect(page.getByRole('alert')).toBeVisible();assert.equal(receipts.size,0);await expect(radio(2)).toBeChecked();
  failure=0;await save.click();await settled();await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();await expect(page.getByRole('alert').first()).toContainText('Completa los campos');await expect(page.getByRole('button',{name:'Microsoft 365',exact:true})).toHaveAttribute('aria-current','step');
  report.checks.push({width,case:'conflicto sin éxito ni evento; validación secuencial conservada'});
  await fresh();concurrent=true;let release,arrived;
  const gate=new Promise(r=>release=r),reached=new Promise(r=>arrived=r);
  await page.route('**/api/encuesta',async route=>{const response=await route.fetch();arrived();await gate;await route.fulfill({response});},{times:1});
  try {await radio(1).check();await reached;await radio(3).check();}finally{release();}
  await expect(page.getByRole('alert')).toContainText('La encuesta cambió en otra sesión');await expect(radio(3)).toBeChecked();
  await page.waitForTimeout(1700); // Un conflicto cancela también el debounce de la edición posterior.
  assert.equal(view.answers['m-word'].level,2);assert.equal(autos().length,1);assert.equal(receipts.size,0);concurrent=false;
  await radio(0).check();await page.waitForTimeout(1700);await save.click();await expect(page.getByRole('alert')).toContainText('La encuesta cambió en otra sesión');assert.equal(autos().length,1);
  report.checks.push({width,case:'otra sesión escribe entre commit y lectura: no usa su versión para sobrescribirla, conserva borrador local'});
  reset();view.navigation.current='ciber';view.navigation.steps[2].accessible=true;
  await page.goto(fixture.origin+base+'/encuesta');const checks=page.locator('.cd-question').first().getByRole('checkbox');
  const id=await page.locator('.cd-question').first().getAttribute('id');delay=1800;
  await checks.nth(0).check();await expect.poll(()=>autos().length).toBe(1);await checks.nth(1).check();await checks.nth(2).check();await checks.nth(0).uncheck();await settled();
  assert.equal(view.answers[id].checks,'011000000000');await expect(checks.nth(0)).not.toBeChecked();await expect(checks.nth(1)).toBeChecked();await expect(checks.nth(2)).toBeChecked();assert.equal(manuals().length,0);assert.equal(maxActive,1);
  report.checks.push({width,case:'rúbrica: varios indicadores y desmarcado durante guardado lento, sin pérdida ni evento manual'});
  await context.close();
 }
 assert.deepEqual(report.errors,[]);report.completed=true;
}finally{await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
