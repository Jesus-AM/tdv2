// Transporte/identidades sintéticos; React, estilos, navegación y controles reales.
import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';

const output=path.resolve('../.artifacts/sites-entry-20261028/browser');await mkdir(output,{recursive:true});
const fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true});
const td={name:'Procedimientos institucionales',path:'/inicio'},cd={name:'Competencias digitales',path:'/competencias-digitales'};
let options=[],unavailable=false,deniedPath='/inicio',failure=403,post;
const report={syntheticTransport:true,realPostgreSql:false,checks:[],errors:[]};
const props={name:'Transformación Digital',auth:{user:null,roles:[],modules:[]},csrfToken:'synthetic',simulacion:null,representacion:null,contextoEdicion:'own',session:{lifetime_ms:7200000},routes:{home:'/',inicio:'/inicio',connect:'/connect',logout:'/logout'}};
fixture.pages={'/accesos':()=>({component:'Accesos',url:'/accesos',props:{...props,available:{options,unavailable}}})};
fixture.handleRequest=async(req,url,json)=>{
 if(url.pathname==='/session/csrf'){json({token:'synthetic'});return true;}
 if(url.pathname===deniedPath&&req.headers['x-tdv2-page']){
  const message=failure===503?'No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.':`Tu cuenta no tiene acceso a ${deniedPath.startsWith('/competencias-digitales')?'Competencias digitales':'Procedimientos institucionales'}.`;
  json({message,access:{message,status:failure,options}},failure);return true;
 }
 if(url.pathname==='/session/use-another-account'){
  post={url:url.pathname+url.search,csrf:req.headers['x-csrf-token']};
  json({redirect:'/connect?account=other&returnUrl='+encodeURIComponent(url.searchParams.get('returnUrl'))});return true;
 }
 return false;
};
try{
 for(const width of [1440,390]){
  const context=await browser.newContext({viewport:{width,height:960},reducedMotion:'reduce'});
  await context.route('**/*',r=>{const u=new URL(r.request().url());if(u.origin!==fixture.origin)return r.abort();if(u.pathname==='/connect')return r.fulfill({contentType:'text/html',body:'Cuenta sintética'});return r.continue();});
  const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
  for(const available of [[],[td,cd],[td,cd,...Array.from({length:6},(_,i)=>({name:`Sitio sintético ${i+3}`,path:`/sitio-sintetico/${i+3}`}))]]){
   options=available;unavailable=false;await page.goto(fixture.origin+'/accesos');
   await expect(page.getByRole('heading',{name:'Elige un sitio',exact:true})).toBeVisible();
   if(!available.length)await expect(page.getByRole('alert')).toHaveText('No hay sitios disponibles para tu cuenta.');
   else await expect(page.getByText('Selecciona el sitio que deseas abrir.',{exact:true})).toBeVisible();
   for(const site of available)await expect(page.getByRole('link',{name:site.name,exact:true})).toBeVisible();
   for(const app of [td,cd])await expect(page.getByRole('link',{name:app.name,exact:true})).toHaveCount(available.includes(app)?1:0);
   await expect(page.getByRole('link',{name:'Volver al portal',exact:true})).toHaveAttribute('href','/');
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   assert(!/Nexo|roles|aplicacion_id|ApplicationId/.test(await page.locator('main').innerText()));
   await page.screenshot({path:path.join(output,`choices-${available.length}-${width}.png`),fullPage:true});
   report.checks.push({width,case:'elección',options:available.map(a=>a.name)});
  }
  for(const app of [td,cd]){
   deniedPath=app.path;options=[app===td?cd:td];failure=403;await page.goto(fixture.origin+deniedPath);
   await expect(page.getByRole('heading',{name:'Acceso no disponible',exact:true})).toBeVisible();
   await expect(page.getByRole('alert')).toHaveText(`Tu cuenta no tiene acceso a ${app.name}.`);
   await expect(page.getByRole('link',{name:options[0].name,exact:true})).toHaveAttribute('href',options[0].path);
   await expect(page.getByRole('link',{name:app.name,exact:true})).toHaveCount(0);
   const button=page.getByRole('button',{name:'Cambiar de cuenta',exact:true});await button.focus();await expect(button).toBeFocused();
   await page.screenshot({path:path.join(output,`denied-${app===td?'td':'cd'}-${width}.png`),fullPage:true});
   await page.keyboard.press('Enter');await page.waitForURL('**/connect?**');
   assert.equal(new URL(page.url()).searchParams.get('returnUrl'),deniedPath);assert.equal(post.csrf,'synthetic');
   report.checks.push({width,case:'denegación específica y cambio de cuenta por teclado con CSRF/retorno',application:app.name});
  }
  failure=503;options=[td];deniedPath=cd.path;await page.goto(fixture.origin+deniedPath);await expect(page.getByRole('alert')).toHaveText('No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.');
  await expect(page.getByRole('button',{name:'Reintentar acceso'})).toBeVisible();report.checks.push({width,case:'error temporal diferenciado, conserva acceso comprobado a la otra aplicación'});
  unavailable=true;await page.goto(fixture.origin+'/accesos');await expect(page.getByRole('alert')).toHaveText('No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.');await expect(page.getByRole('link',{name:td.name,exact:true})).toBeVisible();
  report.checks.push({width,case:'lista parcial explícita sin inferir permiso cuando falla comprobación'});
  await context.close();
 }
 assert.deepEqual(report.errors,[]);report.completed=true;
}finally{await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
