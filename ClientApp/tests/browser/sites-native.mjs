// ASP.NET/PostgreSQL reales desechables; Microsoft, Nexo y sitios adicionales sintéticos.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin=process.env.TDV2_BROWSER_ORIGIN,key=process.env.TDV2_BROWSER_KEY,output=process.env.TDV2_BROWSER_ARTIFACTS;
if(!origin?.startsWith('https://127.0.0.1:')||!key||!output)throw Error('Sólo host sintético aislado.');
const base='/competencias-digitales', browser=await chromium.launch({channel:'msedge',headless:true});
const report={syntheticSources:true,realPostgreSql:true,checks:[],errors:[]};
const names=['Procedimientos institucionales','Competencias digitales',...Array.from({length:6},(_,i)=>`Sitio sintético ${i+3}`)];
async function setup(width){
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width,height:900},reducedMotion:'reduce'});
 await context.route('**/*',async route=>{
  const url=new URL(route.request().url());
  if(url.hostname==='login.microsoftonline.com'&&url.pathname.includes('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'))
   return route.fulfill({status:302,headers:{location:`${origin}/connect?code=synthetic-code&state=${encodeURIComponent(url.searchParams.get('state'))}`},body:''});
  if(url.origin!==origin)return route.abort();
  if(route.request().isNavigationRequest()){
   const response=await route.fetch({maxRedirects:0}),headers=response.headers();
   // Playwright puede enrutar sólo la primera URL de una cadena HTTP. Resolver
   // el inicio local sin seguir al proveedor mantiene todo el OAuth sintético.
   let next=headers.location?new URL(headers.location,origin):null;
   if(next?.origin===origin&&next.pathname==='/connect'&&!next.searchParams.has('code')){
    const authorize=await context.request.get(next.href,{maxRedirects:0});
    next=authorize.headers().location?new URL(authorize.headers().location,origin):null;
   }
   if(next?.hostname==='login.microsoftonline.com'&&next.pathname.includes('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'))
    headers.location=`${origin}/connect?code=synthetic-code&state=${encodeURIComponent(next.searchParams.get('state'))}`;
   else if(next&&next.origin!==origin)throw Error('Redirección externa no autorizada por la prueba.');
   return route.fulfill({response,headers});
  }
  return route.continue();
 });
 return context;
}
try{
 for(const width of [1366,390]){
  const context=await setup(width),page=await context.newPage();page.setDefaultTimeout(15000);
  page.on('pageerror',e=>report.errors.push(e.message));
  let rendered=0;
  await page.exposeFunction('selectorRendered',()=>{rendered++;});
  await page.addInitScript(()=>{let seen=false;new MutationObserver(()=>{if(!seen&&[...document.querySelectorAll('h1')].some(h=>h.textContent==='Elige un sitio')){seen=true;window.selectorRendered();}}).observe(document,{subtree:true,childList:true});});
  const control=async name=>assert.equal((await context.request.post(origin+'/__digitalfixture/sites-'+name,{headers:{'X-Fixture-Key':key}})).status(),200);
  const shot=async name=>{await page.evaluate(async()=>{scrollTo(0,0);await document.fonts.ready;});await page.screenshot({path:path.join(output,`${name}-${width}.png`),fullPage:true});};
  const miEspacio=async()=>{await page.goto(origin+'/');await page.getByRole('link',{name:/Ir a mi espacio/}).click();};
  await control('cd');await page.goto(origin+'/accesos');await page.waitForURL(origin+base);
  await expect(page.locator('.cd-site h1')).toBeVisible();assert.equal(rendered,0);
  report.checks.push({width,case:'intención genérica anónima se conserva tras OAuth y entra directamente sin renderizar selector'});
  for(const scenario of ['td','cd','both','none']){
   await control(scenario);await page.goto(origin+'/');const before=rendered;
   await page.getByRole('link',{name:/Ir a mi espacio/}).click();
   if(scenario==='td'||scenario==='cd'){
    await page.waitForURL(origin+(scenario==='td'?'/inicio':base));
    await expect(page.locator('main')).toBeVisible();assert.equal(rendered,before);
    await shot('directo-'+scenario);
   }else{
    await page.waitForURL(origin+'/accesos');await expect(page.getByRole('heading',{name:'Elige un sitio',exact:true})).toBeVisible();
    for(const name of names)await expect(page.getByRole('link',{name,exact:true})).toHaveCount(scenario==='both'&&names.indexOf(name)<2?1:0);
    if(scenario==='none'){await expect(page.getByRole('alert')).toHaveText('No hay sitios disponibles para tu cuenta.');await page.reload();await expect(page.getByRole('button',{name:'Cambiar de cuenta',exact:true})).toBeVisible();}
    await shot('sitios-'+scenario);
   }
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.goBack();await page.waitForURL(origin+'/');
   report.checks.push({width,case:'Mi espacio: '+scenario+'; sólo sitios autorizados y Atrás regresa al portal'});
  }
  await control('many');await miEspacio();await expect(page.getByRole('heading',{name:'Elige un sitio',exact:true})).toBeVisible();
  await expect(page.getByText('Selecciona el sitio que deseas abrir.',{exact:true})).toBeVisible();
  await page.getByRole('link',{name:names[0],exact:true}).focus();
  for(let i=0;i<names.length;i++){if(i)await page.keyboard.press('Tab');await expect(page.getByRole('link',{name:names[i],exact:true})).toBeFocused();}
  assert(await page.locator('main .surface').evaluate(e=>e.scrollHeight<=e.clientHeight+1));
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  assert(await page.evaluate(()=>document.querySelector('footer').getBoundingClientRect().top>=document.querySelector('main').getBoundingClientRect().bottom-1));
  await shot('sitios-ocho');
  await page.getByRole('link',{name:names[1],exact:true}).focus();await page.keyboard.press('Enter');await page.waitForURL(origin+base);
  report.checks.push({width,case:'ocho sitios desde registro del servidor, orden, teclado, filas naturales y pie sin superposición'});
  for(const scenario of ['unavailable','failed']){
   await control(scenario);await miEspacio();await page.waitForURL(origin+'/accesos');
   await expect(page.getByRole('alert')).toHaveText('No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.');
   await expect(page.getByText('No hay sitios disponibles para tu cuenta.',{exact:true})).toHaveCount(0);
   for(const name of names)await expect(page.getByRole('link',{name,exact:true})).toHaveCount(scenario==='unavailable'&&name===names[0]?1:0);
   assert(!/Nexo|roles|ApplicationId/.test(await page.locator('main').innerText()));await shot('sitios-'+scenario);
   report.checks.push({width,case:'falla '+scenario+': muestra sólo comprobados, no interpreta error como ausencia ni redirige'});
  }
  await control('both');await miEspacio();await expect(page.getByRole('link',{name:names[0],exact:true})).toBeVisible();
  await control('cd');await page.reload();await page.waitForURL(origin+base);
  await page.goto(origin+'/inicio');await expect(page.getByRole('alert')).toHaveText('Tu cuenta no tiene acceso a Procedimientos institucionales.');
  await page.goto(origin+'/connect');await page.waitForURL(origin+base);
  report.checks.push({width,case:'revocación reevalúa y deja un sitio; URL específica denegada no cambia destino; /connect genérico directo'});
  const fresh=await setup(width),specific=await fresh.newPage();
  await specific.goto(origin+base+'/mi-informe-ruta');await specific.waitForURL(origin+base+'/mi-informe-ruta');await expect(specific.locator('.cd-site h1')).toBeVisible();
  report.checks.push({width,case:'enlace específico conserva destino tras OAuth aunque haya un único sitio'});
  await fresh.close();await context.close();
 }
 assert.deepEqual(report.errors,[]);report.completed=true;
}catch(e){report.failure={message:e.message};throw e;}
finally{await writeFile(path.join(output,'sites-browser.json'),JSON.stringify(report,null,2));await browser.close();}
