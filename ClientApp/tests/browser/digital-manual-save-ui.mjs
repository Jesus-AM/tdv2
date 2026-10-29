// React real; transporte sintético controlable. Persistencia/cola/recibos se prueban en DigitalManualSaveTests.
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';

const output=path.resolve('../.artifacts/manual-save-20261029/browser');await mkdir(output,{recursive:true});
const instrument=JSON.parse(await readFile('../tdv2/Contracts/competencias-digitales-v2.json','utf8'));
const base='/competencias-digitales', fixture=await systemFixture(), browser=await chromium.launch({channel:'msedge',headless:true});
const fullProfile=Object.fromEntries(instrument.profile.map(f=>[f.id,f.multiple?[f.options[0]]:f.options[0]]));
const steps=[{id:'perfil',name:'Perfil'},...instrument.modules.map(b=>({id:b.id,name:b.n}))];
const unit={id:'A',code:'100',description:'Unidad sintética',level:2};
let view, writes, receipts, rejectPut, rejectManual, release, gate;
const report={syntheticTransport:true,realPostgreSql:false,checks:[],errors:[]};
function reset(){view={version:1,generation:'11111111-1111-4111-8111-111111111111',completedAt:null,accepted:true,instrument,profile:structuredClone(fullProfile),answers:{},
 institution:{status:'available',name:'Persona Sintética',employee:'0001',unit,level2:unit,level3:null},
 navigation:{current:'perfil',resume:'m365',steps:steps.map((s,i)=>({...s,complete:i===0,accessible:i<2,pending:i===0?[]:['m-word']}))},
 evaluation:{answered:6,total:53,progress:11,report:{}}};writes=[];receipts=new Set();rejectPut=false;rejectManual=false;gate=null;release=null;}
function payload(url){return {component:'CompetenciasDigitales',url,props:{name:'TDV2',csrfToken:'synthetic',photoContext:'synthetic',photoApplication:'competencias-digitales',
 auth:{user:{id:1,name:'Persona Sintética',email:'persona@example.test'},roles:[],modules:[]},simulacion:null,representacion:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:base,logout:'/logout',connect:'/connect'},
 digital:{page:2,generation:view.generation,user:'Persona Sintética',actor:'Persona Sintética',readOnly:false,notice:instrument.notice,content:structuredClone(view),navigation:[{path:base+'/encuesta',name:'Encuesta'}]}}};}
fixture.pages={[base+'/encuesta']:()=>payload(base+'/encuesta')};
fixture.handleRequest=async(req,url,json)=>{
 if(url.pathname==='/user/photo'){json({photo:null,estado:'sin_foto',contexto:'synthetic'});return true;}
 if(url.pathname===base+'/api/generacion'){json({generation:view.generation});return true;}
 if(url.pathname===base+'/api/encuesta'&&req.method==='GET'){json(view);return true;}
 if(!url.pathname.startsWith(base+'/api/'))return false;
 let raw='';for await(const c of req)raw+=c;const body=JSON.parse(raw||'{}');writes.push({method:req.method,path:url.pathname,body});
 if(req.method==='PUT'&&url.pathname===base+'/api/encuesta'){
  if(gate)await gate;
  if(rejectPut){json({message:'Fallo de guardado sintético.'},503);return true;}
  assert.equal(body.version,view.version);
  if(JSON.stringify(body.profile)!==JSON.stringify(view.profile)||JSON.stringify(body.answers)!==JSON.stringify(view.answers))view.version++;
  view.profile=body.profile;view.answers=body.answers;json(view);return true;
 }
 assert.equal(url.pathname,base+'/api/encuesta/guardar');assert.equal(req.method,'POST');
 assert.deepEqual(Object.keys(body).sort(),['generation','operation','version']);
 assert.match(body.operation,/^[\da-f-]{36}$/);
 if(rejectManual){json({message:'Conflicto sintético de revisión.'},409);return true;}
 if(receipts.has(body.operation)){json(view);return true;}
 assert.equal(body.version,view.version);assert.equal(body.generation,view.generation);receipts.add(body.operation);json(view);return true;
};
const autos=()=>writes.filter(w=>w.method==='PUT');const manuals=()=>writes.filter(w=>w.method==='POST');
try {
 for(const width of [1440,390]){
  const context=await browser.newContext({viewport:{width,height:960},reducedMotion:'reduce'});
  await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
  const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
  const save=page.getByRole('button',{name:'Guardar avance',exact:true});const antig=page.locator('[id="perfil:antig"]');
  async function fresh(){reset();await page.goto(fixture.origin+base+'/encuesta');await expect(save).toBeEnabled();}
  async function saved(){await expect(save).toBeEnabled();await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado');}

  await fresh();await antig.selectOption(instrument.profile.find(f=>f.id==='antig').options[1]);
  await expect.poll(()=>autos().length).toBe(1);await saved();
  await page.locator('[id="perfil:puesto"]').selectOption('Personal Académico');await expect.poll(()=>autos().length).toBe(2);await saved();
  await page.getByRole('button',{name:'Microsoft 365',exact:true}).click();
  await page.locator('[id="m-word"]').getByRole('radio',{name:'Inicial',exact:true}).check();await expect.poll(()=>autos().length).toBe(3);await saved();
  await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();await expect(page.locator('.cd-feedback')).toContainText('Completa los campos pendientes');
  assert.equal(manuals().length,0);assert.equal(receipts.size,0);report.checks.push({width,case:'perfil/pregunta/transición: sin confirmación manual'});

  const revision=view.version, beforeAuto=autos().length;await save.click();await saved();
  assert.equal(manuals().length,1);assert.equal(receipts.size,1);assert.equal(autos().length,beforeAuto);assert.equal(view.version,revision);
  report.checks.push({width,case:'manual después de autoguardar sin cambios ni revisión artificial'});

  await fresh();gate=new Promise(resolve=>release=resolve);await antig.selectOption(instrument.profile.find(f=>f.id==='antig').options[1]);await save.click();
  await expect.poll(()=>autos().length).toBe(1);assert.equal(manuals().length,0);await expect(save).toBeDisabled();
  release();gate=null;await saved();assert.equal(manuals().length,1);assert.equal(manuals()[0].body.version,view.version);
  report.checks.push({width,case:'botón guarda borrador y espera confirmación de persistencia antes de auditar'});

  await fresh();gate=new Promise(resolve=>release=resolve);await antig.selectOption(instrument.profile.find(f=>f.id==='antig').options[1]);
  await expect.poll(()=>autos().length).toBe(1);await expect(save).toBeEnabled();assert.equal(manuals().length,0);
  await save.click();await expect(save).toBeDisabled();release();gate=null;await saved();assert.equal(manuals().length,1);assert.equal(manuals()[0].body.version,view.version);
  report.checks.push({width,case:'autoguardado en curso coordinado con botón'});

  await fresh();await save.dblclick();await saved();assert.equal(manuals().length,1);assert.equal(receipts.size,1);
  report.checks.push({width,case:'doble clic: una confirmación'});

  await fresh();await page.getByRole('button',{name:'Microsoft 365',exact:true}).click();await page.route('**/api/encuesta/guardar',async route=>{await route.fetch();await route.abort('failed');},{times:1});
  await save.click();await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');assert.equal(receipts.size,1);const id=manuals()[0].body.operation;
  await save.click();await saved();assert.equal(manuals().length,2);assert.equal(manuals()[1].body.operation,id);assert.equal(receipts.size,1);assert.equal(view.version,1);
  report.checks.push({width,case:'respuesta perdida: recuperación explícita, mismo UUID y un recibo'});

  await fresh();rejectPut=true;await antig.selectOption(instrument.profile.find(f=>f.id==='antig').options[1]);await save.click();
  await expect(page.locator('.cd-save-status')).toHaveText('Sin confirmar. Usa Guardar avance.');assert.equal(manuals().length,0);await expect(antig).toHaveValue(instrument.profile.find(f=>f.id==='antig').options[1]);
  rejectPut=false;await save.click();await saved();assert.equal(receipts.size,1);
  report.checks.push({width,case:'fallo de captura: sin evento, respuestas conservadas y reintento'});

  await fresh();rejectManual=true;await save.click();await expect(page.locator('.cd-feedback')).toContainText('Conflicto sintético de revisión.');
  assert.equal(receipts.size,0);await expect(page.locator('.cd-feedback')).not.toContainText('Avance guardado.');const rejectedId=manuals()[0].body.operation;
  rejectManual=false;await save.click();await saved();assert.equal(manuals()[1].body.operation,rejectedId);assert.equal(receipts.size,1);
  report.checks.push({width,case:'conflicto de confirmación: sin éxito ni recibo hasta reintento'});
  // Capturar desde arriba: fullPage conserva la posición de elementos sticky si se toma tras enfocar el botón inferior.
  await page.evaluate(async()=>{scrollTo({top:0,left:0,behavior:'instant'});await document.fonts.ready;await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});
  await expect(page.locator('.cd-feedback')).not.toContainText('Puedes continuar desde otro dispositivo');
  await page.screenshot({path:path.join(output,`guardado-manual-${width}.png`),fullPage:true});await context.close();
 }
 assert.deepEqual(report.errors,[]);report.completed=true;
} finally {release?.();await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
