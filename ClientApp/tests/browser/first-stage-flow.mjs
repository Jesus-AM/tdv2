import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host sintético aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { synthetic: true, checks: [], errors: [] }, requests = [];
async function session() {
    const context = await browser.newContext({ ignoreHTTPSErrors: true, hasTouch: true, viewport: { width: 1366, height: 768 } });
    await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
        if (url.pathname === '/connect') {
            const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
            if (headers.location?.startsWith('https://login.microsoftonline.com/'))
                headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.continue();
    });
    const page = await context.newPage();
    page.on('pageerror', e => report.errors.push(e.message));
    page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith('/reservas/liberar')) requests.push(r.url()); });
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    await page.goto(origin + '/formatos/A'); await header(page);
    return { context, page };
}
async function header(page) {
    await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    // El HTML inicial precede a la revalidación de sesión/reservas; no interactuar durante esa carga.
    await expect(page.getByRole('combobox', { name: '¿A quién atiende? 1', exact: true })).not.toHaveAttribute('aria-readonly', 'true');
}
const users = page => page.getByRole('combobox', { name: '¿A quién atiende? 1', exact: true });
const priority = page => page.getByRole('combobox', { name: 'Prioridad 1', exact: true });
const row = page => page.locator('[data-edit-block="identificacion:inicial"]');
const option = (page, name) => page.getByRole('option', { name, exact: true });
async function finish(page) {
    await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toHaveAttribute('aria-busy', 'false');
    await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
    await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
}
const check = async (name, work) => { await work(); report.checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); };
const first = await session(), page = first.page;
async function stored() {
    const response = await first.context.request.post(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } });
    assert.equal(response.status(), 200); return (await response.json()).contenido.identificacion[0];
}
try {
    await check('valores iniciales vacíos; menú de usuarios espera reserva confirmada', async () => {
        await expect(users(page)).toHaveText('Selecciona los usuarios');
        await expect(priority(page)).toHaveText('Selecciona una prioridad');
        await expect(users(page)).not.toHaveAttribute('aria-readonly', 'true');
        let grant;
        const gate = new Promise(resolve => { grant = resolve; });
        await page.route('**/formatos/A/reservas', async route => { await gate; await route.continue(); });
        try {
            await users(page).click();
            await expect(page.getByText('Preparando edición…', { exact: true })).toBeVisible();
            await expect(page.getByRole('listbox')).toHaveCount(0);
            await expect(page.getByLabel('Trámite o servicio 1', { exact: true })).toHaveJSProperty('readOnly', true);
            grant(); await expect(page.getByRole('listbox')).toBeVisible();
            await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        } finally { grant(); await page.unroute('**/formatos/A/reservas'); }
    });
    await check('marcar/desmarcar, scroll y autoguardado conservan menú, foco y reserva', async () => {
        const releases = requests.length;
        await expect(page.getByRole('option')).toHaveCount(7);
        await option(page, 'Docentes').click(); await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'true');
        await option(page, 'Estudiantes').click(); await expect(option(page, 'Estudiantes')).toHaveAttribute('aria-selected', 'true');
        await option(page, 'Docentes').click(); await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'false');
        await option(page, 'Docentes').click();
        await page.getByRole('listbox').hover(); await page.mouse.wheel(0, 180);
        await expect.poll(async () => (await stored()).usuario).toEqual(['Docentes', 'Estudiantes']);
        await expect(page.getByRole('listbox')).toBeVisible(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        assert.equal(requests.length, releases);
        await page.screenshot({ path: path.join(artifacts, 'etapa-desktop.png'), fullPage: true });
        await page.keyboard.press('Escape'); await expect(users(page)).toBeFocused();
        await page.keyboard.press('Tab'); await expect(page.getByLabel('¿Qué entrega? 1', { exact: true })).toBeFocused();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await finish(page); await page.reload(); await header(page);
        await expect(users(page)).toHaveText('Docentes, Estudiantes');
        assert.equal((await stored()).prioridad, '');
    });
    await check('prioridad indicativa sólo en campo vacío; cinco opciones reales y persistencia', async () => {
        await priority(page).click(); await expect(page.getByRole('option')).toHaveCount(5);
        await expect(option(page, 'Selecciona una prioridad')).toHaveCount(0);
        await option(page, '4 · Alta').click(); await finish(page);
        await page.reload(); await header(page); await expect(priority(page)).toHaveText(/4\s*Alta/);
        assert.deepEqual((await stored()).usuario, ['Docentes', 'Estudiantes']);
        await priority(page).click(); await expect(option(page, 'Selecciona una prioridad')).toHaveCount(0);
        await page.keyboard.press('Escape'); await finish(page);
    });
    await check('dos sesiones: ocupación bloquea menús y eliminación; liberación habilita sin recargar', async () => {
        const other = await session();
        try {
            await users(page).click(); await option(page, 'Público en general').click();
            await expect(row(other.page)).toHaveAttribute('data-edit-state', 'occupied');
            await users(other.page).click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
            await priority(other.page).click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
            await expect(row(other.page).getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeDisabled();
            await page.keyboard.press('Escape'); await finish(page);
            await expect(row(other.page)).toHaveAttribute('data-edit-state', 'idle');
            await expect(users(other.page)).toHaveText('Docentes, Estudiantes, Público en general');
            await users(other.page).click(); await expect(row(other.page)).toHaveAttribute('data-edit-state', 'owned');
            await option(other.page, 'Público en general').click(); await other.page.keyboard.press('Escape'); await finish(other.page);
            await expect(users(page)).toHaveText('Docentes, Estudiantes');
            // El cambio confirmado y la liberación son notificaciones distintas: recibir texto no concede la fila.
            await expect(row(page)).toHaveAttribute('data-edit-state', 'idle');
        } finally { await other.context.close(); }
    });
    await check('teclado: selección múltiple sin cerrar, Escape restaura foco y vacío queda pendiente', async () => {
        await users(page).focus(); await page.keyboard.press('Enter');
        await option(page, 'Docentes').focus(); await page.keyboard.press('Space');
        await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'false');
        await page.keyboard.press('ArrowDown'); await expect(option(page, 'Estudiantes')).toBeFocused();
        await page.keyboard.press('Enter'); await expect(option(page, 'Estudiantes')).toHaveAttribute('aria-selected', 'false');
        await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(users(page)).toBeFocused(); await finish(page);
        assert.deepEqual((await stored()).usuario, []); assert.equal((await stored()).prioridad, '4');
        await page.reload(); await header(page); await expect(users(page)).toHaveText('Selecciona los usuarios');
    });
    await check('móvil táctil y movimiento reducido: casillas, textos y menú sin desbordamiento', async () => {
        await page.setViewportSize({ width: 360, height: 800 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        await expect(users(page)).not.toHaveAttribute('aria-readonly', 'true');
        await users(page).tap(); await option(page, 'Comunidad universitaria').tap(); await option(page, 'Personal administrativo').tap();
        await expect(page.getByRole('listbox')).toBeVisible(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        const bounds = await page.getByRole('listbox').boundingBox();
        assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= 360);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(artifacts, 'etapa-mobile.png'), fullPage: true });
        await page.keyboard.press('Escape'); await expect(users(page)).toBeFocused(); await finish(page);
        await page.reload(); await header(page); await expect(users(page)).toHaveText('Comunidad universitaria, Personal administrativo');
        assert.equal((await stored()).prioridad, '4');
    });
    async function control(action) {
        const response = await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } });
        assert.equal(response.status(), 200);
    }
    await check('primera etapa: tres pestañas, sin envío, ayudas táctiles consultables sin adquirir reserva', async () => {
        await expect(page.getByRole('tab')).toHaveCount(3);
        await expect(page.getByRole('checkbox', { name: 'Mostrar secciones posteriores' })).toHaveCount(0);
        await expect(page.getByText('Avance de la primera etapa', { exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: /Resolver|Confirmar propuesta|Enviar formato/ })).toHaveCount(0);
        const button = page.getByRole('button', { name: 'Ayuda: Trámite o servicio', exact: true });
        await button.tap(); await expect(page.getByRole('dialog')).toBeVisible();
        await expect(page.getByText('Gestión y seguimiento de proyectos institucionales', { exact: true })).toBeVisible();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'idle');
        const bounds = await page.getByRole('dialog').boundingBox(); assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= 360);
        await page.screenshot({ path: path.join(artifacts, 'etapa-ayuda-mobile.png'), fullPage: true });
        await page.getByRole('button', { name: 'Cerrar', exact: true }).click(); await expect(button).toBeFocused();
    });
    await check('catálogo definitivo, sin Otro/Externo ni captura adicional; nuevas categorías persisten', async () => {
        await page.setViewportSize({ width: 1366, height: 900 });
        await users(page).click();
        for(const label of ['Público en general','Instituciones públicas externas','Empresas y organizaciones privadas']) await option(page,label).click();
        await expect(option(page,'Otro')).toHaveCount(0); await expect(option(page,'Externo')).toHaveCount(0);
        await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(page.getByLabel(/Especifica a quién atiende/)).toHaveCount(0);
        await finish(page); await page.reload(); await header(page);
        assert.deepEqual((await stored()).usuario,['Comunidad universitaria','Personal administrativo','Público en general','Instituciones públicas externas','Empresas y organizaciones privadas']);
        assert.equal('usuarioOtro' in await stored(),false);
        const help=page.getByRole('button',{name:'Ayuda: ¿A quién atiende?',exact:true});
        await help.click(); await expect(page.getByText('Estudiantes · Docentes',{exact:true})).toBeVisible();
        await expect(page.getByText(/personas externas a la UACJ/i)).toBeVisible();
        await page.keyboard.press('Escape'); await expect(help).toBeFocused();
    });
    await check('primera escritura durante adquisición se conserva; Tab no libera y otra pestaña permanece bloqueada', async () => {
        const field = page.getByLabel('Trámite o servicio 1', { exact: true });
        let grant; const gate = new Promise(resolve => { grant = resolve; });
        await page.route('**/formatos/A/reservas', async route => { await gate; await route.continue(); });
        try {
            await field.click(); await field.pressSequentially('Primera escritura');
            await expect(field).toHaveValue(''); grant(); await expect(field).toHaveValue('Primera escritura');
            await page.keyboard.press('Tab'); await expect(users(page)).toBeFocused();
            await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        } finally { grant(); await page.unroute('**/formatos/A/reservas'); }
        const other = await first.context.newPage();
        try {
            await other.goto(origin + '/formatos/A'); await other.getByRole('tab', { name: 'Identificación general', exact: true }).click();
            await expect(row(other)).toHaveAttribute('data-edit-state', 'occupied');
            await expect(row(other).getByText(/Estás editando este registro en otra pestaña/)).toBeVisible();
            const help = other.getByRole('button', { name: 'Ayuda: ¿A quién atiende?', exact: true });
            await help.click(); await expect(other.getByRole('dialog')).toBeVisible();
            await expect(row(other)).toHaveAttribute('data-edit-state', 'occupied'); await other.keyboard.press('Escape');
            await finish(page); await expect(row(other)).toHaveAttribute('data-edit-state', 'idle');
            await expect(row(other).getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeEnabled();
        } finally { await other.close(); }
    });
    await check('ayudas con teclado/Escape conservan reserva, foco de retorno y texto; prioridad usa paleta exacta', async () => {
        const field = page.getByLabel('Trámite o servicio 1', { exact: true });
        await field.focus(); await expect(field).toHaveJSProperty('readOnly', false); await field.fill('Trámite de prueba');
        const releases = requests.length, button = page.getByRole('button', { name: 'Ayuda: Prioridad', exact: true });
        await button.focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('dialog')).toBeVisible();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await expect.poll(async () => (await stored()).tramite).toBe('Trámite de prueba');
        assert.equal(requests.length, releases);
        await expect(page.getByRole('dialog').getByText('Ejemplo', { exact: true })).toHaveCount(0);
        await expect(page.getByRole('dialog').getByText('Qué tan urgente es mejorar o automatizar el trámite:', { exact: true })).toBeVisible();
        await page.keyboard.press('Escape'); await expect(button).toBeFocused();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await priority(page).click();
        const expected = [['5 · Muy alta', 'rgb(185, 28, 28)'], ['4 · Alta', 'rgb(194, 65, 12)'], ['3 · Media', 'rgb(161, 98, 7)'], ['2 · Baja', 'rgb(30, 79, 168)'], ['1 · Puede esperar', 'rgb(71, 85, 105)']];
        for (const [label, color] of expected) {
            const circle = option(page, label).locator('span span').first();
            await expect(circle).toHaveCSS('background-color', color); await expect(circle).toHaveCSS('color', 'rgb(255, 255, 255)');
        }
        await expect(page.getByRole('option')).toHaveCount(5);
        await page.screenshot({ path: path.join(artifacts, 'etapa-prioridad.png'), fullPage: true });
        await page.keyboard.press('Escape'); await finish(page);
    });
    await check('tabla compacta: tamaños del tema, encabezados 700, texto largo y scroll contenido en 1920/1366/768/360', async () => {
        const field = page.getByLabel('Trámite o servicio 1', { exact: true });
        const long = 'Gestión y seguimiento de procedimientos institucionales para todas las áreas académicas y administrativas de la Universidad. '.repeat(3);
        await field.focus(); await expect(field).toHaveJSProperty('readOnly', false); await field.fill(long); await finish(page);
        const table=page.getByRole('table',{name:'Identificación general',exact:true});
        for(const width of [1920,1366,768,360]) {
            await page.setViewportSize({width,height:900});
            assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
        await expect(table.locator('th').first()).toHaveText('Código');
            for(const header of await table.locator('th').all()) {await expect(header).toHaveCSS('font-size','12px');await expect(header).toHaveCSS('font-weight','700');}
            await expect(field).toHaveCSS('font-size','13px'); await expect(field).toHaveCSS('font-weight','400');
            assert.ok(await field.evaluate(e=>e.scrollHeight<=e.clientHeight+2));
            const scroll=table.locator('..'); await scroll.evaluate(e=>{e.scrollLeft=e.scrollWidth;});
            const button=page.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0);await expect(button).toBeVisible();
            const box=await button.boundingBox();assert.ok(box.width>=(width<=600?40:30)&&box.height>=(width<=600?40:30));
            await scroll.evaluate(e=>{e.scrollLeft=0;});
            await page.evaluate(()=>window.scrollTo(0,0));
            await page.screenshot({path:path.join(artifacts,`tabla-${width}.png`),fullPage:true});
        }
        await page.setViewportSize({width:1366,height:768});
        const normalScale=await page.evaluate(()=>devicePixelRatio);
        await page.keyboard.press('Control+0');
        for(let i=0;i<5;i++)await page.keyboard.press('Control+Equal');
        report.nativeZoom200=await page.evaluate(()=>devicePixelRatio)/normalScale;
        if(report.nativeZoom200>1.9){
            await page.evaluate(()=>window.scrollTo(0,0));
            await page.screenshot({path:path.join(artifacts,'tabla-zoom-200.png'),fullPage:true});
            assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
        }
        await page.keyboard.press('Control+0');
        // Si Edge headless no aplica atajos del navegador, documentar el viewport equivalente y la validación manual pendiente.
        await page.setViewportSize({width:683,height:384});
        await page.evaluate(()=>window.scrollTo(0,0));
        await page.screenshot({path:path.join(artifacts,'tabla-viewport-200.png'),fullPage:true});
        assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
        await page.setViewportSize({width:1366,height:900});
        await field.focus();await expect(field).toHaveJSProperty('readOnly',false);await field.fill('Trámite de prueba');await finish(page);
    });
    await check('validación confirma código y Procedimiento ofrece V/A; ayuda conserva todos los campos de Sistemas', async () => {
        const validation = page.getByRole('combobox', { name: 'Validación 1', exact: true });
        await validation.click(); await option(page, 'V · Vigente').click(); await finish(page);
        const savedCode = (await stored()).codigo; assert.match(savedCode, /^PO-/);
        await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        await expect(page.getByLabel('Otro medio', { exact: true })).toBeVisible();
        const procedure = page.getByRole('combobox', { name: 'Procedimiento 1', exact: true });
        await procedure.click(); await expect(option(page, `${savedCode} · Trámite de prueba`)).toBeVisible();
        await option(page, `${savedCode} · Trámite de prueba`).click(); await finish(page);
        const help = page.getByRole('button', { name: 'Ayuda: Actividad (lista)', exact: true });
        await help.click(); await expect(page.getByRole('dialog').getByText(/Solo aparecen las marcadas como Vigente o Ajustar/)).toBeVisible();
        await page.keyboard.press('Escape'); await expect(help).toBeFocused();
        await expect(page.getByRole('heading', { name: 'Revisión de la primera etapa', exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: /Preguntas ·|Evaluación de|Acuerdos ·/ })).toHaveCount(0);
    });
    await check('eliminación espera renovación en vuelo, no emite nueva actividad y descarta respuestas anteriores', async () => {
        await header(page);
        // Recargar crea una sesión de pestaña nueva con actividad inicial verificable.
        await page.reload();await header(page);
        let release;const gate=new Promise(resolve=>{release=resolve;});let waiting=false, removalActivity=0;
        const observe=r=>{if(r.url().endsWith('/reservas/actividad')&&r.postDataJSON()?.removal)removalActivity++;};
        page.on('request',observe);
        await page.route('**/formatos/A/reservas/actividad',async route=>{const response=await route.fetch();waiting=true;await gate;await route.fulfill({response});});
        try {
            const input=page.getByLabel('Trámite o servicio 1',{exact:true});await input.focus();await expect(input).toHaveJSProperty('readOnly',false);
            await input.fill('Retiro durante renovación');await expect.poll(()=>waiting).toBe(true);
            await expect.poll(async()=>(await stored()).tramite).toBe('Retiro durante renovación');
            const remove=page.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0);await remove.click();
            const confirm=page.getByRole('button',{name:'Eliminar',exact:true});await confirm.click();await expect(confirm).toBeDisabled();
            assert.equal((await stored()).id,'inicial'); // Todavía no inicia el retiro.
            release();await expect(page.getByRole('dialog')).toHaveCount(0);await expect(row(page)).toHaveCount(0);
            await expect(page.locator('.form-actions [role=status]')).toHaveText('Guardado');
            await page.getByRole('tab',{name:'Sistemas y herramientas',exact:true}).click();
            await expect(page.getByLabel('Sistema o herramienta 1',{exact:true})).toBeVisible();
            assert.equal(removalActivity,0);
        } finally {release();page.off('request',observe);await page.unroute('**/formatos/A/reservas/actividad');}
    });
    await check('ILDA: eliminación directa sin enfocar, Cancelar intacto, cascada confirmada y no reaparición', async () => {
        await control('stage-ilda-seed'); await page.reload(); await header(page);
        const ilda = page.locator('[data-edit-block="identificacion:ilda:17"]'), remove = ilda.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0);
        await expect(ilda).toHaveAttribute('data-edit-state', 'idle'); await expect(remove).toBeEnabled();
        await remove.click(); await expect(page.getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click(); await expect(ilda).toHaveAttribute('data-edit-state', 'idle');
        assert.equal((await stored()).id, 'ilda:17');
        let activities=0;const observe=r=>{if(r.url().endsWith('/reservas/actividad'))activities++;};page.on('request',observe);
        await remove.click(); await page.getByRole('button', { name: 'Eliminar', exact: true }).click();
        await expect(ilda).toHaveCount(0); await expect(page.getByRole('dialog')).toHaveCount(0);
        await expect(page.locator('.form-actions [role=status]')).toHaveText('Guardado');
        page.off('request',observe);assert.equal(activities,0);
        await control('stage-ilda-republish'); await page.reload(); await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await expect(page.locator('[data-edit-block^="identificacion:"]')).toHaveCount(0);
    });
    await check('administrador: descripciones ocultas, interruptor inicia apagado y no altera permisos de otro perfil', async () => {
        await control('configuration-admin'); await page.goto(origin + '/inicio');
        await expect(page.getByRole('heading', { name: 'Procedimientos Institucionales', exact: true })).toBeVisible();
        await expect(page.getByText('Consulta las áreas y da seguimiento al llenado de sus formatos', { exact: true })).toHaveCount(0);
        await expect(page.getByText(/Puedes consultar todas las áreas y llenar los formatos de/)).toHaveCount(0);
        await page.goto(origin + '/formatos/A'); await expect(page.getByRole('tab')).toHaveCount(3);
        const toggle = page.getByRole('switch', { name: 'Mostrar secciones posteriores' });
        await expect(toggle).not.toBeChecked(); await toggle.check(); await expect(page.getByRole('tab')).toHaveCount(7);
        await page.getByRole('tab', { name: 'Datos', exact: true }).click(); await toggle.uncheck();
        await expect(page.getByRole('tab', { name: 'Contexto', exact: true })).toHaveAttribute('aria-selected', 'true');
        await page.reload(); await expect(page.getByRole('tab')).toHaveCount(3); await expect(toggle).not.toBeChecked();
        await expect(page.getByRole('progressbar', { name: 'Cargando estado de colaboración' })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeEnabled();
        await page.setViewportSize({ width: 1920, height: 1080 }); await page.screenshot({ path: path.join(artifacts, 'etapa-admin.png'), fullPage: true });
    });
    await check('históricos bloqueados y enviados muestran categorías originales y detalle sólo como consulta', async()=>{
        for(const action of ['recipients-history','recipients-submitted']) {
            await control(action);await page.reload();await page.getByRole('tab',{name:'Identificación general',exact:true}).click();
            await expect(users(page)).toHaveAttribute('aria-readonly','true');
            await expect(users(page)).toHaveText('Docentes, Otro, Externo');
            await expect(page.getByText('Otro: Detalle histórico sintético',{exact:true})).toBeVisible();
            await expect(page.getByLabel(/Especifica a quién atiende/)).toHaveCount(0);
            await expect(page.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeDisabled();
            await users(page).click();await expect(page.getByRole('listbox')).toHaveCount(0);
            assert.deepEqual((await stored()).usuario,['Docentes','Otro','Externo']);
            assert.equal((await stored()).usuarioOtro,'Detalle histórico sintético');
        }
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await writeFile(path.join(artifacts, 'first-stage-browser.json'), JSON.stringify(report, null, 2));
    await writeFile(path.join(artifacts, 'first-stage-reservations.json'), JSON.stringify({ releases: requests.length }, null, 2));
    await browser.close();
}
