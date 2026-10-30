// React/bundles, fuentes y Edge reales. Identidad, módulos y HTTP sintéticos; sin bases.
import assert from 'node:assert/strict';
import {readFile,mkdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
import {chromium,expect} from '@playwright/test';
import {systemFixture} from './system-ui-fixture.mjs';

const output=path.resolve(process.env.TDV2_VISUAL_ARTIFACTS||'../.artifacts/digital-header-20261030');await mkdir(output,{recursive:true});
const instrument=JSON.parse(await readFile('../tdv2/Contracts/competencias-digitales-v2.json','utf8'));
const fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true}),base='/competencias-digitales';
const all=[['encuesta','Encuesta'],['mi-informe-ruta','Mi informe y ruta'],['avance-dependencia','Avance por dependencia'],['resultados-generales','Resultados generales'],['configuracion','Configuración']].map(([slug,name])=>({path:base+'/'+slug,name}));
const longName='María Alejandra de los Ángeles Hernández Villaseñor de la Universidad Autónoma de Ciudad Juárez';
let scenario={count:5,name:'Persona sintética',represented:false},writes=0;
const generation='11111111-1111-4111-8111-111111111111';
function survey(){return{hasResponses:false,version:0,generation,completedAt:null,accepted:true,instrument,profile:{},answers:{},institution:{status:'available',name:scenario.name,employee:'SYNTHETIC',unit:null,level2:null,level3:null},navigation:{current:'perfil',resume:'perfil',steps:[{id:'perfil',name:'Perfil',complete:false,accessible:true,pending:[]},...instrument.modules.map(b=>({id:b.id,name:b.n,complete:false,accessible:false,pending:[]}))]},evaluation:{answered:0,total:53,progress:0,pending:[],report:{global:null,level:null,blocks:[],tools:[],route:[],recommendations:[],transversal:[]}}};}
function content(index){
 if(index===4)return{section:null,sections:[],generation,units:[],targets:[],catalog:{source:'Fuente sintética',available:false,completedAt:null},nexoRoles:[],nexoModules:[]};
 if(index===0||index===1)return{selectedUnit:null,selectedLevel:2,selectedPosition:null,positionOptions:[],institutional:true,roots:[],options:[],levels:[2,3,4,5],summary:{completed:0,inProgress:0,participatingUnits:0,target:0,configuredTargets:0,totalUnits:0,participation:null,maturity:null,notStarted:0,populationAvailable:true},units:[],blocks:[],tools:[],positions:[],competenceLevels:[],recommendations:[]};
 return survey();
}
fixture.pages=Object.fromEntries(all.map(({path:route})=>[route,()=>{
 const index=['avance-dependencia','resultados-generales','encuesta','mi-informe-ruta','configuracion'].indexOf(route.split('/').at(-1));
 const actor=scenario.represented?'Administradora original sintética':scenario.name;
 return{component:'CompetenciasDigitales',url:route,props:{name:'TDV2',csrfToken:'synthetic',photoContext:scenario.name,photoApplication:'competencias-digitales',contextoEdicion:'own',auth:{user:{id:1,name:actor,email:'persona@example.test'},roles:[],modules:[]},simulacion:null,representacion:scenario.represented?{id:'synthetic-representation',nombre:scenario.name,email:'representada@example.test',escritura:false,application:'competencias-digitales'}:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:'/inicio',logout:'/logout',connect:'/connect'},digital:{generation,page:index,user:scenario.name,actor,readOnly:scenario.represented,notice:instrument.notice,navigation:scenario.navigation||all.slice(0,scenario.count),content:content(index)}}};
}]));
fixture.handleRequest=async(req,url,json)=>{
 if(url.pathname==='/user/photo'){json({photo:null,estado:'sin_foto',contexto:scenario.name});return true;}
 if(url.pathname===base+'/api/generacion'){json({generation});return true;}
 if(req.method!=='GET'){writes++;json({});return true;}
 return false;
};
const report={realReactAndEdge:true,compiledFrontend:true,syntheticIdentityAndPermissions:true,realPostgreSql:false,checks:[],behaviourChecks:[],errors:[]};
async function ready(page){await expect(page.locator('.cd-site')).toBeVisible();await page.evaluate(async()=>{await document.fonts.ready;await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));});}
async function inspect(page){return page.evaluate(()=>{
 const bar=document.querySelector('.cd-header-inner'),logo=bar.querySelector('img'),account=bar.querySelector('.account-trigger'),links=bar.querySelector('.cd-nav-links'),main=document.querySelector('.cd-site main');
 const rect=e=>{const r=e.getBoundingClientRect();return{left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width,height:r.height};};
 return{layout:bar.dataset.layout,bar:rect(bar),logo:rect(logo),account:rect(account),links:rect(links),main:rect(main),nameVisible:getComputedStyle(bar.querySelector('.account-visible-name')).display!=='none',name:bar.querySelector('.account-visible-name').textContent,buttons:[...links.children].map(e=>({text:e.textContent,...rect(e),scroll:e.scrollWidth,client:e.clientWidth})),background:getComputedStyle(document.querySelector('.cd-header')).backgroundColor,navBackground:getComputedStyle(bar.querySelector('.cd-nav')).backgroundColor,font:getComputedStyle(account).fontFamily,overflow:document.documentElement.scrollWidth>innerWidth+1,gutter:parseFloat(getComputedStyle(main).paddingLeft),naturalRatio:logo.naturalWidth/logo.naturalHeight};
 });}
async function exercise({width,count,long=false,zoom=false,represented=false,shot=false}){
 scenario={count,name:long?longName:'Persona sintética',represented};
 const context=await browser.newContext({viewport:{width:zoom?width/2:width,height:zoom?480:960},deviceScaleFactor:zoom?2:1,reducedMotion:'reduce'});
 await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
 const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
 await page.goto(fixture.origin+base+'/encuesta');await ready(page);const view=await inspect(page);
 assert(!view.overflow);assert(view.bar.width<=1401);assert(Math.abs(view.bar.height-72)<2);
 assert.equal(view.background,'rgb(33, 52, 85)');assert.equal(view.navBackground,view.background);assert.match(view.font,/Noto Sans/);
 assert(Math.abs(view.logo.width/view.logo.height-view.naturalRatio)<.01);
 assert(Math.abs(view.logo.left-view.main.left-view.gutter)<2);assert(Math.abs(view.account.right-(view.main.right-view.gutter))<2);
 assert(Math.abs((view.logo.top+view.logo.bottom)/2-(view.account.top+view.account.bottom)/2)<2);
 const expected=all.slice(0,count).map(n=>n.name);
 assert.deepEqual(view.buttons.map(b=>b.text),expected);
 const trigger=page.getByRole('button',{name:'Abrir navegación de Competencias digitales',exact:true});
 if(view.layout==='menu'){
  await expect(trigger).toBeVisible();assert(!view.nameVisible);
  await trigger.focus();await page.keyboard.press('Enter');const menu=page.getByRole('menu',{name:'Abrir navegación de Competencias digitales'});
  await expect(menu.getByRole('menuitem')).toHaveText(expected);await expect(menu.locator('[aria-current="page"]')).toHaveText('Encuesta');
  await page.keyboard.press('Escape');await expect(menu).toHaveCount(0);await expect(trigger).toBeFocused();
  await trigger.click();await page.mouse.click(5,150);await expect(menu).toHaveCount(0);
  await trigger.click();await menu.getByRole('menuitem',{name:'Mi informe y ruta',exact:true}).click();
 }else{
  await expect(trigger).toBeHidden();for(const b of view.buttons){assert(Math.abs(b.top-view.buttons[0].top)<1);assert(b.scroll<=b.client+1);}
  assert(view.logo.right<view.links.left);assert(view.links.right<view.account.left);
  await page.getByRole('navigation',{name:'Módulos de Competencias digitales'}).getByRole('button',{name:'Mi informe y ruta',exact:true}).focus();await page.keyboard.press('Enter');
 }
 await expect(page).toHaveURL(fixture.origin+base+'/mi-informe-ruta');await expect(page.getByRole('heading',{name:'Todavía no tienes un informe',exact:true})).toBeVisible();
 const account=page.getByRole('button',{name:'Abrir menú de usuario · '+scenario.name,exact:true});await account.focus();await page.keyboard.press('Enter');
 const accountMenu=page.locator('#account-menu');await expect(accountMenu).toBeVisible();await expect(accountMenu).toContainText(scenario.name);
 assert(await accountMenu.evaluate(e=>e.scrollWidth<=e.clientWidth+1));
 await page.keyboard.press('Escape');await expect(accountMenu).toHaveCount(0);await expect(account).toBeFocused();
 if(represented){await expect(page.locator('.cd-representation')).toContainText('Administradora original sintética');await expect(page.locator('.cd-representation')).toContainText(scenario.name);await expect(page.locator('.cd-representation').getByRole('button',{name:'Volver a mi usuario'})).toBeVisible();}
 if(shot){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(output,`cabecera-${count}-${width}${zoom?'-zoom200':''}${represented?'-representacion':''}.png`),fullPage:false});if(view.layout==='menu'){await trigger.click();await page.screenshot({path:path.join(output,`menu-${count}-${width}${zoom?'-zoom200':''}${represented?'-representacion':''}.png`),fullPage:false});await page.keyboard.press('Escape');}}
 report.checks.push({width,cssWidth:zoom?width/2:width,zoomEquivalent:zoom?200:100,count,longName:long,represented,...view});
 await context.close();
}
try{
 for(const width of [1920,1440,1280,1100,900,768,390,320])for(const count of [2,5])for(const long of [false,true])await exercise({width,count,long,shot:long&&[1440,1280,900,390].includes(width)});
 for(const count of [2,5])for(const long of [false,true])await exercise({width:1440,count,long,zoom:true,shot:long});
 for(const width of [1440,390])await exercise({width,count:2,long:true,represented:true,shot:true});
 // Cambiar la distribución no debe reiniciar el formulario ni dejar foco en enlaces ocultos.
 scenario={count:5,name:longName,represented:false};
 const context=await browser.newContext({viewport:{width:900,height:960},reducedMotion:'reduce'});
 await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
 const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
 await page.goto(fixture.origin+base+'/encuesta');await ready(page);
 const field=await page.locator('#cd-step-content').elementHandle();
 const trigger=page.getByRole('button',{name:'Abrir navegación de Competencias digitales',exact:true});
 await trigger.click();await page.setViewportSize({width:1440,height:960});
 await expect(page.locator('.cd-header-inner')).toHaveAttribute('data-layout','full');
 await expect(page.locator('#cd-module-menu')).toHaveCount(0);
 await expect(page.locator('.cd-nav [aria-current]')).toBeFocused();
 await page.screenshot({path:path.join(output,'encuesta-5-1440.png')});
 await page.setViewportSize({width:1280,height:960});
 await expect(page.locator('.cd-header-inner')).toHaveAttribute('data-layout','compact-account');
 await expect(page.locator('.cd-account .account-visible-name')).toBeHidden();
 await page.setViewportSize({width:390,height:960});
 await expect(page.locator('.cd-header-inner')).toHaveAttribute('data-layout','menu');await expect(trigger).toBeFocused();
 assert(await field.evaluate(e=>e===document.querySelector('#cd-step-content')));
 await page.screenshot({path:path.join(output,'encuesta-5-390.png')});
 report.behaviourChecks.push('Cambio 900 → 1440 → 1280 → 390: menú/foco correctos y mismo formulario');
 for(const width of [1440,390]){
  await page.setViewportSize({width,height:960});await ready(page);
  await page.locator('.cd-progress').evaluate(e=>scrollTo(0,e.getBoundingClientRect().top+scrollY+200));
  await expect.poll(()=>page.evaluate(()=>Math.abs(document.querySelector('.cd-progress').getBoundingClientRect().top-document.querySelector('.cd-header').getBoundingClientRect().bottom))).toBeLessThan(2);
  report.behaviourChecks.push(`Progreso bajo la altura real de la cabecera: ${width}px`);
 }
 scenario.navigation=[...all].reverse();await page.goto(fixture.origin+base+'/encuesta');await ready(page);
 await trigger.click();await expect(page.locator('#cd-module-menu').getByRole('menuitem')).toHaveText([...all].reverse().map(n=>n.name));
 report.behaviourChecks.push('Orden autorizado alternativo del servidor conservado sin reordenarlo');
 await page.keyboard.press('Escape');await page.locator('.cd-account .account-trigger').click();
 await expect(page.locator('#account-menu')).toContainText(longName);
 await page.screenshot({path:path.join(output,'cuenta-nombre-completo-390.png')});
 await context.close();
 assert.equal(writes,0);assert.deepEqual(report.errors,[]);
}finally{await writeFile(path.join(output,'header-verification.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
