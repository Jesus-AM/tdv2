import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw Error('Only isolated synthetic host');
const browser = await chromium.launch({ channel: 'msedge', headless: true, ignoreDefaultArgs: ['--hide-scrollbars'], args: ['--disable-features=OverlayScrollbar,FluentOverlayScrollbar'] });
const expect = baseExpect.configure({ timeout: 20000 });
const report = { realPostgreSql: true, realAspNet: true, realSignalR: true, identities: ['persona@uacj.mx', 'colaboradora@uacj.mx'], mockedEditingTransport: false, checks: [], errors: [] };
const check = async (name, work) => { await work(); report.checks.push(name); console.log('TABLE PASS ' + name); };
let first, second;
async function control(action) { assert.equal((await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200); }
async function session(who) {
    if (first) await control('delivery-identity?who=' + who);
    const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', hasTouch: true });
    await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
        if (url.pathname === '/connect') {
            const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
            if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.continue();
    });
    const page = await context.newPage(); page.setDefaultTimeout(20000); page.on('pageerror', e => report.errors.push(e.message));
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio'); return { context, page };
}
async function tab(page, name) { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
const row = (page, id) => page.locator(`[data-edit-block="${id}"]`);
const input = (page, label) => page.getByLabel(label, { exact: true });
async function finish(page) { await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
async function stored() { return (await (await first.context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido; }
async function request(page, url, method = 'GET', body) {
    return page.evaluate(async ({ url, method, body }) => {
        const csrf = await (await fetch('/session/csrf')).json();
        const res = await fetch(url, { method, headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own', 'Content-Type': 'application/json' }, body: body && JSON.stringify(body) });
        return { status: res.status, body: await res.json() };
    }, { url, method, body });
}
const live = async page => (await request(page, '/formatos/A/estado?tab=' + crypto.randomUUID())).body;
const mutation = (block, version, value, leaseId) => ({ tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), section: 'sistemas', release: false, blocks: [{ key: block, version, value, leaseId }] });
const longText = Array.from({length:32},(_,i)=>`Línea ${i+1}: Entrega íntegra del procedimiento, con información y saltos de línea conservados.`).join('\n');
const selection = field => field.evaluate(e=>[e.selectionStart,e.selectionEnd,e.selectionDirection]);
async function dragHorizontal(page, selector) {
    const scroll=page.locator(selector).locator('..');
    await scroll.evaluate(e=>{e.scrollLeft=0;e.scrollIntoView({block:'end'});});
    const bar=await scroll.evaluate(e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.bottom-(e.offsetHeight-e.clientHeight)/2,width:r.width,thickness:e.offsetHeight-e.clientHeight,before:e.scrollLeft};});
    assert.ok(bar.thickness>2,'Native scrollbars must be visible; Playwright hide-scrollbars is disabled');
    await page.mouse.move(bar.x+40,bar.y);await page.mouse.down();await page.mouse.move(bar.x+Math.min(bar.width-30,280),bar.y,{steps:12});await page.mouse.up();
    await expect.poll(()=>scroll.evaluate(e=>e.scrollLeft)).toBeGreaterThan(bar.before);
    return scroll;
}
async function bounded(field) {
    const dimensions=await field.evaluate(e=>({height:e.clientHeight,line:parseFloat(getComputedStyle(e).lineHeight),scroll:e.scrollHeight,overflow:getComputedStyle(e).overflowY}));
    assert.ok(dimensions.height<=dimensions.line*4+2,JSON.stringify(dimensions));
    assert.ok(dimensions.scroll>dimensions.height);assert.notEqual(dimensions.overflow,'hidden');return dimensions;
}
async function choose(page,label,value) { await page.getByRole('combobox',{name:label+' 1',exact:true}).click();await page.getByRole('option',{name:value,exact:true}).click(); }
async function layout(page,key) {
    const r=row(page,key),firstCell=r.locator('td').first();
    assert.equal(await firstCell.getAttribute('class').then(c=>c.includes('row-editing-cell')),true);
    assert.equal(await r.locator('.row-presence').count(),1);
    const dim=await r.evaluate(e=>{const b=e.querySelector('button[aria-label="Eliminar registro"]').getBoundingClientRect(),f=e.querySelector('.MuiInputBase-root').getBoundingClientRect(),p=e.querySelector('td').getBoundingClientRect();return {button:{y:b.y,w:b.width,h:b.height},fieldY:f.y,presence:p.width};});
    assert.ok(dim.button.w>=44&&dim.button.h>=44);assert.ok(Math.abs(dim.button.y-dim.fieldY)<2,JSON.stringify(dim));assert.equal(dim.presence,124);
}
try {
    first=await session('owner');await control('delivery-seed');second=await session('helper');
    const a=first.page,b=second.page;
    const leaseTraffic=[];a.on('request',r=>{if(r.url().includes('/formatos/A/reservas'))leaseTraffic.push(new URL(r.url()).pathname);});
    await a.setViewportSize({width:1100,height:1000});
    for(const p of [a,b]) {await p.goto(origin+'/formatos/A');await tab(p,'Identificación general');}
    await check('barra horizontal nativa conserva reserva, texto y selección; otra identidad sigue bloqueada',async()=>{
        const f=input(a,'Trámite o servicio 1');await f.click();await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');await f.fill('Texto antes de desplazar');
        await a.keyboard.press('Home');await a.keyboard.down('Shift');await a.keyboard.press('ArrowRight');await a.keyboard.press('ArrowRight');await a.keyboard.up('Shift');const before=await selection(f);
        await a.evaluate(()=>{window.scrollEvents=[];for(const name of ['focusout','focusin'])document.addEventListener(name,e=>window.scrollEvents.push({event:name,target:e.target.tagName,related:e.relatedTarget?.tagName||null}),true);});
        const beforeTraffic=leaseTraffic.filter(p=>!p.endsWith('/actividad')).length;
        await dragHorizontal(a,'.identification-table');assert.deepEqual(await selection(f),before);await expect(f).toHaveValue('Texto antes de desplazar');
        await expect.poll(async()=>(await stored()).identificacion[0].tramite).toBe('Texto antes de desplazar');
        assert.equal(leaseTraffic.filter(p=>!p.endsWith('/actividad')).length,beforeTraffic,'Scrolling does not release or reacquire');
        await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');await expect(row(b,'identificacion:inicial')).toHaveAttribute('data-edit-state','occupied');
        await expect(input(b,'Trámite o servicio 1')).toHaveJSProperty('readOnly',true);await expect(row(b,'identificacion:inicial').getByRole('button',{name:'Eliminar registro',exact:true})).toBeDisabled();
        await expect(row(b,'identificacion:inicial').getByRole('group',{name:/Eliminar registro.*Persona sintética/})).toBeVisible();
        const current=await live(b),value=(await stored()).identificacion[0],version=current.bloques.find(v=>v.key==='identificacion:inicial').version;
        const req=mutation('identificacion:inicial',version,{...value,tramite:'Escritura ajena'});
        assert.equal((await request(b,'/formatos/A/reservas','POST',req)).status,409);assert.equal((await request(b,'/formatos/A/bloques','PATCH',req)).status,409);
        req.removal={section:'identificacion',id:'inicial'};req.blocks[0].value=null;assert.equal((await request(b,'/formatos/A/reservas','POST',req)).status,409);
        report.focusSequence=await a.evaluate(()=>window.scrollEvents);
        await f.focus();assert.deepEqual(await selection(f),before);await a.keyboard.type('TE');await expect(f).toHaveValue('TExto antes de desplazar');
    });
    await check('dos usuarios en filas distintas, presencia inicial estable y liberación al salir realmente',async()=>{
        await input(b,'Trámite o servicio 2').click();await expect(row(b,'identificacion:segundo')).toHaveAttribute('data-edit-state','owned');await input(b,'Trámite o servicio 2').fill('Otra fila independiente');
        await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');await expect(row(a,'identificacion:segundo').locator('td').first().getByRole('img',{name:'Participante: Colaboradora sintética',exact:true})).toBeVisible();
        await layout(a,'identificacion:inicial');await finish(b);
        await a.getByRole('tab',{name:'Identificación general',exact:true}).focus();await expect(row(b,'identificacion:inicial')).toHaveAttribute('data-edit-state','idle');await expect(input(b,'Trámite o servicio 1')).toHaveValue('TExto antes de desplazar');
        await input(b,'Trámite o servicio 1').click();await expect(row(b,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');await finish(b);
    });
    await check('pegado largo limitado a cuatro líneas y desplazamiento interno, rueda y barra vertical sin salida',async()=>{
        const f=input(a,'¿Qué entrega? 1');await first.context.grantPermissions(['clipboard-read','clipboard-write']);await a.evaluate(text=>navigator.clipboard.writeText(text),longText);
        await f.focus();await a.keyboard.press('Control+A');await a.keyboard.press('Control+V');await expect(f).toHaveValue(longText);await bounded(f);
        await f.evaluate(e=>{e.scrollTop=0;e.setSelectionRange(10,25);});const before=await selection(f);await f.hover();await a.mouse.wheel(0,150);
        await expect.poll(()=>f.evaluate(e=>e.scrollTop)).toBeGreaterThan(0);assert.deepEqual(await selection(f),before);
        const bounds=await f.boundingBox();await a.mouse.move(bounds.x+bounds.width-7,bounds.y+20);await a.mouse.down();await a.mouse.move(bounds.x+bounds.width-7,bounds.y+55,{steps:8});await a.mouse.up();
        await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');assert.deepEqual(await selection(f),before);
        await a.bringToFront();await a.setViewportSize({width:1100,height:800});
        await a.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
        const initialY=await a.evaluate(()=>scrollY);
        // El scrollbar raíz se procesa en el compositor. Recalcular el pulgar si el viewport acaba de cambiar.
        for(let attempt=0;attempt<3&&(await a.evaluate(()=>scrollY))===initialY;attempt++) {
            const pageBar=await a.evaluate(()=>{const h=document.documentElement.scrollHeight,v=innerHeight;return{x:innerWidth-7,y:scrollY/h*v+(v/h*v)/2,h,v,client:document.documentElement.clientWidth};});
            report.verticalBar=pageBar;await a.mouse.move(pageBar.x,pageBar.y);await a.mouse.down();await a.mouse.move(pageBar.x,initialY===0?Math.min(770,pageBar.y+200):Math.max(20,pageBar.y-200),{steps:20});await a.mouse.up();
            await expect.poll(()=>a.evaluate(()=>scrollY),{timeout:1000}).not.toBe(initialY).catch(()=>{});
        }
        assert.notEqual(await a.evaluate(()=>scrollY),initialY,'Native page scrollbar moved');
        await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');await expect(row(b,'identificacion:inicial')).toHaveAttribute('data-edit-state','occupied');
        // El portapapeles de Windows entrega CRLF; textarea representa esos saltos como LF.
        await expect.poll(async()=>(await stored()).identificacion[0].resultado.replace(/\r\n/g,'\n')).toBe(longText);await layout(a,'identificacion:inicial');
        await a.screenshot({path:path.join(artifacts,'tables-desktop.png'),fullPage:true});
        // Null focus is not an exit, including loss of focus to browser chrome.
        await f.focus();await f.evaluate(e=>e.blur());await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');
        await finish(a);await a.reload();await tab(a,'Identificación general');await expect(input(a,'¿Qué entrega? 1')).toHaveValue(longText);await bounded(input(a,'¿Qué entrega? 1'));
    });
    await check('primera tecla, Tab y selección sobreviven a la confirmación; ayuda conserva contexto',async()=>{
        const f=input(a,'Trámite o servicio 1');await f.focus();await a.keyboard.press('End');await a.keyboard.type(' teclado');await a.keyboard.press('Tab');
        await expect.poll(async()=>(await stored()).identificacion[0].tramite).toContain('teclado');await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');
        const help=a.getByRole('button',{name:'Ayuda: ¿Qué entrega?',exact:true});await help.click();await a.keyboard.press('Escape');await expect(help).toBeFocused();await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','owned');
        await a.getByRole('heading',{name:'Identificación general',exact:true}).click();await expect(row(a,'identificacion:inicial')).toHaveAttribute('data-edit-state','idle');
    });
    await check('Sistemas comparte presencia y conserva contexto en barras, menús, SII y campos condicionales',async()=>{
        await control('systems-sync');for(const p of [a,b]) {await p.reload();await tab(p,'Sistemas y herramientas');}
        await choose(a,'Sistema o herramienta','SIIv2');const module=input(a,'Módulo de SIIv2 1');await module.click();await module.fill('Consultas');await a.getByRole('option',{name:'Consultas · ID 3',exact:true}).click();
        await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');await dragHorizontal(a,'.systems-table');await expect(row(b,'sistemas:inicial')).toHaveAttribute('data-edit-state','occupied');
        await choose(a,'Sistema o herramienta','Otra (escríbela)');await input(a,'Nombre de la herramienta 1').fill(longText);await bounded(input(a,'Nombre de la herramienta 1'));
        await choose(a,'¿Para qué se usa?','Otro (escríbelo)');await input(a,'Describe el uso 1').fill(longText);await bounded(input(a,'Describe el uso 1'));await input(a,'Fallas o comentarios 1').fill(longText);await bounded(input(a,'Fallas o comentarios 1'));await layout(a,'sistemas:inicial');
        const help=a.getByRole('button',{name:'Ayuda: ¿Cómo funciona?',exact:true});await help.click();await a.getByRole('button',{name:'Cerrar ayuda',exact:true}).click();await expect(help).toBeFocused();await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');
        await row(b,'medios').getByRole('checkbox').first().click();await expect(row(b,'medios')).toHaveAttribute('data-edit-state','owned');await expect(row(b,'sistemas:inicial')).toHaveAttribute('data-edit-state','occupied');await finish(b);
        await finish(a);assert.equal((await stored()).sistemas[0].sistemaOtro,longText);assert.equal((await stored()).sistemas[0].usoOtro,longText);assert.equal((await stored()).sistemas[0].fallas,longText);
    });
    await check('un gesto iniciado sobre otra fila no la reserva; un toque completado sí cambia de registro',async()=>{
        await a.bringToFront();await input(a,'Fallas o comentarios 1').click();await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');
        const other=input(a,'Fallas o comentarios 2');await other.scrollIntoViewIfNeeded();const box=await other.boundingBox(),session=await first.context.newCDPSession(a);
        const x=box.x+box.width/2,y=box.y+box.height/2;
        await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y}]});
        for(let i=1;i<=6;i++)await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x,y:y-i*16}]});
        await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await session.detach();
        await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');await expect(row(a,'sistemas:segunda')).toHaveAttribute('data-edit-state','idle');
        await other.tap();await expect(row(a,'sistemas:segunda')).toHaveAttribute('data-edit-state','owned');await expect(row(b,'sistemas:inicial')).toHaveAttribute('data-edit-state','idle');await expect(row(b,'sistemas:segunda')).toHaveAttribute('data-edit-state','occupied');await finish(a);
    });
    await check('Eliminar arriba, rojo y 44 px; eliminación directa libre con confirmación y actualización en vivo',async()=>{
        await expect(row(a,'sistemas:segunda')).toHaveAttribute('data-edit-state','idle');const remove=row(a,'sistemas:segunda').getByRole('button',{name:'Eliminar registro',exact:true});
        await expect(remove).toBeEnabled();const color=await remove.evaluate(e=>getComputedStyle(e.querySelector('svg')).color);const [red,green,blue]=color.match(/\d+/g).map(Number);assert.ok(red>green+50&&red>blue+50,color);await layout(a,'sistemas:segunda');
        await a.getByRole('button',{name:'Agregar fila',exact:true}).focus();await a.keyboard.press('Shift+Tab');await expect(remove).toBeFocused();assert.notEqual(await remove.evaluate(e=>getComputedStyle(e).outlineStyle),'none');await expect(row(a,'sistemas:segunda')).toHaveAttribute('data-edit-state','idle');
        await remove.click();await a.getByRole('dialog').getByRole('button',{name:'Eliminar',exact:true}).click();await expect(row(a,'sistemas:segunda')).toHaveCount(0);await expect(row(b,'sistemas:segunda')).toHaveCount(0);
    });
    await check('móvil: Edición pegada al inicio, avatar accesible con teclado y tacto, desplazamiento táctil emulado',async()=>{
        await a.setViewportSize({width:390,height:844});await b.setViewportSize({width:390,height:844});
        const comments=input(a,'Fallas o comentarios 1');await comments.focus();await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');
        await dragHorizontal(a,'.systems-table');await layout(a,'sistemas:inicial');
        const sticky=await row(a,'sistemas:inicial').locator('td').first().boundingBox(),container=await a.locator('.systems-table').locator('..').boundingBox();assert.ok(Math.abs(sticky.x-container.x)<=2);
        const avatar=row(b,'sistemas:inicial').getByRole('img',{name:'Participante: Persona sintética',exact:true});await avatar.focus();await expect(b.getByRole('tooltip')).toHaveText('Persona sintética');await avatar.tap();await expect(b.getByRole('tooltip')).toHaveText('Persona sintética');
        const scroller=a.locator('.systems-table').locator('..');await scroller.evaluate(e=>{e.scrollLeft=0;e.scrollIntoView({block:'center'});});
        const box=await scroller.boundingBox(),session=await first.context.newCDPSession(a),y=Math.max(40,Math.min(700,box.y+100)),x=Math.min(340,box.x+280);
        await session.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y}]});for(let i=1;i<=6;i++)await session.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x-i*30,y}]});await session.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
        await expect.poll(()=>scroller.evaluate(e=>e.scrollLeft)).toBeGreaterThan(0);await expect(row(a,'sistemas:inicial')).toHaveAttribute('data-edit-state','owned');await expect(row(b,'sistemas:inicial')).toHaveAttribute('data-edit-state','occupied');
        assert.ok(await a.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await a.screenshot({path:path.join(artifacts,'tables-mobile.png'),fullPage:true});await session.detach();await finish(a);
    });
    await check('consulta enviada conserva íntegros los textos y permite recorrerlos; backend sigue rechazando escritura',async()=>{
        await control('recipients-submitted');await a.reload();await tab(a,'Sistemas y herramientas');const f=input(a,'Fallas o comentarios 1');await expect(f).toHaveJSProperty('readOnly',true);await expect(f).toHaveValue(longText);await bounded(f);
        await f.hover();await a.mouse.wheel(0,250);await expect.poll(()=>f.evaluate(e=>e.scrollTop)).toBeGreaterThan(0);await expect(row(a,'sistemas:inicial').getByRole('button',{name:'Eliminar registro',exact:true})).toBeDisabled();
        const current=await live(a),req=mutation('sistemas:inicial',current.bloques.find(v=>v.key==='sistemas:inicial').version,current.contenido.sistemas[0],crypto.randomUUID());assert.equal((await request(a,'/formatos/A/reservas','POST',req)).status,409);assert.equal((await request(a,'/formatos/A/bloques','PATCH',req)).status,409);
        await tab(a,'Identificación general');await expect(input(a,'¿Qué entrega? 1')).toHaveValue(longText);await bounded(input(a,'¿Qué entrega? 1'));
    });
    assert.deepEqual(report.errors,[]);report.passed=true;
} catch(e) {report.failure=e.stack;await first?.page.screenshot({path:path.join(artifacts,'tables-failure.png'),fullPage:true}).catch(()=>{});throw e;}
finally {await writeFile(path.join(artifacts,'table-scroll-browser.json'),JSON.stringify(report,null,2));await browser.close();}
