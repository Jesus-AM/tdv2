// Presentación React real con proyecciones sintéticas. No abre PostgreSQL, Nexo ni ejecuta migraciones/reinicios.
import assert from 'node:assert/strict';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';
import { verifySignatures } from './identity-checks.mjs';
import { verifyFooterFlow } from './page-flow.mjs';
import { renderedContrast } from './rendered-contrast.mjs';

const output = path.resolve(process.env.TDV2_VISUAL_ARTIFACTS || '../.artifacts/identity-20261027'); await mkdir(output, { recursive: true });
const instrument = JSON.parse(await readFile('../tdv2/Contracts/competencias-digitales-v2.json', 'utf8'));
const base = '/competencias-digitales', fixture = await systemFixture();
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const reproduce = process.env.TDV2_STYLE_REPRO === '1';
const report = { vite: process.env.TDV2_TEST_VITE === '1', reproduce, syntheticTransport: true, realPostgreSql: false, migrations: false, resets: false, checks: [], fonts: [], errors: [], surfaces: [], resources: [] };
const unit = { id: 'synthetic-2', code: '0002', description: 'Dependencia sintética de Servicios Universitarios e Información Institucional', level: 2, parent: null, levelKnown: true };
const area = { ...unit, id: 'synthetic-3', code: '0003', description: 'Coordinación sintética de Atención y Seguimiento de Solicitudes', level: 3, parent: unit.id };
const profile = Object.fromEntries(instrument.profile.map(p => [p.id, p.multiple ? [p.options[0]] : p.options[0]]));
let scenario = 'profile', missingPhoto = false, represented = false, failSave = false;
let writes = [];
let accessCase = 'participante';
const menus = {
    participante: [2,3], responsable: [2,3,0,1], supervisor: [2,3,0,1], administrador: [2,3,0,1,4], soloInforme: [3], soloReporte: [1],
};
const slugs=['avance-dependencia','resultados-generales','encuesta','mi-informe-ruta','configuracion'];
const names=['Avance por dependencia','Resultados generales','Encuesta','Mi informe y ruta','Configuración'];
const summary={completed:0,inProgress:1,participatingUnits:1,target:null,configuredTargets:0,totalUnits:2,participation:null,maturity:null};
function aggregates() { return {selectedUnit:unit.id,selectedLevel:null,selectedPosition:null,positionOptions:['Personal operativo'],institutional:accessCase!=='responsable',roots:[unit],options:[unit,area],levels:[2,3],summary,
    units:[unit,area].map(u=>({id:u.id,code:u.code,name:u.description,level:u.level,parent:u.parent,parentName:u.parent?unit.description:null,directCompleted:0,directInProgress:1,summary})),
    blocks:[],tools:[],positions:[],competenceLevels:[0,0,0,0],recommendations:[]}; }
const steps = [{ id: 'perfil', name: 'Perfil' }, ...instrument.modules.map(b => ({ id: b.id, name: b.n }))];
function survey() {
    const done = scenario === 'report', block = scenario === 'questions';
    const tools = instrument.modules.flatMap(b => b.tools.slice(0, 3).map((t, i) => ({ id: t.id, name: t.n, block: b.id, value: i, expected: 2, gap: Math.max(0, 2-i) })));
    return { hasResponses: true, generation: 'synthetic-generation', version: 1, completedAt: done ? '2026-10-27T12:00:00Z' : null,
        navigation: { current: block ? 'm365' : 'perfil', resume: block ? 'm365' : 'perfil', steps: steps.map((s, i) => ({ ...s, complete: done || block && i===0, accessible: done || i===0 || block && i===1, pending: i===0 && !block && !done ? ['perfil:antig'] : [] })) },
        institution: { status: 'available', name: represented ? 'Persona Representada Sintética' : 'Persona Sintética de Verificación', employee: '000-synthetic', unit: area, level2: unit, level3: area, ancestors: [unit, area], origin: 'Fuente sintética de prueba' },
        profile: { ...profile, antig: block || done ? profile.antig : '' }, answers: { 'm-word': { state: 'level', level: 0 } }, instrument, accepted: true,
        evaluation: { answered: 1, total: 50, progress: 2, pending: ['perfil:antig'], report: { global: 33.3, level: 1,
            blocks: instrument.modules.map(b => ({ id: b.id, name: b.n, score: 33.3, level: 1 })), tools,
            route: tools.filter(t=>t.gap>0).slice(0,3).map((t,i)=>({ tool:t.id,name:t.name,block:t.block,phase:i+1,from:t.value,to:t.expected,why:'Recomendación sintética para revisar la presentación.',courses:['Curso de prueba'],missing:[],license:null })),
            transversal: [], recommendations: [{kind:'warning',title:'Oportunidades de mejora',text:'Contenido sintético para comprobar contraste, etiquetas y distribución.'}] } } };
}
function payload(url) {
    const page = slugs.indexOf(url.split('/').at(-1));
    const content = survey(), name = 'Persona Sintética de Verificación';
    return { component: 'CompetenciasDigitales', url, props: { name: 'TDV2', csrfToken: 'synthetic', photoContext: represented ? 'represented-photo' : 'own-photo', photoApplication: 'competencias-digitales',
        auth: { user: { id:1,name,email:'persona@example.test' },roles:[],modules:[],canPreview:false,canRepresent:false },
        simulacion:null, representacion:represented ? { nombre:content.institution.name,escritura:false } : null, session:{lifetime_ms:7200000},
        routes:{home:'/',inicio:'/inicio',logout:'/logout',connect:'/connect'},
        digital:{ generation:content.generation,notice:instrument.notice,page,user:content.institution.name,actor:name,readOnly:represented,content:page===0||page===1?aggregates():content,
            navigation:menus[accessCase].map(i=>({path:base+'/'+slugs[i],name:names[i]})) } } };
}
fixture.pages = Object.fromEntries([base,...slugs.map(s=>base+'/'+s)].map(url=>[url,()=>payload(url)]));
fixture.handleRequest = async (req,url,json) => {
    if(url.pathname==='/user/photo') { const now=new Date(Date.now()+600000).toISOString(); json({photo:missingPhoto||represented?null:'data:image/svg+xml,'+encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="64" height="64" fill="#213455"/><text x="10" y="43" fill="white" font-size="30">PS</text></svg>'),estado:'actual',contexto:represented?'represented-photo':'own-photo',renuevaEn:now,vigenteHasta:now});return true; }
    if(url.pathname===base+'/api/generacion'){json({generation:'synthetic-generation'});return true;}
    if(url.pathname.startsWith(base+'/api/')&&req.method!=='GET') {
        let raw='';for await(const c of req)raw+=c;writes.push({path:url.pathname,body:JSON.parse(raw||'{}')});
        assert.equal(url.pathname,base+'/api/encuesta','La prueba visual no acepta avisos, finaliza ni reinicia');
        json(failSave?{message:'Fallo de guardado sintético. Inténtalo nuevamente.'}:survey(),failSave?503:200);return true;
    }
    return false;
};
async function actualFont(page, selector, label) {
    await page.evaluate(()=>document.fonts.ready); const cdp=await page.context().newCDPSession(page);
    await cdp.send('DOM.enable');await cdp.send('CSS.enable');const {root}=await cdp.send('DOM.getDocument',{depth:-1});
    const {nodeId}=await cdp.send('DOM.querySelector',{nodeId:root.nodeId,selector});
    const {fonts}=await cdp.send('CSS.getPlatformFontsForNode',{nodeId});assert(fonts.some(f=>f.isCustomFont&&f.familyName.includes('Noto Sans')),JSON.stringify(fonts));
    report.fonts.push({label,fonts});await cdp.detach();
}
try {
    for(const width of reproduce ? [1366,390] : process.env.TDV2_NAVIGATION_ONLY === '1' ? [] : [1920,1440,1366,1024,768,390,320]){
        scenario='profile';missingPhoto=false;represented=false;failSave=false;
        const context=await browser.newContext({viewport:{width,height:width<500?844:900},reducedMotion:'reduce'});
        await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
        const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
        page.on('response', response => { if (/\/(css|fonts)\/|firma-1\.png|transformacion-digital-blanco\.svg/.test(response.url())) report.resources.push({url:new URL(response.url()).pathname,status:response.status(),type:response.headers()['content-type']}); });
        page.setDefaultTimeout(report.vite ? 30000 : 10000);
        await page.goto(fixture.origin+base);await expect(page.locator('.cd-welcome')).toBeVisible();await page.evaluate(()=>document.fonts.ready);
        const surface=await page.evaluate(()=>{
            const values=selector=>{const e=document.querySelector(selector),s=getComputedStyle(e),r=e.getBoundingClientRect();return {background:s.backgroundColor,color:s.color,border:s.borderTopColor,bottomBorder:s.borderBottomColor,borderStyle:s.borderTopStyle,borderWidth:s.borderTopWidth,visibility:s.visibility,opacity:s.opacity,display:s.display,width:r.width,height:r.height};};
            const img=document.querySelector('.cd-brandmark img');return {blue:getComputedStyle(document.querySelector('.cd-site')).getPropertyValue('--cd-blue').trim(),rootBlue:getComputedStyle(document.documentElement).getPropertyValue('--uacj-blue').trim(),header:values('.cd-header'),name:values('.account-visible-name'),nav:values('.cd-nav button'),footer:values('.cd-footer'),card:values('.cd-card'),logoLoaded:img.complete&&img.naturalWidth>0&&img.getAttribute('src')==='/images/logos/transformacion-digital-blanco.svg',logoFilter:getComputedStyle(img).filter};
        });
        report.surfaces.push({width,...surface});
        await page.screenshot({path:path.join(output,`digital-home-${width}.png`),fullPage:true});
        if(reproduce){assert.equal(surface.blue,'');assert.equal(surface.header.background,'rgba(0, 0, 0, 0)');assert.equal(surface.name.color,'rgb(255, 255, 255)');assert.equal(surface.name.opacity,'1');assert(surface.logoLoaded);await context.close();continue;}
        assert.equal(surface.header.background,'rgb(33, 52, 85)');assert.equal(surface.header.bottomBorder,'rgb(33, 52, 85)');assert.equal(surface.footer.background,'rgb(33, 52, 85)');assert.equal(surface.name.color,'rgb(255, 255, 255)');assert.equal(surface.nav.color,'rgb(255, 255, 255)');assert(surface.logoLoaded);assert.equal(surface.logoFilter,'none');
        assert.equal(surface.card.background,'rgb(255, 255, 255)');assert.equal(surface.card.border,'rgb(218, 218, 218)');assert.equal(surface.card.borderStyle,'solid');assert.equal(surface.card.borderWidth,'1px');
        const contrast=await renderedContrast(page,'.cd-header');assert(contrast.every(c=>c.ratio>=c.required),JSON.stringify(contrast));report.surfaces.at(-1).contrast=contrast;
        const alignment=await page.evaluate(()=>{
            const rect=e=>{const r=e.getBoundingClientRect();return {left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width,height:r.height,center:r.top+r.height/2};};
            const main=document.querySelector('#cd-main'),s=getComputedStyle(main);
            return {wrappers:[...document.querySelectorAll('.cd-header-inner,.cd-titleband>.cd-wrap,#cd-main,.cd-footer>.cd-wrap')].map(rect),
                left:main.getBoundingClientRect().left+parseFloat(s.paddingLeft),right:main.getBoundingClientRect().right-parseFloat(s.paddingRight),
                logo:rect(document.querySelector('.cd-brandmark')),account:rect(document.querySelector('.account-trigger')),nav:rect(document.querySelector('.cd-nav')),
                footer:[...document.querySelectorAll('.cd-footer .institutional-signatures>*')].map(rect),paragraph:rect(document.querySelector('.cd-welcome>p'))};
        });
        const common=alignment.wrappers[0];assert.equal(Math.round(common.width),Math.min(width,1400));assert(Math.abs(common.left-(width-common.width)/2)<1);
        for(const box of alignment.wrappers) {assert(Math.abs(box.left-common.left)<1);assert(Math.abs(box.right-common.right)<1);}
        assert(Math.abs(alignment.logo.left-alignment.left)<1);assert(Math.abs(alignment.account.right-alignment.right)<1);
        assert(Math.abs(alignment.logo.center-alignment.account.center)<1);assert(alignment.nav.top>=alignment.logo.bottom-1);
        assert(Math.abs(alignment.nav.left-alignment.left)<1);assert(Math.abs(alignment.nav.right-alignment.right)<1);
        assert(Math.abs(alignment.footer[0].left-alignment.left)<1);assert(alignment.paragraph.width<800);
        if(width>=1200){for(let i=1;i<alignment.footer.length;i++)assert(alignment.footer[i].left-alignment.footer[i-1].right>=16);}
        const account=page.getByRole('button',{name:'Abrir menú de usuario · Persona Sintética de Verificación',exact:true});
        if(width<=700){await expect(page.locator('.account-visible-name')).toBeHidden();assert(alignment.account.width>=44&&alignment.account.height>=44);}
        else {await expect(page.locator('.account-visible-name')).toBeVisible();await expect(page.locator('.account-trigger>.MuiSvgIcon-root')).toBeVisible();}
        const brand=page.getByRole('link',{name:'Volver al portal de Transformación Digital',exact:true});await expect(brand).toHaveAttribute('href','/');await brand.focus();await page.keyboard.press('Tab');await expect(account).toBeFocused();await expect(account).toHaveCSS('outline-color','rgb(255, 255, 255)');await expect(account).toHaveCSS('outline-style','solid');
        await page.keyboard.press('Enter');await expect(page.getByRole('menu').getByText('Persona Sintética de Verificación',{exact:true})).toBeVisible();
        await actualFont(page,'#account-menu .MuiTypography-body2','cuenta-'+width);
        await page.screenshot({path:path.join(output,`digital-account-${width}.png`)});
        await page.keyboard.press('Escape');await expect(account).toBeFocused();
        report.surfaces.at(-1).alignment=alignment;
        await page.goto(fixture.origin+base+'/encuesta');
        await expect(page.locator('.cd-profile-groups')).toBeVisible();await expect(page.locator('.cd-account img')).toHaveAttribute('alt','Persona Sintética de Verificación');
        await expect(page.locator('[id="perfil:antig"]')).toHaveCSS('border-top-color','rgb(218, 218, 218)');
        await expect(page.locator('.cd-header .institutional-directorate')).toHaveCount(0);
        await expect(page.locator('.cd-brandmark img')).toHaveAttribute('alt','Transformación Digital UACJ');
        assert(await page.locator('.cd-brandmark img').evaluate(e=>{const r=e.getBoundingClientRect();return Math.abs(r.width/r.height-e.naturalWidth/e.naturalHeight)<.02;}),'Logotipo oficial completo y proporcionado');
        await expect(page.locator('.cd-nav button')).toHaveText(['Encuesta','Mi informe y ruta']);
        await expect(page.locator('.cd-nav button[aria-current]')).toHaveCSS('border-bottom-color','rgb(240, 189, 16)');
        await account.focus();await page.keyboard.press('Tab');const focusedModule=page.locator('.cd-nav button').first();await expect(focusedModule).toBeFocused();await expect(focusedModule).toHaveCSS('outline-color','rgb(255, 255, 255)');await expect(focusedModule).toHaveCSS('outline-style','solid');
        const focusedContrast=await renderedContrast(page,'.cd-nav');assert(focusedContrast.every(c=>c.ratio>=c.required),JSON.stringify(focusedContrast));report.surfaces.at(-1).focusedContrast=focusedContrast;
        await page.screenshot({path:path.join(output,`digital-navigation-focus-${width}.png`)});
        await expect(page.locator('.cd-steps button').nth(1)).toBeDisabled();
        await actualFont(page,'.cd-profile-body h2','perfil-'+width);
        if(width>700) await actualFont(page,'.account-visible-name','nombre-cabecera-'+width);
        await page.screenshot({path:path.join(output,`digital-profile-${width}.png`),fullPage:true});
        await verifySignatures(page,'.cd-footer',true);
        await page.locator('.cd-footer').screenshot({path:path.join(output,`digital-footer-${width}.png`)});
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();
        await expect(page.locator('[id="perfil:antig"]')).toBeFocused();
        const position=await page.locator('[id="perfil:antig"]').boundingBox(),head=await page.locator('.cd-header').boundingBox();
        assert(position.y>head.y+head.height,'El foco pendiente no queda bajo la cabecera');
        const before=writes.length; await page.getByRole('button',{name:'Abrir menú de usuario'}).click();
        await page.getByRole('menuitem',{name:'Aviso de privacidad'}).click();await expect(page.getByRole('dialog')).toBeVisible();
        await expect(page.getByRole('dialog').getByRole('heading')).toHaveText('Aviso de privacidad');
        await expect(page.getByRole('dialog')).not.toContainText('simplificado');
        await actualFont(page,'#cd-notice-title','aviso-'+width);await page.keyboard.press('Escape');assert.equal(writes.length,before);
        if(width===1366){failSave=true;await page.locator('[id="perfil:antig"]').selectOption(profile.antig);await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();await expect(page.getByText('Fallo de guardado sintético. Inténtalo nuevamente.')).toBeVisible();await expect(page.locator('[id="perfil:antig"]')).toHaveValue(profile.antig);await expect(page.locator('.cd-steps button').nth(1)).toBeDisabled();failSave=false;}
        scenario='questions';await page.goto(fixture.origin+base+'/encuesta');await expect(page.locator('.cd-question').first()).toBeVisible();
        await page.screenshot({path:path.join(output,`digital-questions-${width}.png`)});
        scenario='report';await page.goto(fixture.origin+base+'/mi-informe-ruta');await expect(page.locator('.cd-report')).toBeVisible();
        await actualFont(page,'.cd-report-hero h2','informe-'+width);await expect(page.locator('.cd-gap').first()).toHaveCSS('background-color','rgb(240, 189, 16)');
        await page.screenshot({path:path.join(output,`digital-report-${width}.png`),fullPage:true});
        await verifySignatures(page,'.cd-footer',true);
        report.checks.push({width,flow:await verifyFooterFlow(page,'#cd-main','.cd-footer'),profile:true,report:true,privacyReadOnly:true});
        missingPhoto=true;scenario='profile';await page.goto(fixture.origin+base);await expect(page.locator('.cd-account .MuiAvatar-root')).toHaveText('PS');
        represented=true;await page.reload();await expect(page.locator('.cd-representation')).toContainText('Persona Representada Sintética');await expect(page.locator('.cd-account .MuiAvatar-root')).toHaveText('PR');
        const representedContrast=await renderedContrast(page,'.cd-header');assert(representedContrast.every(c=>c.ratio>=c.required),JSON.stringify(representedContrast));
        await expect(page.getByRole('button',{name:'Abrir menú de usuario · Persona Representada Sintética',exact:true})).toBeVisible();
        const exit=page.getByRole('button',{name:'Volver a mi usuario',exact:true});await expect(exit).toBeVisible();await exit.focus();await expect(exit).toHaveCSS('outline-color','rgb(33, 52, 85)');
        await page.screenshot({path:path.join(output,`digital-representation-${width}.png`),fullPage:true});
        await context.close();
    }
    if(!reproduce) for(const width of [1366,768,390,320]) {
        const context=await browser.newContext({viewport:{width,height:width<500?844:900},reducedMotion:'reduce'});
        await context.route('**/*',r=>new URL(r.request().url()).origin===fixture.origin?r.continue():r.abort());
        const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));
        page.on('response',response=>{if(/\/(css|fonts)\//.test(response.url()))report.resources.push({url:new URL(response.url()).pathname,status:response.status(),type:response.headers()['content-type']});});
        for(const role of Object.keys(menus)) {
            accessCase=role;scenario='profile';represented=false;
            await page.goto(fixture.origin+base);
            const order=menus[role];
            await expect(page.locator('.cd-nav button')).toHaveText(order.map(i=>names[i]));
            await expect(page.locator('.cd-nav').getByRole('group')).toHaveCount(0);
            await expect(page.locator('.cd-nav-group')).toHaveCount(0);
            assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
            if(role==='administrador') {
                await page.screenshot({path:path.join(output,`digital-navigation-admin-${width}.png`)});
                await page.getByRole('button',{name:'Encuesta',exact:true}).click();
                await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();
                await expect(page.locator('[id="perfil:antig"]')).toBeFocused();
                const field=await page.locator('[id="perfil:antig"]').boundingBox(),header=await page.locator('.cd-header').boundingBox();
                assert(field.y>=header.y+header.height,'El menú completo no oculta el campo pendiente');
                await page.screenshot({path:path.join(output,`digital-survey-admin-${width}.png`)});
            }
            if(['administrador','supervisor','responsable'].includes(role)) for(const index of [0,1]) {
                await page.getByRole('button',{name:names[index],exact:true}).click();
                await expect(page.locator('.cd-nav button[aria-current]')).toHaveText(names[index]);
                await expect(page.locator('#cd-main>section').first().locator('h2')).toHaveText(`${unit.code} - ${unit.description}`);
                await expect(page.getByRole('combobox',{name:'Nivel de UR',exact:true})).toHaveCount(role==='responsable'?0:1);
                await expect(page.getByRole('region',{name:'Detalle jerárquico de unidades'})).not.toContainText('Nivel de UR');
                await expect(page.locator('#cd-main')).not.toContainText('Tipo UR');
                const dependent=page.getByRole('combobox',{name:'Unidad dependiente',exact:true});
                await dependent.fill(area.code);await expect(page.getByRole('option')).toContainText(`${area.code} - ${area.description}`);
                await expect(page.getByRole('option')).toContainText(`Depende de ${unit.code} - ${unit.description}`);await page.keyboard.press('Escape');
                await verifySignatures(page,'.cd-footer',true);
                if(role==='administrador') await page.screenshot({path:path.join(output,`digital-${slugs[index]}-${width}.png`),fullPage:true});
            }
        }
        report.checks.push({width,authorizedNavigationCases:Object.keys(menus),aggregateNames:true});await context.close();
    }
    if(!reproduce){assert(report.resources.some(r=>r.url==='/css/institutional.css'&&r.status===200&&r.type?.startsWith('text/css')));assert(report.resources.some(r=>r.url==='/images/logos/transformacion-digital-blanco.svg'&&r.status===200&&r.type?.startsWith('image/svg+xml')));assert(!report.resources.some(r=>r.url.startsWith('/__vite/css/')||r.url.startsWith('/__vite/fonts/')));}
    assert.deepEqual(report.errors,[]);report.completed=true;
} finally {await writeFile(path.join(output,'digital-visual-report.json'),JSON.stringify(report,null,2));await browser.close();await fixture.close();}
