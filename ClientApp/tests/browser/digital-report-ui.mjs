// React compilado + proyecciones del backend real exportadas por --verify-digital-presentation.
// Transporte/aceptaciones de prueba en memoria. Ninguna base, migración o reinicio.
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';
import { verifySignatures } from './identity-checks.mjs';

const output=path.resolve(process.env.TDV2_VISUAL_ARTIFACTS||'../.artifacts/report-notice-20261027/browser');await mkdir(output,{recursive:true});
const data=JSON.parse(await readFile('../.artifacts/report-notice-20261027/projections.json','utf8'));
const base='/competencias-digitales',slugs=['avance-dependencia','resultados-generales','encuesta','mi-informe-ruta','configuracion'];
const names=['Avance por dependencia','Resultados generales','Encuesta','Mi informe y ruta','Configuración'];
const fixture=await systemFixture(),browser=await chromium.launch({channel:'msedge',headless:true});
const report={database:false,migrations:false,resets:false,backendProjections:true,syntheticTransport:true,checks:[],errors:[]};
let scenario='empty',role='participante',represented=false,accepted=false,writes=[],consents=[];
function survey(){const v=structuredClone(data.views[scenario]);if(scenario==='oldConsent'&&accepted)v.accepted=true;return v;}
function payload(url){const content=survey(),name=content.institution.name;return {component:'CompetenciasDigitales',url,props:{name:'TDV2',csrfToken:'synthetic',photoContext:'synthetic-photo',photoApplication:'competencias-digitales',
    auth:{user:{id:1,name,email:'persona@example.test'},roles:[],modules:[],canPreview:false,canRepresent:false},simulacion:null,representacion:represented?{nombre:name,escritura:false}:null,session:{lifetime_ms:7200000},routes:{home:'/',inicio:base,logout:'/logout',connect:'/connect'},
    digital:{generation:content.generation,notice:content.instrument.notice,page:slugs.indexOf(url.split('/').at(-1)),user:name,actor:name,readOnly:represented,content,navigation:(role==='soloInforme'?[3]:data.menus[role]).map(i=>({path:base+'/'+slugs[i],name:names[i]}))}}};}
fixture.pages=Object.fromEntries(slugs.map(s=>[base+'/'+s,()=>payload(base+'/'+s)]));
fixture.handleRequest=async(req,url,json)=>{
    if(url.pathname==='/user/photo'){json({photo:null,estado:'sin_foto',contexto:'synthetic-photo'});return true;}
    if(url.pathname===base+'/api/generacion'){json({generation:survey().generation});return true;}
    if(url.pathname.startsWith(base+'/api/')&&req.method!=='GET'){
        let raw='';for await(const c of req)raw+=c;const body=JSON.parse(raw||'{}');writes.push({path:url.pathname,body});
        assert.equal(url.pathname,base+'/api/aviso','Esta prueba nunca guarda, finaliza ni reinicia encuestas');
        if(represented||scenario==='final'){json({message:'Sólo lectura'},403);return true;}
        assert.equal(body.version,data.views.empty.instrument.notice.version);assert.equal(body.generation,survey().generation);
        consents.push(body.version);accepted=true;json(survey());return true;
    }return false;
};
const exactNotice='Los datos personales proporcionados serán utilizados únicamente para los fines de registro y estadística dentro la aplicación de Diagnóstico de Madurez Digital de las Competencias Digitales del Personal de la Universidad Autónoma de Ciudad Juárez, con el propósito de identificar las competencias digitales del personal y orientar las acciones de capacitación en el marco del proyecto de Transformación Digital Institucional. Para conocer el tratamiento de sus datos personales, consulte el Aviso de Privacidad de la UACJ en:';
async function layout(page){assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,'Sin desbordamiento horizontal');const f=await page.locator('.cd-footer').boundingBox(),m=await page.locator('#cd-main').boundingBox();assert(f.y>=m.y+m.height-1);assert(f.y+f.height>=await page.evaluate(()=>innerHeight)-1);}
async function notice(page){const dialog=page.getByRole('dialog');await expect(dialog).toBeVisible();await expect(dialog.getByRole('heading')).toHaveText('Aviso de privacidad');await expect(dialog.locator('p').first()).toHaveText(exactNotice);const link=dialog.getByRole('link',{name:'https://www.uacj.mx/transparencia/aviso_privacidad.html',exact:true});await expect(link).toHaveAttribute('href','https://www.uacj.mx/transparencia/aviso_privacidad.html');await expect(link).toHaveAttribute('target','_blank');await expect(link).toHaveAttribute('rel','noopener noreferrer');}
try{
for(const width of [1440,768,390,320]){
 const context=await browser.newContext({viewport:{width,height:900},reducedMotion:'reduce'});await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
 role='participante';represented=false;accepted=false;
 for(const state of ['empty','profile','initialized','zero','na','partial','final']){
    scenario=state;await page.goto(fixture.origin+base+'/mi-informe-ruta');await expect(page.locator('#cd-main')).toBeVisible();await page.evaluate(()=>document.fonts.ready);
    if(['empty','profile','initialized'].includes(state)){await expect(page.getByRole('heading',{name:'Todavía no tienes un informe'})).toBeVisible();await expect(page.getByRole('button',{name:'Ir a la encuesta',exact:true})).toBeVisible();await expect(page.locator('.cd-report-blocks,.cd-gap,table')).toHaveCount(0);}
    else{
      await expect(page.locator('.cd-report')).toBeVisible();await expect(page.locator('.cd-report-blocks>.cd-card')).toHaveCount(5);await expect(page.locator('.cd-phase')).toHaveCount(3);
      await expect(page.getByRole('button',{name:'Continuar encuesta',exact:true})).toHaveCount(state==='final'?0:1);
      if(state==='zero'){
        await expect(page.locator('.cd-global>strong')).toHaveText('0 %');
        const cdp=await context.newCDPSession(page);await cdp.send('DOM.enable');await cdp.send('CSS.enable');const {root}=await cdp.send('DOM.getDocument',{depth:-1});
        const {nodeId}=await cdp.send('DOM.querySelector',{nodeId:root.nodeId,selector:'.cd-level'});const {fonts}=await cdp.send('CSS.getPlatformFontsForNode',{nodeId});
        report.fonts??=[];report.fonts.push({width,fonts,computed:await page.locator('.cd-level').first().evaluate(e=>getComputedStyle(e).font)});await cdp.detach();
        assert(fonts.some(f=>f.isCustomFont&&f.familyName.includes('Noto Sans')),'Noto Sans real en etiquetas del informe');
      }
      if(state==='na'){await expect(page.locator('.cd-global')).toContainText('Sin puntuación aplicable');await expect(page.locator('.cd-global>strong')).toHaveCount(0);}
      if(width>700){await expect(page.getByRole('table')).toHaveCount(1);await expect(page.getByRole('tab')).toHaveCount(5);const table=await page.getByRole('table').boundingBox(),panel=await page.locator('.cd-report-tools').boundingBox();assert(Math.abs(table.width-panel.width)<2);}
      else{await expect(page.getByRole('table')).toHaveCount(0);await expect(page.getByRole('combobox',{name:'Bloque del detalle'})).toBeVisible();await expect(page.locator('.cd-tool-card').first()).toContainText('Tu nivel');await expect(page.locator('.cd-tool-card').first()).toContainText('Esperado');await expect(page.locator('.cd-tool-card').first()).toContainText('Estado');}
      assert.equal(await page.locator('.cd-report-tools').evaluate(e=>e.scrollHeight>e.clientHeight+2),false);
      if(state==='partial'){
        const persistent=await page.locator('.cd-report-summary,.cd-report-recommendations,.cd-report-training').allTextContents();
        for(const b of survey().instrument.modules){if(width>700){const tab=page.getByRole('tab',{name:b.n,exact:true});await tab.focus();await page.keyboard.press('Enter');await expect(tab).toHaveAttribute('aria-selected','true');}else{await page.getByRole('combobox',{name:'Bloque del detalle'}).click();await page.getByRole('option',{name:b.n,exact:true}).click();}
         await expect(page.locator('#cd-detail-block')).toHaveText(b.n);assert.deepEqual(await page.locator('.cd-report-summary,.cd-report-recommendations,.cd-report-training').allTextContents(),persistent);
         const expected=survey().evaluation.report.tools.filter(t=>t.block===b.id);assert.equal(await page.locator(width>700?'.cd-report-tools tbody tr':'.cd-tool-card').count(),expected.length);
        }
        await expect(page.locator('.cd-gap').first()).toHaveCSS('background-color','rgb(240, 189, 16)');
        await page.locator('.cd-report-detail').evaluate(e=>scrollTo(0,e.getBoundingClientRect().top+scrollY-document.querySelector('.cd-header').getBoundingClientRect().height-20));
        await page.screenshot({path:path.join(output,`detalle-${width}.png`)});
      }
    }
    await layout(page);await verifySignatures(page,'.cd-footer',false);
    if(width!==320&&['empty','zero','partial','final'].includes(state)){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(output,`${state}-${width}.png`)});if(state==='partial'&&width===1440)await page.screenshot({path:path.join(output,'informe-completo-1440.png'),fullPage:true});}
    report.checks.push({width,state});
 }
 for(const r of [...Object.keys(data.menus),'soloInforme']){role=r;scenario='empty';await page.goto(fixture.origin+base+'/mi-informe-ruta');await expect(page.locator('.cd-nav button')).toHaveText((r==='soloInforme'?[3]:data.menus[r]).map(i=>names[i]));await expect(page.locator('.cd-nav-group')).toHaveCount(0);if(r==='soloInforme')await expect(page.getByRole('button',{name:'Ir a la encuesta'})).toHaveCount(0);await layout(page);}
 role='administrador';scenario='final';await page.goto(fixture.origin+base+'/mi-informe-ruta');await expect(page.locator('.cd-report')).toBeVisible();const before=writes.length;await page.getByRole('button',{name:'Abrir menú de usuario'}).click();await page.getByRole('menuitem',{name:'Aviso de privacidad'}).click();await notice(page);await expect(page.getByRole('dialog').getByRole('checkbox')).toHaveCount(0);await page.getByRole('button',{name:'Cerrar',exact:true}).click();assert.equal(writes.length,before);
 await page.goto(fixture.origin+base+'/encuesta');await expect(page.getByRole('button',{name:'Ver mi informe',exact:true})).toBeVisible();await expect(page.getByRole('dialog')).toHaveCount(0);
 scenario='oldConsent';role='participante';consents=['uacj-digital-aviso-2026-10-27.1'];accepted=false;await page.goto(fixture.origin+base+'/encuesta');await notice(page);await expect(page.getByRole('dialog').getByRole('checkbox')).not.toBeChecked();await expect(page.getByRole('dialog').getByRole('button',{name:'Continuar',exact:true})).toBeDisabled();
 await page.screenshot({path:path.join(output,`aviso-${width}.png`)});await page.keyboard.press('Escape');assert.deepEqual(consents,['uacj-digital-aviso-2026-10-27.1']);await page.getByRole('button',{name:'Leer y confirmar'}).click();await page.getByRole('dialog').getByRole('checkbox').check();await page.getByRole('dialog').getByRole('button',{name:'Continuar',exact:true}).click();await expect(page.getByRole('dialog')).toHaveCount(0);assert.deepEqual(consents,['uacj-digital-aviso-2026-10-27.1','uacj-digital-aviso-2026-10-27.2']);
 await page.reload();await expect(page.getByRole('dialog')).toHaveCount(0);
 represented=true;accepted=false;await page.reload();await expect(page.getByRole('dialog')).toHaveCount(0);await expect(page.locator('.cd-representation')).toBeVisible();await expect(page.getByRole('button',{name:'Guardar y continuar',exact:true})).toHaveCount(0);const representedBefore=writes.length;await page.getByRole('button',{name:'Abrir menú de usuario'}).click();await page.getByRole('menuitem',{name:'Aviso de privacidad'}).click();await notice(page);await expect(page.getByRole('dialog').getByRole('checkbox')).toHaveCount(0);await page.keyboard.press('Escape');assert.equal(writes.length,representedBefore);
 report.checks.push({width,noticeVersion:true,preservedOldConsentInSyntheticTransport:true,readOnly:true,menuRoles:true});await context.close();
}
assert.deepEqual(report.errors,[]);report.completed=true;
}finally{await writeFile(path.join(output,'report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
console.log(`PASS ${report.checks.length} estados/anchos y flujos de aviso; transporte sintético, proyecciones reales.`);
