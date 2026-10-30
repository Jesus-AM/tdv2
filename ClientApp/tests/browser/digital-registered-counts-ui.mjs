// Presentación React/Edge real; población y HTTP sintéticos, sin conexiones institucionales.
import assert from 'node:assert/strict';
import {readFile,mkdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium,expect} from '@playwright/test';
import {systemFixture} from './system-ui-fixture.mjs';
const output=path.resolve('../.artifacts/digital-canonical-population-20261030');await mkdir(output,{recursive:true});
const instrument=JSON.parse(await readFile('../tdv2/Contracts/competencias-digitales-v2.json','utf8'));
const base='/competencias-digitales',url=base+'/avance-dependencia',generation='11111111-1111-4111-8111-111111111111';
const fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true});
const summary={completed:null,inProgress:0,participatingUnits:null,target:80,configuredTargets:0,totalUnits:1,participation:null,maturity:null,notStarted:null,populationAvailable:true,populationProblem:'correspondence',registered:{stored:9,completed:8,inProgress:0,withoutCapture:1}};
const unit={id:'A',code:'100',description:'Dependencia sintética',level:2,parent:null,levelKnown:true};
const content={selectedUnit:null,selectedLevel:2,selectedPosition:null,positionOptions:[],institutional:true,roots:[unit],options:[unit],levels:[2,3,4,5],summary,units:[{id:'A',code:'100',name:unit.description,level:2,parent:null,parentName:null,directCompleted:0,directInProgress:0,summary}],blocks:[],tools:[],positions:[],competenceLevels:[0,0,0,0],recommendations:[]};
fixture.pages={[url]:()=>({component:'CompetenciasDigitales',url,props:{name:'TDV2',csrfToken:'synthetic',photoContext:'synthetic',photoApplication:'competencias-digitales',auth:{user:{id:1,name:'Persona Sintética',email:'persona@example.test'},roles:[],modules:[]},simulacion:null,representacion:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:base,logout:'/logout',connect:'/connect'},digital:{page:0,generation,user:'Persona Sintética',actor:'Persona Sintética',readOnly:false,notice:instrument.notice,content,navigation:[{path:base+'/encuesta',name:'Encuesta'},{path:base+'/mi-informe-ruta',name:'Mi informe y ruta'},{path:url,name:'Avance por dependencia'}]}}})};
fixture.handleRequest=async(req,request,json)=>{if(request.pathname===base+'/api/generacion'){json({generation});return true;}return false;};
const report={syntheticTransport:true,realPostgreSql:false,checks:[],errors:[]};
try{
 for(const width of [1440,900,390]) for(const mode of ['pending','resolved']){
  Object.assign(summary,mode==='pending' ? {completed:null,inProgress:0,target:80,notStarted:null,participation:null,populationProblem:'correspondence',registered:{stored:9,completed:8,inProgress:0,withoutCapture:1}} : {completed:8,inProgress:0,target:80,notStarted:72,participation:10,populationProblem:null,registered:null});
  const context=await browser.newContext({viewport:{width,height:960},reducedMotion:'reduce'});
  await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
  const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
  await page.goto(fixture.origin+url);
  const cards=page.locator('.cd-kpis');
  await expect(cards.locator('section').filter({hasText:mode==='pending'?/^Finalizadas registradas/:/^Encuestas finalizadas/}).locator('strong')).toHaveText('8');
  await expect(cards.locator('section').filter({hasText:'Encuestas en curso'}).locator('strong')).toHaveText('0');
  await expect(cards.locator('section').filter({hasText:'Personas que deben responder'}).locator('strong')).toHaveText('80');
  await expect(cards.locator('section').filter({hasText:'Porcentaje de avance'}).locator('strong')).toHaveText(mode==='pending'?'No disponible':'10 %');
  await expect(cards.locator('section').filter({hasText:'Personas sin iniciar'}).locator('strong')).toHaveText(mode==='pending'?'No disponible':'72');
  await expect(page.getByRole('heading',{name:'Encuestas registradas',exact:true})).toHaveCount(mode==='pending'?1:0);
  await expect(page.locator('td[data-label="Finalizadas"]')).toContainText('8');
  if(mode==='pending') await expect(page.locator('td[data-label="Finalizadas"]')).toContainText('Registradas');
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  const colors=await page.locator('.cd-nav').evaluate(e=>getComputedStyle(e).backgroundColor);assert.equal(colors,'rgb(33, 52, 85)');
  await page.screenshot({path:path.join(output,`${mode}-${width}.png`),fullPage:true});
  report.checks.push({width,mode,canonicalUsers:80,completed:mode==='pending'?null:8,inProgress:0,notStarted:mode==='pending'?null:72,percentage:mode==='pending'?null:10});
  await context.close();
 }
 assert.deepEqual(report.errors,[]);
}finally{await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
