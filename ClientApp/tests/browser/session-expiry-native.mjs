import assert from 'node:assert/strict';
import {chromium,expect} from '@playwright/test';
import {writeFile} from 'node:fs/promises';
import path from 'node:path';
const origin=process.env.TDV2_BROWSER_ORIGIN,key=process.env.TDV2_BROWSER_KEY,output=process.env.TDV2_BROWSER_ARTIFACTS;
if(!origin?.startsWith('https://127.0.0.1:')||!key||!output)throw new Error('Sólo host sintético aislado.');
const browser=await chromium.launch({channel:'msedge',headless:true});
const report={syntheticMicrosoftAndNexo:true,realAspNet:true,realPostgreSql:true,controlledClock:true,checks:[],errors:[]};
try{
 for(const width of [1440,900,390]) for(const site of ['procedimientos','competencias']){
  const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width,height:960},reducedMotion:'reduce'});
  let logins=0,writes=0,expired=false;
  await context.route('**/*',async route=>{
   const url=new URL(route.request().url());if(url.origin!==origin)return route.abort();
   if(url.pathname==='/connect'){
    logins++;const response=await route.fetch({maxRedirects:0}),headers=response.headers();
    if(headers.location?.startsWith('https://login.microsoftonline.com/'))headers.location=`${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
    return route.fulfill({response,headers});
   }
   if(expired&&['POST','PUT','DELETE'].includes(route.request().method())&&!url.pathname.startsWith('/session/'))writes++;
   return route.continue();
  });
  const control=async minutes=>{const r=await context.request.post(origin+'/__fixture/photo-control?minutes='+minutes,{headers:{'X-Fixture-Key':key}});assert.equal(r.status(),200);};
  await control(0);
  const destination=site==='procedimientos'?'/inicio':'/competencias-digitales/encuesta';
  const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));await page.clock.install();
  await page.goto(origin+'/connect?returnUrl='+encodeURIComponent(destination));await expect(page).toHaveURL(origin+destination);
  await expect(page.locator('header').first()).toBeVisible();
  if(site==='competencias'){
   const dialog=page.getByRole('dialog');if(await dialog.count()){
    await dialog.getByRole('checkbox').check();await dialog.getByRole('button',{name:'Continuar',exact:true}).click();await expect(dialog).toHaveCount(0);
   }
  }
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
  await page.screenshot({path:path.join(output,`sesion-${site}-antes-${width}.png`),fullPage:true});
  const tab=await context.newPage();tab.on('pageerror',e=>report.errors.push(e.message));await tab.clock.install();
  await tab.goto(origin+destination);await expect(tab.locator('header').first()).toBeVisible();
  await page.bringToFront();await page.goto(origin+destination+'?historial=1');await expect(page.locator('header').first()).toBeVisible();await page.clock.pauseAt(await page.evaluate(()=>Date.now()+1000));
  if(site==='competencias')await page.locator('[id="perfil:antig"]').selectOption({index:1});
  const before=logins;expired=true;await control(120);
  await page.clock.fastForward(7200000);
  await expect(page).toHaveURL(origin+'/',{timeout:20000});await expect(tab).toHaveURL(origin+'/',{timeout:20000});
  assert.equal(logins,before);assert.equal(writes,0);
  await expect(page.locator('body')).not.toContainText('Persona sintética');
  await page.screenshot({path:path.join(output,`sesion-${site}-portal-${width}.png`),fullPage:true});
  await page.goBack();await expect(page).toHaveURL(origin+'/');assert.equal(logins,before);
  expired=false;
  await page.clock.resume();
  await page.goto(origin+'/connect?returnUrl='+encodeURIComponent(destination));await expect(page).toHaveURL(origin+destination);
  if(site==='competencias'){
   const state=await(await context.request.get(origin+'/competencias-digitales/api/encuesta')).json();
   assert.equal(state.profile.antig||'','');
   await page.goto(origin+'/competencias-digitales/avance-dependencia');
   await expect(page.getByText('Vista institucional',{exact:true})).toHaveCount(0);
   await expect(page.getByText(/Agrupación:/)).toHaveCount(0);
   await expect(page.getByText('Porcentaje de avance',{exact:true}).first()).toBeVisible();
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   await page.screenshot({path:path.join(output,`sesion-avance-${width}.png`),fullPage:true});
   await page.goto(origin+'/competencias-digitales/resultados-generales');
   await expect(page.getByText(/Resultados definitivos con la adscripción/)).toHaveCount(0);
   await expect(page.getByText(/Agrupación:/)).toHaveCount(0);
  }
  report.checks.push({site,width,portal:true,twoTabs:true,noAutomaticOAuth:true,noPendingWrites:true,history:true,newExplicitLogin:true});
  await context.close();
 }
 assert.deepEqual(report.errors,[]);
}finally{await writeFile(path.join(output,'session-expiry-ui.json'),JSON.stringify(report,null,2));await browser.close();}
