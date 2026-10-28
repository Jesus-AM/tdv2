// React compilado y proyecciones reales del evaluador. Transporte sintético, sin bases.
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';

const output=path.resolve('../.artifacts/instrument-activity-20261028/browser');await mkdir(output,{recursive:true});
const data=JSON.parse(await readFile('../.artifacts/instrument-activity-20261028/edition.json','utf8'));
const base='/competencias-digitales',fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true});
let view=structuredClone(data.oldDraft),writes=[];
const report={syntheticTransport:true,realBackendProjection:true,checks:[],errors:[]};
function payload(url){return {component:'CompetenciasDigitales',url,props:{name:'TDV2',csrfToken:'synthetic',photoContext:'synthetic-photo',photoApplication:'competencias-digitales',
 auth:{user:{id:1,name:view.institution.name,email:'persona@example.test'},roles:[],modules:[],canPreview:false,canRepresent:false},simulacion:null,representacion:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:base,logout:'/logout',connect:'/connect'},
 digital:{generation:view.generation,notice:view.instrument.notice,page:url.endsWith('mi-informe-ruta')?3:2,user:view.institution.name,actor:view.institution.name,readOnly:false,content:structuredClone(view),navigation:[{path:base+'/encuesta',name:'Encuesta'},{path:base+'/mi-informe-ruta',name:'Mi informe y ruta'}]}}};}
fixture.pages=Object.fromEntries(['encuesta','mi-informe-ruta'].map(s=>[base+'/'+s,()=>payload(base+'/'+s)]));
fixture.handleRequest=async(req,url,json)=>{
 if(url.pathname==='/user/photo'){json({photo:null,estado:'sin_foto',contexto:'synthetic-photo'});return true;}
 if(url.pathname===base+'/api/generacion'){json({generation:view.generation});return true;}
 if(url.pathname.startsWith(base+'/api/')&&req.method!=='GET'){
  let raw='';for await(const c of req)raw+=c;const body=JSON.parse(raw||'{}');writes.push({path:url.pathname,body});
  if(url.pathname.endsWith('/actualizar-instrumento')){assert.equal(body.instrumentVersion,data.currentDraft.instrument.version);view=structuredClone(data.currentDraft);}
  else {assert.equal(url.pathname,base+'/api/encuesta','Nunca finalizar ni reiniciar');view={...view,profile:body.profile,answers:body.answers,version:view.version+1};}
  json(view);return true;
 }return false;
};
const retired=['m-bookings','m-viva-connections-y-org-explo','m-viva-learning','m-seguridad-en-microsoft-365'];
async function layout(page){assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);const f=await page.locator('.cd-footer').boundingBox(),m=await page.locator('#cd-main').boundingBox();assert(f.y>=m.y+m.height-1);}
try{
 for(const width of [1440,768,390]){
  const context=await browser.newContext({viewport:{width,height:960},reducedMotion:'reduce'});await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
  view=structuredClone(data.oldDraft);writes=[];await page.goto(fixture.origin+base+'/encuesta');
  await expect(page.getByRole('heading',{name:'El instrumento tiene una nueva edición'})).toBeVisible();await page.getByRole('button',{name:'Perfil',exact:true}).click();await expect(page.locator('[id="perfil:puesto"]')).toBeDisabled();
  await page.screenshot({path:path.join(output,`borrador-anterior-${width}.png`)});assert.equal(writes.length,0);
  await page.getByRole('button',{name:'Confirmar actualización del borrador'}).click();await expect(page.locator('[id="perfil:puesto"]')).toBeEnabled();
  const field=view.instrument.profile.find(f=>f.id==='puesto');await expect(page.locator('[id="perfil:puesto"] option')).toHaveText(['Selecciona una opción',...field.options]);
  await expect(page.locator('[id="perfil:puesto"]')).toHaveValue('');
  await expect(page.locator('[id="perfil:funciones"] input')).toHaveCount(4);await expect(page.locator('[id="perfil:funciones"]')).not.toContainText('Técnica');
  assert.equal(view.answers['m-word'].level,data.oldDraft.answers['m-word'].level);assert(!retired.some(k=>k in view.answers));
  await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(output,`perfil-nuevo-${width}.png`),fullPage:true});
  await page.locator('[id="perfil:puesto"]').selectOption('Personal Académico');await page.getByRole('checkbox',{name:'Otra',exact:true}).check();
  await expect.poll(()=>writes.filter(w=>w.path===base+'/api/encuesta').length).toBeGreaterThan(0);
  assert.deepEqual(view.profile.funciones,['Académica','Otra']);await layout(page);
  view=structuredClone(data.academic);await page.goto(fixture.origin+base+'/encuesta');await page.getByRole('button',{name:'Microsoft 365',exact:true}).click();
  for(const id of retired)await expect(page.locator(`[id="${id}"]`)).toHaveCount(0);
  for(const tool of view.instrument.modules.find(b=>b.id==='m365').tools.filter(t=>t.doc&&!t.opt))await expect(page.locator(`[id="${tool.id}"]`)).toBeVisible();
  await layout(page);await page.screenshot({path:path.join(output,`academico-${width}.png`)});
  await page.goto(fixture.origin+base+'/mi-informe-ruta');await expect(page.locator('.cd-global>strong')).toHaveText('0 %');await expect(page.locator('.cd-report-blocks>.cd-card')).toHaveCount(5);
  let count=0;for(const b of view.instrument.modules){if(width>700)await page.getByRole('tab',{name:b.n,exact:true}).click();else{await page.getByRole('combobox',{name:'Bloque del detalle'}).click();await page.getByRole('option',{name:b.n,exact:true}).click();}const rows=page.locator(width>700?'.cd-report-tools tbody tr':'.cd-tool-card');count+=await rows.count();}
  assert.equal(count,80);await expect(page.getByRole('button',{name:/Descargar mi informe/})).toHaveCount(0);await layout(page);await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(output,`informe-cero-${width}.png`)});
  report.checks.push({width,explicitUpgrade:true,preservedAnswers:true,profiles:11,functions:4,academicTools:true,reportTools:count,zeroValid:true});await context.close();
 }
 assert.deepEqual(report.errors,[]);report.completed=true;
}finally{await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
console.log(`PASS ${report.checks.length} anchos: borrador anterior, edición nueva, perfil, herramientas académicas e informe cero.`);
