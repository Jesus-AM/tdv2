import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { verifyFooterFlow } from './page-flow.mjs';
import { verifySignatures } from './identity-checks.mjs';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host sintético aislado.');
const base = '/competencias-digitales';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticSources: true, realPostgreSql: true, checks: [], skipped: [], errors: [], layouts: [] };
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce' });
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    // El retorno del proveedor se simula también cuando OAuth viene de una cadena de redirecciones.
    // No se envía ninguna petición a Microsoft desde este navegador de pruebas.
    if (url.hostname === 'login.microsoftonline.com' && url.pathname.includes('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'))
        return route.fulfill({ status: 302, headers: { location: `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(url.searchParams.get('state'))}` }, body: '' });
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(); page.setDefaultTimeout(15000); page.on('pageerror', e => report.errors.push(e.message));
const check = async (name, run) => {
    if (process.env.TDV2_DIGITAL_PROFILE_ONLY === '1' && !['portal conserva diseño y el botón inicia OAuth hacia Competencias','aviso accesible: sin preselección, Escape/cierre no aceptan y captura bloqueada','perfil institucional ineditable y perfil personal persistido'].includes(name)) { report.skipped.push({name,reason:'Recorrido limitado a perfil y su acceso.'}); return; }
    if (process.env.TDV2_DIGITAL_PRESENTATION_ONLY === '1' && name.startsWith('reinicio:')) { report.skipped.push({name,reason:'Revisión de presentación: no ejecutar reinicios.'}); return; }
    await run(); report.checks.push({ name, passed: true }); console.log('DIGITAL UI PASS ' + name);
};
const control = async action => assert.equal((await context.request.post(origin + '/__digitalfixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200);
const getSurvey = async () => { const r = await context.request.get(origin + base + '/api/encuesta'); assert.equal(r.status(), 200); return r.json(); };
const api = async (method, endpoint, data) => {
    if (data && /^\/api\/(encuesta|aviso|metas)/.test(endpoint) && !('generation' in data)) data = { ...data, generation: (await getSurvey()).generation };
    const csrf = await (await context.request.get(origin + '/session/csrf')).json();
    return context.request.fetch(origin + base + endpoint, { method, data, headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own' } });
};
const openPrivacy = async () => { await page.getByRole('button',{name:'Abrir menú de usuario'}).click(); await page.getByRole('menuitem',{name:'Aviso de privacidad',exact:true}).click(); };
const save = async () => { const done = page.waitForResponse(r => r.url().endsWith('/api/encuesta/guardar') && r.request().method() === 'POST'); await page.getByRole('button', { name: 'Guardar avance', exact: true }).click(); assert.equal((await done).status(), 200); await expect(page.getByText('Avance guardado. Puedes continuar desde otro dispositivo.', { exact: true })).toBeVisible(); };
const completeBlock = async id => {
    await expect(page.getByRole('button', {name:'Guardar avance',exact:true})).toBeEnabled();
    const s = await getSurvey();
    for (const t of s.instrument.modules.find(b => b.id === id).tools) if (!t.opt && (!t.doc || s.profile.puesto === 'Personal Académico')) s.answers[t.id] ||= ['ciber','datos','sii'].includes(id) ? { state: 'level', checks: '000000000000' } : { state: 'level', level: 0 };
    const r = await api('PUT', '/api/encuesta', { version:s.version, profile:s.profile, answers:s.answers }); assert.equal(r.status(),200); return r.json();
};
const steps = () => page.getByRole('navigation', {name:'Bloques de la encuesta'});
const captureDocument = async (target, name) => {
    // El informe móvil puede superar el límite de textura de Chromium: evidencia
    // de la ventana visible; los recorridos verifican también el contenido inferior.
    await target.screenshot({ path: path.join(artifacts, name + '.png'), fullPage: false });
};
const screenshot = async name => {
    await expect(page.locator('.cd-site h1')).toBeVisible();
    await page.evaluate(() => scrollTo({top:0,left:0,behavior:'instant'}));
    await page.evaluate(async () => { await document.fonts.ready; await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))); });
    await captureDocument(page, name);
};
try {
    await check('portal conserva diseño y el botón inicia OAuth hacia Competencias', async () => {
        await page.goto(origin + '/'); await expect(page.getByRole('link', { name: 'Responder encuesta de competencias digitales', exact: true })).toHaveAttribute('href', base);
        await page.getByRole('link', { name: 'Responder encuesta de competencias digitales', exact: true }).click(); await page.waitForURL(origin + base);
        await expect(page.getByRole('heading', { name: 'Conoce tus competencias digitales' })).toBeVisible();
        await expect(page.getByRole('navigation', { name: 'Módulos de Competencias digitales' }).getByRole('button')).toHaveCount(5);
    });
    await check('avance y resultados: rutas directas, recargas, gráficas y tablas con PostgreSQL', async () => {
        for (const slug of ['avance-dependencia', 'resultados-generales', 'mi-informe-ruta', 'configuracion']) { await page.goto(origin + base + '/' + slug); await expect(page.locator('.cd-site h1')).toBeVisible(); await page.reload(); await expect(page.locator('.cd-site h1')).toBeVisible(); }
        await page.goto(origin + base + '/avance-dependencia'); await expect(page.getByText('Área sintética A4', { exact: false }).first()).toBeVisible(); await screenshot('competencias-avance-desktop');
        await page.goto(origin + base + '/resultados-generales'); await expect(page.getByRole('heading', { name: 'Resultados por bloque', exact: true })).toBeVisible(); await screenshot('competencias-resultados-desktop');
        const exportResponse = await context.request.get(origin + base + '/api/resultados/descarga?unit=A'); assert.equal(exportResponse.status(), 200); const csv = await exportResponse.text(); assert(!csv.includes('@uacj.mx') && !csv.includes('synthetic-browser'));
    });
    await check('aviso accesible: sin preselección, Escape/cierre no aceptan y captura bloqueada', async () => {
        await page.goto(origin + base + '/encuesta'); const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible();
        await expect(dialog.getByRole('button', { name: 'Continuar', exact: true })).toBeDisabled();
        await page.keyboard.press('Tab'); assert(await dialog.evaluate(d => d.contains(document.activeElement)));
        await page.keyboard.press('Escape'); await expect(dialog).toHaveCount(0); assert.equal((await getSurvey()).accepted, false);
        await openPrivacy(); await expect(dialog.getByRole('checkbox')).toHaveCount(0);
        await page.getByRole('button',{name:'Cerrar',exact:true}).click(); assert.equal((await getSurvey()).accepted,false);
        await expect(page.getByRole('button', { name: 'Guardar avance' })).toBeDisabled();
        await page.getByRole('button', { name: 'Leer y confirmar' }).click(); await dialog.getByRole('checkbox').check();
        await dialog.getByRole('button', { name: 'Continuar', exact: true }).click(); await expect(dialog).toHaveCount(0); assert.equal((await getSurvey()).accepted, true);
        await page.reload(); await expect(page.getByRole('dialog')).toHaveCount(0);
    });
    await check('perfil institucional ineditable y perfil personal persistido', async () => {
        await expect(page.getByText('Información completada automáticamente: nombre y adscripción')).toBeVisible();
        await page.getByRole('button', {name:'Guardar y continuar',exact:true}).click();
        await expect(page.getByText('Completa los campos pendientes de esta sección para continuar.')).toBeVisible();
        await expect(steps().getByRole('button',{name:'Microsoft 365',exact:true})).toBeDisabled();
        await expect(page.locator('[id="perfil:puesto"]')).toBeFocused();
        const before = await getSurvey(); await openPrivacy();
        await expect(page.getByRole('dialog').getByRole('checkbox')).toHaveCount(0); await page.getByRole('button',{name:'Cerrar',exact:true}).click();
        assert.equal((await getSurvey()).version,before.version);
        assert.equal(await page.getByRole('textbox', { name: 'Nombre', exact: true }).count(), 0);
        for (const width of [1366,390,320]) {
            await page.setViewportSize({width,height:900});
            await expect(page.locator('.cd-profile-group>h3')).toHaveText(['Datos institucionales','Perfil laboral','Uso de herramientas e información']);
            assert.deepEqual(await page.locator('.cd-profile-groups [id^="perfil:"]').evaluateAll(es=>es.map(e=>e.id)),['perfil:puesto','perfil:antig','perfil:funciones','perfil:sup','perfil:freq','perfil:datosP']);
            const columns=await page.locator('.cd-profile-group .cd-form-grid').first().evaluate(e=>getComputedStyle(e).gridTemplateColumns.split(' ').length);
            assert.equal(columns,width===1366?2:1);
            await page.locator('[id="perfil:puesto"]').focus(); await page.keyboard.press('Tab'); await expect(page.locator('[id="perfil:antig"]')).toBeFocused();
            assert(await page.locator('.cd-readonly').evaluateAll(es=>es.every(e=>e.scrollHeight<=e.clientHeight+1&&e.scrollWidth<=e.clientWidth+1)));
            await screenshot('perfil-organizado-'+width);
            report.layouts.push({width,flow:await verifyFooterFlow(page,'#cd-main','.cd-footer')});
        }
        await page.setViewportSize({width:1366,height:900});
        const survey = await getSurvey();
        for (const f of survey.instrument.profile) {
            if (f.multiple) await page.getByRole('checkbox', { name: f.options[0], exact: true }).check();
            else if (['antig','puesto'].includes(f.id)) await page.getByRole('combobox', { name: f.label, exact: false }).selectOption(f.options[0]);
            else await page.locator('[id="perfil:'+f.id+'"]').getByRole('radio',{name:f.options[0],exact:true}).check();
        }
        await save(); await screenshot('competencias-perfil-desktop'); await page.reload(); assert.equal((await getSurvey()).profile.puesto, 'Dirección');
    });
    await check('M365 conserva rúbricas y Inicial cero; complementaria no elegida no es pendiente', async () => {
        await page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', { name: 'Microsoft 365', exact: true }).click();
        await page.locator('#m-word').getByRole('radio', { name: 'Inicial', exact: true }).check(); await save();
        assert.equal((await getSurvey()).answers['m-word'].level, 0); await screenshot('competencias-encuesta-desktop');
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click(); await expect(steps().getByRole('button',{name:'Ciberseguridad',exact:true})).toBeDisabled();
        const optional = (await getSurvey()).instrument.modules[0].tools.find(t => t.opt && !t.doc);
        await page.locator('.cd-optional').getByRole('checkbox',{name:optional.n,exact:true}).check(); await save();
        assert((await getSurvey()).navigation.steps[1].pending.includes(optional.id));
        await page.locator('.cd-optional').getByRole('checkbox',{name:optional.n,exact:true}).uncheck(); await save();
        await completeBlock('m365'); await page.reload();
    });
    await check('rúbricas de Ciberseguridad permiten confirmación Inicial y captura por indicadores', async () => {
        await page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', { name: 'Ciberseguridad', exact: true }).click();
        const questions = page.locator('.cd-question'); await questions.first().getByRole('button', { name: 'No realizo ninguna · Inicial' }).click();
        for (let i = 0; i < 3; i++) await questions.nth(1).getByRole('checkbox').nth(i).check();
        await save(); const survey = await getSurvey(); const block = survey.instrument.modules.find(m => m.id === 'ciber');
        assert.equal(survey.answers[block.tools[0].id].checks, '000000000000'); assert.equal(survey.answers[block.tools[1].id].checks.slice(0, 4), '1110');
    });
    await check('cada bloque bloquea su siguiente sección y URL/API respetan el avance', async () => {
        const s = await getSurvey(); assert.equal((await api('POST','/api/encuesta/finalizar',{version:s.version})).status(),422);
        const r=await context.request.get(origin+base+'/api/encuesta?paso=ia'); assert.equal((await r.json()).navigation.current,'ciber');
        await page.goto(origin+base+'/encuesta?paso=ia'); await expect(steps().getByRole('button',{name:'Ciberseguridad',exact:true})).toHaveAttribute('aria-current','step');
        const saved = page.waitForResponse(r => r.url().endsWith('/api/encuesta') && r.request().method() === 'PUT');
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click(); assert.equal((await saved).status(),200); await expect(steps().getByRole('button',{name:'Gobernanza de datos',exact:true})).toBeDisabled();
        await completeBlock('ciber');
    });
    await check('móvil 390/320: cinco bloques, aviso desplazable, controles y sin desbordamiento exterior', async () => {
        for (const width of [390, 320]) {
            await page.setViewportSize({ width, height: 844 }); await page.goto(origin + base + '/encuesta?paso=perfil');
            await screenshot('competencias-perfil-mobile-' + width);
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', { name: 'Gobernanza de datos', exact: true }).click();
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await screenshot('competencias-encuesta-mobile-' + width);
            await openPrivacy(); await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Cerrar', exact: true }).click();
            await page.goto(origin + base + '/resultados-generales'); await expect(page.locator('.cd-site h1')).toBeVisible(); assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); await screenshot('competencias-resultados-mobile-' + width);
        }
        await page.setViewportSize({ width: 1366, height: 900 });
    });
    await check('error de guardado conserva cambios y no muestra éxito', async () => {
        await page.goto(origin + base + '/encuesta'); await page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', { name: 'Microsoft 365', exact: true }).click();
        await page.locator('#m-word').getByRole('radio', { name: 'Básico', exact: true }).check();
        await page.route('**/competencias-digitales/api/encuesta', async route => route.request().method() === 'PUT' ? route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'Guardado temporalmente no disponible.' }) }) : route.continue());
        await page.getByRole('button', { name: 'Guardar y continuar', exact: true }).click(); await expect(page.getByText('Guardado temporalmente no disponible.', { exact: true })).toBeVisible(); await expect(page.locator('#m-word').getByRole('radio', { name: 'Básico', exact: true })).toBeChecked();
        await expect(steps().getByRole('button',{name:'Microsoft 365',exact:true})).toHaveAttribute('aria-current','step');
        await page.unroute('**/competencias-digitales/api/encuesta'); await save();
    });
    await check('respuesta perdida después de guardar: reintento no duplica y solo entonces avanza', async () => {
        await page.goto(origin+base+'/encuesta?paso=m365');
        await page.route('**/competencias-digitales/api/encuesta',async route => {
            if(route.request().method()!=='PUT') return route.continue();
            await route.fetch(); return route.fulfill({status:503,contentType:'application/json',body:JSON.stringify({message:'Respuesta de red perdida después del guardado.'})});
        });
        await page.locator('#m-word').getByRole('radio',{name:'Intermedio',exact:true}).check();
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();
        await expect(page.getByText('Respuesta de red perdida después del guardado.',{exact:true})).toBeVisible();
        await expect(steps().getByRole('button',{name:'Microsoft 365',exact:true})).toHaveAttribute('aria-current','step');
        const committed=await getSurvey(); await page.unroute('**/competencias-digitales/api/encuesta');
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();
        await expect(steps().getByRole('button',{name:'Ciberseguridad',exact:true})).toHaveAttribute('aria-current','step');
        assert.equal((await getSurvey()).version,committed.version);
    });
    await check('retroceso, edición anterior y salida a otro módulo conservan el borrador', async () => {
        await page.goto(origin + base + '/encuesta?paso=perfil');
        const before = await getSurvey(), field = before.instrument.profile.find(f => f.id === 'antig');
        await page.getByRole('combobox', {name:field.label,exact:false}).selectOption('');
        await page.getByRole('button',{name:'Guardar y continuar',exact:true}).click();
        await expect(page.getByText('Completa los campos pendientes de esta sección para continuar.',{exact:true})).toBeVisible();
        await expect(steps().getByRole('button',{name:'Microsoft 365',exact:true})).toBeDisabled();
        const after = await getSurvey(); assert.deepEqual(after.answers,before.answers); assert.equal(after.navigation.resume,'perfil');
        await page.reload(); await expect(steps().getByRole('button',{name:'Perfil',exact:true})).toHaveAttribute('aria-current','step');
        await page.getByRole('combobox',{name:field.label,exact:false}).selectOption(field.options[0]);
        // La navegación espera al guardado y continúa por el mecanismo compartido del router.
        await page.getByRole('navigation',{name:'Módulos de Competencias digitales'}).getByRole('button',{name:'Mi informe y ruta',exact:true}).click();
        await page.waitForURL(origin+base+'/mi-informe-ruta'); assert.equal((await getSurvey()).profile.antig,field.options[0]);
        await page.goto(origin+base+'/encuesta'); await expect(steps().getByRole('button',{name:'Gobernanza de datos',exact:true})).toHaveAttribute('aria-current','step');
    });
    await check('cuenta compartida: nombre completo, fotografía y avatar ante imagen fallida', async () => {
        await page.goto(origin+base+'/encuesta?paso=perfil');
        await expect(page.locator('.cd-account img')).toHaveAttribute('alt','Persona sintética');
        await expect(page.locator('.cd-account img')).toBeVisible();
        assert(await page.locator('.cd-account img').evaluate(i => i.complete && i.naturalWidth > 0));
        await page.getByRole('button',{name:'Abrir menú de usuario'}).click();
        await expect(page.getByRole('menu').getByText('Persona sintética',{exact:true})).toBeVisible();
        await expect(page.getByRole('menuitem',{name:'Cerrar sesión'})).toBeVisible(); await page.keyboard.press('Escape');
        await page.route('**/user/photo',async route => { const r=await route.fetch(); const data=await r.json(); return route.fulfill({response:r,json:{...data,photo:'data:image/png;base64,AAAA'}}); });
        const failedPhoto = page.waitForResponse(r=>r.url().endsWith('/user/photo'));
        await page.reload(); await failedPhoto; await expect(page.locator('.cd-account .MuiAvatar-root')).toHaveText('Ps');
        await page.unroute('**/user/photo'); await page.reload();
    });
    await check('comparación visual con original en escritorio y móvil, fuentes y elementos fijos', async () => {
        const original=await readFile('../docs/competencias-digitales/referencia-original.html','utf8');
        const reference=await context.newPage();
        // Copia solo en memoria: no ejecutar el arranque del entorno de autoría ni modificar el original.
        let html=original.replace(/boot\(\);\s*<\/script>/,'</script>').replace(/<link[^>]+fonts\.googleapis[^>]+>/g,'');
        const fontCss=[['Regular',400],['Medium',500],['SemiBold',600],['Bold',700]].map(([weight,n]) => `@font-face{font-family:"Noto Sans";src:url('/fonts/noto-sans/NotoSans-${weight}.ttf');font-weight:${n}}`).join('');
        html=html.replace('</head>',`<style>${fontCss}</style></head>`);
        await reference.route('**/__reference',r=>r.fulfill({contentType:'text/html; charset=utf-8',body:html}));
        await reference.goto(origin+'/__reference');
        await reference.evaluate("S.view='encuesta';S.step='perfil';S.me=blankMe();S.nombre='Persona sintética';S.privOK=true;render();document.documentElement.style.setProperty('--hh',document.getElementById('hdr').offsetHeight+'px');");
        for(const width of [1366,390,320]) {
            await page.setViewportSize({width,height:900}); await reference.setViewportSize({width,height:900});
            await page.goto(origin+base+'/encuesta?paso=perfil'); await page.evaluate(()=>document.fonts.ready); await reference.evaluate(()=>document.fonts.ready);
            await screenshot('ajuste-perfil-'+width); await reference.screenshot({path:path.join(artifacts,'referencia-perfil-'+width+'.png'),fullPage:true});
            assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
            await page.locator('[id="perfil:puesto"]').focus(); await page.locator('[id="perfil:puesto"]').evaluate(e=>e.scrollIntoView({block:'center'}));
            assert(await page.locator('[id="perfil:puesto"]').evaluate(e=>e.getBoundingClientRect().top>=document.querySelector('.cd-progress').getBoundingClientRect().bottom));
            const cdp=await context.newCDPSession(page); await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
            const {root}=await cdp.send('DOM.getDocument'); const {nodeId}=await cdp.send('DOM.querySelector',{nodeId:root.nodeId,selector:'.cd-titleband h1'});
            const fonts=await cdp.send('CSS.getPlatformFontsForNode',{nodeId}); assert(fonts.fonts.some(f=>f.familyName==='Noto Sans'&&f.isCustomFont)); await cdp.detach();
        }
        for(const width of [1366,390]) {
            await page.setViewportSize({width,height:900}); await reference.setViewportSize({width,height:900});
            await page.goto(origin+base+'/encuesta?paso=m365'); await screenshot('ajuste-preguntas-'+width);
            await reference.evaluate("S.step='m365';render();"); await reference.screenshot({path:path.join(artifacts,'referencia-preguntas-'+width+'.png'),fullPage:true});
        }
        await page.setViewportSize({width:1366,height:900});
        await reference.close();
    });
    await check('finalización completa e informe sin descarga ni acciones de edición', async () => {
        for(const id of ['datos','sii','ia']) await completeBlock(id);
        await page.goto(origin+base+'/encuesta'); await page.getByRole('button', { name: 'Finalizar encuesta', exact: true }).click(); await page.getByRole('button', { name: 'Confirmar finalización', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'Tu encuesta ya fue completada' })).toBeVisible(); await page.reload(); await expect(page.getByRole('button', { name: 'Guardar avance' })).toHaveCount(0);
        await page.getByRole('button', { name: 'Ver mi informe', exact: true }).click(); await expect(page.getByText(/Informe definitivo · Finalizada el/)).toBeVisible(); await expect(page.getByRole('link', { name: /Descargar mi informe/ })).toHaveCount(0);
        assert.equal((await context.request.get(origin + base + '/api/informe/descarga')).status(), 501); await screenshot('competencias-informe-desktop');
    });
    await check('informe por bloques, ámbar, flujo de página y footer exclusivo en escritorio/móvil', async () => {
        const survey = await getSurvey(), reference = await context.newPage();
        let html = (await readFile('../docs/competencias-digitales/referencia-original.html','utf8')).replace(/boot\(\);\s*<\/script>/,'</script>').replace(/<link[^>]+fonts\.googleapis[^>]+>/g,'');
        html = html.replace('</head>', `<style>${[['Regular',400],['Medium',500],['SemiBold',600],['Bold',700]].map(([w,n])=>`@font-face{font-family:"Noto Sans";src:url('/fonts/noto-sans/NotoSans-${w}.ttf');font-weight:${n}}`).join('')}</style></head>`);
        await reference.route('**/__report-reference',r=>r.fulfill({contentType:'text/html; charset=utf-8',body:html})); await reference.goto(origin+'/__report-reference');
        await reference.evaluate(s => { S.me = {...blankMe(),...s.profile,dep:'Área sintética A',area:'Área sintética A3',ans:Object.fromEntries(s.evaluation.report.tools.map(t=>[t.id,t.value])),completedAt:s.completedAt}; S.nombre='Persona sintética'; S.privOK=true; S.view='informe'; render(); },survey);
        for (const width of [1366,390,320]) {
            await page.setViewportSize({width,height:900}); await reference.setViewportSize({width,height:900});
            await page.goto(origin+base+'/mi-informe-ruta'); await expect(page.locator('.cd-report-blocks>.cd-card')).toHaveCount(5);
            await expect(page.locator('.cd-report-tools table')).toHaveCount(width>700?1:0);
            assert(await page.locator('.cd-report-tools').evaluateAll(es=>es.every(e=>e.clientHeight>=e.scrollHeight-2)));
            assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
            if(width<=700) await expect(page.getByRole('combobox',{name:'Bloque del detalle'})).toBeVisible();
            await verifySignatures(page, '.cd-footer', true); await expect(page.locator('.cd-footer button,.cd-footer a,.cd-footer p')).toHaveCount(0);
            assert.equal(await page.locator('.cd-footer').evaluate(e=>getComputedStyle(e).backgroundColor),'rgb(33, 52, 85)');
            await screenshot('reinicio-informe-'+width); await reference.evaluate(()=>document.fonts.ready); await captureDocument(reference,'referencia-informe-'+width);
            const before=await getSurvey(); await openPrivacy(); const dialog=page.getByRole('dialog'); await expect(dialog.getByRole('checkbox')).toHaveCount(0);
            await page.keyboard.press('Tab'); assert(await dialog.evaluate(d=>d.contains(document.activeElement))); await page.keyboard.press('Escape'); assert.deepEqual(await getSurvey(),before);
        }
        await page.setViewportSize({width:1366,height:900}); await reference.close();
        const gap=page.locator('.cd-gap').first(); if(await gap.count()) assert.equal(await gap.evaluate(e=>getComputedStyle(e).backgroundColor),'rgb(240, 189, 16)');
    });
    await check('configuración administra metas de adscripción directa', async () => {
        await page.goto(origin + base + '/configuracion'); const input = page.getByRole('combobox', { name: 'Unidad responsable de la meta' }); await input.fill('100'); await page.getByRole('option', { name: '100 · Área sintética A', exact: true }).click();
        await page.getByRole('spinbutton', { name: 'Meta de adscripción directa' }).fill('10'); const response = page.waitForResponse(r => r.url().endsWith('/api/metas/A') && r.request().method() === 'PUT'); await page.getByRole('button', { name: 'Guardar meta' }).click(); assert.equal((await response).status(), 200);
    });
    await check('reinicio: confirmación, resumen obsoleto, respuesta perdida y nuevas capturas protegidas', async () => {
        await page.goto(origin+base+'/configuracion'); const stale = await context.newPage(); await stale.goto(origin+base+'/mi-informe-ruta');
        await expect(page.getByRole('checkbox',{name:'Eliminar también las metas de participación'})).not.toBeChecked();
        await page.getByRole('button',{name:'Revisar reinicio global'}).click(); let dialog=page.getByRole('dialog');
        const destroy=()=>dialog.getByRole('button',{name:'Eliminar encuestas de toda la aplicación'});
        await expect(destroy()).toBeDisabled(); await dialog.getByRole('textbox').fill('REINICIAR'); await expect(destroy()).toBeDisabled();
        await dialog.getByRole('button',{name:'Cancelar'}).click(); assert((await getSurvey()).completedAt);
        await page.getByRole('button',{name:'Revisar reinicio global'}).click();
        const config=(await (await context.request.get(origin+base+'/configuracion',{headers:{'X-TDV2-Page':'1'}})).json()).props.digital.content;
        const t=config.targets.find(t=>t.unitId==='A'); assert.equal((await api('PUT','/api/metas/A',{value:11,version:t.version})).status(),200);
        await dialog.getByRole('textbox').fill('REINICIAR ENCUESTAS'); await destroy().click(); await expect(dialog.getByRole('button',{name:'Revisar nuevo resumen'})).toBeVisible();
        await dialog.getByRole('button',{name:'Revisar nuevo resumen'}).click(); await expect(dialog.getByRole('textbox')).toBeEnabled();
        await dialog.getByRole('textbox').fill('REINICIAR ENCUESTAS'); await screenshot('reinicio-confirmacion-desktop');
        await page.route('**/competencias-digitales/api/reinicio',async route=>{await route.fetch();await route.fulfill({status:503,contentType:'application/json',body:JSON.stringify({message:'Respuesta de red perdida después del commit.'})});});
        await destroy().click(); await expect(dialog.getByText('Respuesta de red perdida después del commit.')).toBeVisible();
        const fresh=await getSurvey(); assert.equal(fresh.version,0); assert.equal(fresh.accepted,false);
        assert.equal((await api('POST','/api/aviso',{version:fresh.instrument.notice.version,generation:fresh.generation})).status(),200);
        await page.unroute('**/competencias-digitales/api/reinicio'); await destroy().click(); await expect(dialog.getByRole('heading',{name:'Reinicio confirmado'})).toBeVisible();
        assert.equal((await getSurvey()).version,1); await expect(stale.getByText(/Un administrador reinició las encuestas/)).toBeVisible(); await stale.close();
        await dialog.getByRole('button',{name:'Cerrar y actualizar'}).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
        await page.setViewportSize({width:390,height:844}); await page.getByRole('checkbox',{name:'Eliminar también las metas de participación'}).check(); await page.getByRole('button',{name:'Revisar reinicio global'}).click();
        await dialog.getByRole('textbox').fill('REINICIAR ENCUESTAS'); await screenshot('reinicio-confirmacion-mobile'); await destroy().click(); await expect(dialog.getByRole('heading',{name:'Reinicio confirmado'})).toBeVisible(); await dialog.getByRole('button',{name:'Cerrar y actualizar'}).click();
        await expect(page.getByText('No se han configurado metas.')).toBeVisible(); await page.goto(origin+base+'/encuesta'); await expect(page.getByRole('dialog').getByRole('checkbox')).not.toBeChecked();
        assert.deepEqual((await getSurvey()).profile,{}); assert.equal((await getSurvey()).navigation.current,'perfil'); await page.keyboard.press('Escape'); await page.setViewportSize({width:1366,height:900});
    });
    await check('representación consulta permisos efectivos sin configuración y salida siempre visible', async () => {
        await page.goto(origin + base + '/configuracion'); await page.getByRole('textbox', { name: 'Buscar persona por nombre o correo' }).fill('otra'); await page.getByRole('button', { name: 'Buscar', exact: true }).click();
        await page.getByRole('combobox', { name: 'Persona representada' }).click(); await page.getByRole('option', { name: /Otra persona sintética/ }).click(); await page.getByRole('textbox', { name: 'Motivo de la consulta' }).fill('Comprobar navegación de consulta'); await page.getByRole('button', { name: 'Iniciar representación de consulta' }).click();
        await expect(page.getByText('Consulta como Otra persona sintética')).toBeVisible(); await expect(page.getByRole('navigation', { name: 'Módulos de Competencias digitales' }).getByRole('button')).toHaveCount(2);
        await expect(page.locator('.cd-account .MuiAvatar-root')).toHaveText('Op');
        await page.getByRole('button',{name:'Abrir menú de usuario'}).click();
        await expect(page.getByRole('menu').getByText('Persona sintética',{exact:true})).toBeVisible(); await expect(page.getByRole('menu').getByText('Otra persona sintética',{exact:true})).toBeVisible(); await page.keyboard.press('Escape');
        await page.goto(origin + base + '/encuesta'); await expect(page.getByRole('dialog')).toHaveCount(0); await expect(page.getByRole('button', { name: 'Guardar avance' })).toHaveCount(0); await screenshot('competencias-representacion');
        await page.goto(origin + '/inicio'); await expect(page.getByRole('heading', { name: 'Acceso no disponible' })).toBeVisible();
        await page.getByRole('button', { name: 'Volver a mi usuario', exact: true }).click(); await expect(page.getByText('Consulta como Otra persona sintética')).toHaveCount(0);
        await page.waitForURL(origin + base); await expect(page.locator('.cd-site h1')).toBeVisible();
    });
    await check('supervisor sin configuración, N2/N3 con rama y sin filtro de nivel; participante personal', async () => {
        await control('supervisor'); await page.goto(origin + base + '/avance-dependencia'); await expect(page.getByRole('navigation', { name: 'Módulos de Competencias digitales' }).getByRole('button')).toHaveCount(4);
        for (const role of ['n2', 'n3']) { await control(role); await page.goto(origin + base + '/avance-dependencia'); await expect(page.getByRole('combobox', { name: 'Nivel de UR' })).toHaveCount(0); await expect(page.getByText('Área ajena B4', { exact: false })).toHaveCount(0); await screenshot('competencias-alcance-' + role); }
        await control('participant'); await page.goto(origin + base); await expect(page.getByRole('navigation', { name: 'Módulos de Competencias digitales' }).getByRole('button')).toHaveCount(2);
    });
    await check('regresión /inicio de Procedimientos conserva vista y portada no registra errores JS', async () => {
        await page.goto(origin + '/inicio'); await expect(page.getByText('Procedimientos Institucionales', { exact: false }).first()).toBeVisible(); await page.goto(origin + '/'); await expect(page.getByRole('link', { name: 'Responder encuesta de competencias digitales', exact: true })).toBeVisible(); assert.deepEqual(report.errors, []);
    });
    await check('participante exclusivo conserva nombre y foto sin permisos de Procedimientos', async () => {
        await control('exclusive'); await page.goto(origin+base);
        await expect(page.getByRole('navigation',{name:'Módulos de Competencias digitales'}).getByRole('button')).toHaveCount(2);
        await expect(page.locator('.cd-account img')).toHaveAttribute('alt','Persona sintética');
        await expect(page.locator('.cd-account img')).toBeVisible();
        assert.equal((await context.request.get(origin+'/inicio',{headers:{'X-TDV2-Page':'1'}})).status(),403);
        await page.getByRole('button',{name:'Abrir menú de usuario'}).click(); await expect(page.getByRole('menu').getByText('Persona sintética',{exact:true})).toBeVisible(); await page.keyboard.press('Escape');
    });
} catch (e) { report.failure = { message: e.message, stack: e.stack }; console.error(e); await page.screenshot({ path: path.join(artifacts, 'competencias-failure.png'), fullPage: true }).catch(() => {}); process.exitCode = 1; }
finally { await writeFile(path.join(artifacts, 'competencias-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
