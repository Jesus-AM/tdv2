import assert from 'node:assert/strict';
import {chromium,expect} from '@playwright/test';
import {writeFile} from 'node:fs/promises';
import path from 'node:path';
const origin=process.env.TDV2_BROWSER_ORIGIN,key=process.env.TDV2_BROWSER_KEY,output=process.env.TDV2_BROWSER_ARTIFACTS;
if(!origin?.startsWith('https://127.0.0.1:')||!key||!output)throw Error('Sólo host sintético aislado.');
const reproduce=process.env.TDV2_TEST_ENTRY_REPRO==='1';
const browser=await chromium.launch({channel:'msedge',headless:true});
const report={reproduce,realAspNet:true,realPostgreSql:true,publishedAssets:true,microsoftAndNexo:'synthetic',checks:[],errors:[]};
let loginTimes=[];
// Conserva el limitador real del servidor; sólo espacia OAuth del fixture.
async function throttle(){
 loginTimes=loginTimes.filter(t=>Date.now()-t<61000);
 while(loginTimes.length>=16){await new Promise(r=>setTimeout(r,Math.min(30000,61000-(Date.now()-loginTimes[0]))));loginTimes=loginTimes.filter(t=>Date.now()-t<61000);}
 // Reserva también el callback, que puede llegar en la misma cadena de redirects.
 loginTimes.push(Date.now(),Date.now());
}
async function control(context,url){const response=await context.request.post(origin+url,{headers:{'X-Fixture-Key':key}});assert.equal(response.status(),200);}
async function microsoftPeer(route,onAuthorize){
 const url=new URL(route.request().url());if(url.origin!==origin)return route.abort();
 if(!route.request().isNavigationRequest())return route.continue();
 let target=url;
 // Playwright no vuelve a aplicar route a cada salto de una redirección.
 // Sigue sólo el salto local de inicio OAuth, pasando por el controlador real.
 for(let hops=0;hops<3;hops++){
  if(target.pathname==='/connect')await throttle();
  const response=await route.fetch({url:target.href,maxRedirects:0}),headers=response.headers();
  if(headers.location?.startsWith('https://login.microsoftonline.com/')){
   onAuthorize();headers.location=`${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
  }else if(headers.location&&new URL(headers.location,origin).origin===origin&&new URL(headers.location,origin).pathname==='/connect'){
   target=new URL(headers.location,origin);continue;
  }
  return route.fulfill({response,headers});
 }throw Error('Demasiados saltos OAuth locales en el fixture.');
}
try{
 for(const width of reproduce?[1440]:[1440,900,390])for(const site of ['procedimientos','competencias']){
  const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width,height:960},reducedMotion:'reduce'});
  let oauth=0;
  await context.route('**/*',route=>microsoftPeer(route,()=>oauth++));
  await control(context,'/__fixture/photo-control?minutes=0');
  await control(context,'/__digitalfixture/sites-both');
  const destination=site==='procedimientos'?'/inicio':'/competencias-digitales';
  const label=site==='procedimientos'?'Registrar un procedimiento institucional':'Responder encuesta de competencias digitales';
  const page=await context.newPage();page.setDefaultTimeout(20000);page.setDefaultNavigationTimeout(90000);await page.clock.install();
  page.on('pageerror',e=>report.errors.push(e.message));
  const timing=async()=>{const response=await context.request.get(origin+'/session/status');assert.equal(response.status(),200);return response.json();};
  const enter=async(state,newLogin,blockedBefore=false)=>{
   const before=oauth,button=page.getByRole('link',{name:label});const href=await button.getAttribute('href');
   const first=new URL(href,origin).pathname;
   const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname===first&&r.request().isNavigationRequest(),{timeout:90000}),button.click({timeout:90000})]);
   if(reproduce&&blockedBefore){
    await expect(page).toHaveURL(origin+'/');assert.equal(oauth,before);
    assert.equal(response.status(),302);
    report.checks.push({site,width,state,blocked:true,firstPath:first,status:response.status(),noOAuth:true});
    // Recuperación de la prueba anterior: no se presenta como éxito del botón.
    await page.goto(origin+'/connect?returnUrl='+encodeURIComponent(destination));
   }
   await expect(page).toHaveURL(origin+destination);
   if(site==='procedimientos')await expect(page.getByRole('heading',{name:'Procedimientos Institucionales',exact:true})).toBeVisible();
   else await expect(page.locator('.cd-welcome')).toBeVisible();
   assert.equal(oauth,before+(newLogin?1:0));
   const status=await timing();assert.equal(status.expires_at-status.started_at,7200000);
   if(!reproduce||!blockedBefore)report.checks.push({site,width,state,entered:true,newLogin,firstPath:first,status:response.status()});
   console.log(JSON.stringify({synthetic:true,site,width,state,blocked:reproduce&&blockedBefore}));
   return status;
  };
  await page.goto(origin+'/');let session=await enter('sin sesión',true);
  await page.goto(origin+'/');const valid=await enter('sesión válida',false);assert.equal(valid.id,session.id);assert.equal(valid.expires_at,session.expires_at);
  await control(context,'/__fixture/session-legacy');await page.goto(origin+'/');
  assert((await context.cookies()).some(c=>c.name==='tdv2.session-ended'));
  session=await enter('ticket antiguo',true,true);
  await page.goto(origin+'/');await control(context,'/__fixture/session-advance');
  const renewed=await enter('cookie vencida desde portada abierta',true,true);assert.notEqual(renewed.id,session.id);session=renewed;
  await page.goto(origin+destination+'?historial=1');await expect(page.locator('header').first()).toBeVisible();
  await page.clock.pauseAt(await page.evaluate(()=>Date.now()+1000));const beforeExpiry=oauth;
  await control(context,'/__fixture/session-advance');await page.clock.fastForward(7200000);
  await expect(page).toHaveURL(origin+'/');assert.equal(oauth,beforeExpiry);
  await page.goBack();await expect(page).toHaveURL(origin+'/');assert.equal(oauth,beforeExpiry);
  await page.clock.resume();const fresh=await enter('clic voluntario después de vencer',true,true);
  assert.notEqual(fresh.id,session.id);assert(fresh.started_at>=session.expires_at);
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  if(!reproduce)await page.screenshot({path:path.join(output,`acceso-publico-${site}-${width}.png`),fullPage:true});
  await context.close();
 }
 if(!reproduce){
  // Error de generación EF simulado: 503 no es 401 y no bloquea el otro sitio.
  const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:960}});let oauth=0;
  await context.route('**/*',route=>microsoftPeer(route,()=>oauth++));
  await control(context,'/__fixture/photo-control?minutes=0');await control(context,'/__digitalfixture/sites-both');
  const page=await context.newPage();page.setDefaultNavigationTimeout(90000);page.setDefaultTimeout(90000);page.on('pageerror',e=>report.errors.push(e.message));
  await page.goto(origin+'/');await page.getByRole('link',{name:'Responder encuesta de competencias digitales'}).click();await expect(page).toHaveURL(origin+'/competencias-digitales');
  await expect(page.locator('.cd-site')).toBeVisible();const before=oauth;
  await control(context,'/__digitalfixture/generation-failure?enabled=true');
  const failed=page.waitForResponse(r=>r.url()===origin+'/competencias-digitales/api/generacion');
  await page.evaluate(()=>window.dispatchEvent(new Event('focus')));const failure=await failed;assert.equal(failure.status(),503);
  await expect(page).toHaveURL(origin+'/competencias-digitales');assert.equal((await context.request.get(origin+'/session/status')).status(),200);
  await page.goto(origin+'/');await page.getByRole('link',{name:'Registrar un procedimiento institucional'}).click();await expect(page).toHaveURL(origin+'/inicio');await expect(page.getByRole('heading',{name:'Procedimientos Institucionales',exact:true})).toBeVisible();assert.equal(oauth,before);
  report.checks.push({case:'Generation InvalidOperationException simulada con NpgsqlException/TimeoutException: 503, sesión vigente y acceso público a Procedimientos independiente',passed:true});
  await control(context,'/__digitalfixture/generation-failure?enabled=false');
  for(const scenario of ['td','cd']){
   await control(context,'/__digitalfixture/sites-'+scenario);
   for(const site of ['procedimientos','competencias']){
    const destination=site==='procedimientos'?'/inicio':'/competencias-digitales',label=site==='procedimientos'?'Registrar un procedimiento institucional':'Responder encuesta de competencias digitales';
    await page.goto(origin+'/');await page.getByRole('link',{name:label}).click();await expect(page).toHaveURL(origin+destination);
    if((scenario==='td')===(site==='procedimientos'))await expect(site==='procedimientos'?page.getByRole('heading',{name:'Procedimientos Institucionales',exact:true}):page.locator('.cd-welcome')).toBeVisible();
    else await expect(page.getByRole('alert')).toHaveText(`Tu cuenta no tiene acceso a ${site==='procedimientos'?'Procedimientos institucionales':'Competencias digitales'}.`);
    assert.equal((await context.request.get(origin+'/session/status')).status(),200);
    report.checks.push({case:'permiso independiente '+scenario,site,passed:true});
   }
  }
  await control(context,'/__digitalfixture/sites-both');await page.goto(origin+'/');
  await expect(page.getByRole('link',{name:/Ir a mi espacio/})).toBeVisible();
  await control(context,'/__fixture/session-legacy');
  await page.getByRole('link',{name:/Ir a mi espacio/}).click();await expect(page).toHaveURL(origin+'/accesos');
  await expect(page.getByRole('heading',{name:'Elige un sitio',exact:true})).toBeVisible();
  await expect(page.getByRole('link',{name:'Procedimientos institucionales',exact:true})).toBeVisible();
  await expect(page.getByRole('link',{name:'Competencias digitales',exact:true})).toBeVisible();
  report.checks.push({case:'Mi espacio también admite nuevo acceso voluntario tras ticket antiguo',passed:true});
  await context.close();
 }
 assert.deepEqual(report.errors,[]);
}finally{await writeFile(path.join(output,'public-entry-ui.json'),JSON.stringify(report,null,2));await browser.close();}
