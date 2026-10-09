import assert from 'node:assert/strict';
import { expect } from '@playwright/test';
import path from 'node:path';

// Sólo se invoca dentro del host sintético: cookies, SignalR y PostgreSQL reales; ninguna identidad institucional.
export async function verifyDirectDeletion({ check, page, context, browser, origin, control, stored, finish, savedAfter, isolateMicrosoft, errors, artifacts }) {
    const dialog = target => target.getByRole('dialog', { name: 'Eliminar registro' });
    const remove = target => target.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0);
    const confirm = target => dialog(target).getByRole('button', { name: 'Eliminar', exact: true });
    const header = async target => {
        await target.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await expect(target.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeEnabled();
    };
    const reset = async () => { await finish(); await control('ready-to-submit'); await page.reload(); await header(page); };
    const mutations = request => /\/formatos\/A\/(reservas|bloques)$/.test(new URL(request.url()).pathname) && ['POST', 'PATCH'].includes(request.method());

    await check('eliminación directa en cuatro tablas sin enfocar campos: reserva al confirmar, espera guardado y libera', async () => {
        for (const [tab, section, label] of [
            ['Identificación general', 'identificacion', 'Eliminar registro'],
            ['Sistemas y herramientas', 'sistemas', 'Eliminar registro'],
            ['Datos', 'datos', 'Eliminar registro'], ['Acuerdos', 'acuerdos', 'Eliminar registro'],
        ]) {
            await reset(); await page.getByRole('tab', { name: tab, exact: true }).click();
            const before = await stored(), requests = [];
            const capture = request => { if (mutations(request)) requests.push({ method: request.method(), body: request.postDataJSON() }); };
            page.on('request', capture);
            try {
                const button = page.getByRole('button', { name: label, exact: true }).first(), row = button.locator('xpath=ancestor::tr');
                await expect(row).toHaveAttribute('data-edit-state', 'idle'); await expect(button).toBeEnabled();
                await expect(button).toHaveCSS('color', 'rgb(211, 47, 47)');
                await button.focus(); await page.keyboard.press('Enter');
                await expect(dialog(page)).toBeVisible();
                await expect(dialog(page).getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
                assert.equal(requests.length, 0); // Ni el foco del icono ni abrir el diálogo adquieren reservas.
                let releaseResponse; const gate = new Promise(resolve => { releaseResponse = resolve; });
                let committed = false;
                await page.route('**/formatos/A/bloques', async route => {
                    const response = await route.fetch(); committed = true; await gate; await route.fulfill({ response });
                });
                try {
                    await confirm(page).click(); await expect.poll(() => committed).toBe(true);
                    await expect(dialog(page)).toBeVisible(); await expect(confirm(page)).toBeDisabled();
                    await expect(dialog(page).getByRole('button', { name: 'Cancelar', exact: true })).toBeDisabled();
                    const received = page.waitForResponse(r => new URL(r.url()).pathname.endsWith('/bloques'));
                    releaseResponse(); assert.equal((await received).status(), 200);
                    await expect(dialog(page)).toHaveCount(0);
                } finally { releaseResponse(); await page.unroute('**/formatos/A/bloques'); }
                const after = await stored(); assert.equal(after.version, before.version + 1); assert.equal(after.contenido[section].length, 0);
                assert.equal(requests.filter(r => r.method === 'PATCH').length, 1);
                const reservation = requests.find(r => r.method === 'POST').body;
                const live = await (await context.request.get(`${origin}/formatos/A/estado?tab=${reservation.tabId}`)).json();
                assert.ok(live.bloques.filter(b => reservation.blocks.some(r => r.key === b.key)).every(b => !b.reserva));
                if (section === 'identificacion') {
                    assert.deepEqual(after.contenido.evaluaciones, {});
                    assert.equal(after.contenido.sistemas[0].proceso, ''); assert.equal(after.contenido.datos[0].proceso, '');
                }
            } finally { page.off('request', capture); }
        }
    });

    await check('otra pestaña bloquea Eliminar y su confirmación; liberar registro y relación lo habilita sin recargar', async () => {
        await reset(); const other = await context.newPage();
        try {
            await other.goto(origin + '/formatos/A'); await header(other);
            await remove(page).click();
            const input = other.getByLabel('Trámite / servicio 1', { exact: true });
            await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
            await expect(confirm(page)).toBeDisabled();
            await expect(dialog(page).getByText('Estás editando este registro en otra pestaña.', { exact: true })).toBeVisible();
            await dialog(page).getByRole('button', { name: 'Cancelar', exact: true }).click();
            await expect(remove(page)).toBeDisabled();
            await finish(other); await expect(remove(page)).toBeEnabled();
            await other.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
            const system = other.locator('#panel-sistemas textarea').first(); await system.focus(); await expect(system).toHaveJSProperty('readOnly', false);
            await expect(remove(page)).toBeDisabled();
            await remove(page).locator('..').hover();
            await expect(page.getByRole('tooltip')).toContainText('un registro relacionado en otra pestaña');
            await finish(other); await expect(remove(page)).toBeEnabled();
        } finally { await other.close(); }
    });

    await check('cambios de contenido o relaciones durante la confirmación exigen revisar y no eliminan otra versión', async () => {
        await reset(); const other = await context.newPage();
        try {
            await other.goto(origin + '/formatos/A'); await header(other);
            for (const related of [false, true]) {
                await remove(page).click();
                await other.getByRole('tab', { name: related ? 'Sistemas y herramientas' : 'Identificación general', exact: true }).click();
                const input = related ? other.locator('#panel-sistemas textarea').first() : other.getByLabel('Trámite / servicio 1', { exact: true });
                await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
                await savedAfter(() => input.fill(related ? 'Relación actualizada por otra pestaña' : 'Proceso actualizado por otra pestaña'), 200, other);
                await finish(other);
                await expect(dialog(page).getByText(/Cancela y revisa la versión vigente/)).toBeVisible();
                await expect(confirm(page)).toBeDisabled();
                await dialog(page).getByRole('button', { name: 'Cancelar', exact: true }).click();
                assert.equal((await stored()).contenido.identificacion.length, 1);
            }
        } finally { await other.close(); }
    });

    await check('versión recibida mientras prepare espera no elimina; libera reservas adquiridas al rechazar la confirmación antigua', async () => {
        await reset(); const other = await context.newPage();
        let release; const gate = new Promise(resolve => { release = resolve; }); let waiting = false;
        await page.route('**/formatos/A/reservas', async route => { waiting = true; await gate; await route.continue(); });
        try {
            await other.goto(origin + '/formatos/A'); await header(other);
            await remove(page).click(); await confirm(page).click(); await expect.poll(() => waiting).toBe(true);
            const input = other.getByLabel('Trámite / servicio 1', { exact: true });
            await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
            await savedAfter(() => input.fill('Versión guardada durante la adquisición'), 200, other); await finish(other);
            release();
            await expect(dialog(page).getByText(/Cancela y revisa la versión vigente/)).toBeVisible(); await expect(confirm(page)).toBeDisabled();
            assert.equal((await stored()).contenido.identificacion[0].tramite, 'Versión guardada durante la adquisición');
            await dialog(page).getByRole('button', { name: 'Cancelar', exact: true }).click();
            await expect(remove(other)).toBeEnabled(); // SignalR comunica también la liberación de las relaciones.
        } finally { release(); await page.unroute('**/formatos/A/reservas'); await other.close(); }
    });

    await check('dos sesiones confirman eliminación simultánea: una reserva gana, otra muestra titular y sólo hay un guardado', async () => {
        await reset(); const independent = await browser.newContext({ ignoreHTTPSErrors: true }); await isolateMicrosoft(independent);
        const other = await independent.newPage(); other.on('pageerror', error => errors.push(error.message));
        let releaseReserve, releaseSave;
        const reserveGate = new Promise(resolve => { releaseReserve = resolve; }), saveGate = new Promise(resolve => { releaseSave = resolve; });
        let arrived = 0, patches = 0; const statuses = [], pages = [page, other];
        try {
            await other.goto(origin + '/connect'); await other.waitForURL(origin + '/inicio');
            const cookie = cookies => cookies.find(c => c.name === 'tdv2.aspnet.session')?.value;
            assert.notEqual(cookie(await context.cookies()), cookie(await independent.cookies()));
            await other.goto(origin + '/formatos/A'); await header(other);
            const before = await stored();
            for (const target of pages) {
                await remove(target).click();
                await target.route('**/formatos/A/reservas', async route => {
                    arrived++; await reserveGate; const response = await route.fetch(); statuses.push({ target, status: response.status() }); await route.fulfill({ response });
                });
                await target.route('**/formatos/A/bloques', async route => { patches++; await saveGate; await route.continue(); });
            }
            await Promise.all(pages.map(target => confirm(target).click())); await expect.poll(() => arrived).toBe(2); releaseReserve();
            await expect.poll(() => statuses.length).toBe(2); assert.deepEqual(statuses.map(r => r.status).sort(), [200, 409]);
            const loser = statuses.find(r => r.status === 409).target;
            await expect(dialog(loser).getByText('Persona sintética está editando este registro.', { exact: true })).toBeVisible();
            await expect(confirm(loser)).toBeDisabled(); await expect.poll(() => patches).toBe(1);
            await loser.screenshot({ path: path.join(artifacts, 'eliminacion-reserva-ajena.png'), fullPage: true });
            releaseSave(); await expect.poll(async () => (await stored()).contenido.identificacion.length).toBe(0);
            assert.equal((await stored()).version, before.version + 1); assert.equal(patches, 1);
            await dialog(loser).getByRole('button', { name: 'Cancelar', exact: true }).click();
            for (const target of pages) await expect(dialog(target)).toHaveCount(0);
        } finally {
            releaseReserve(); releaseSave();
            for (const target of pages) { await target.unroute('**/formatos/A/reservas'); await target.unroute('**/formatos/A/bloques'); }
            await independent.close();
        }
    });

    await check('fallo de guardado al eliminar conserva propuesta contextual y reintenta sin duplicar la operación', async () => {
        await reset(); await control('fail-save');
        try {
            await remove(page).click(); await savedAfter(() => confirm(page).click(), 503);
            await expect(dialog(page)).toHaveCount(0);
            await expect(page.getByText(/No se confirmó la eliminación/)).toBeVisible();
            await expect(page.getByRole('button', { name: /Resolver/ })).toHaveCount(0);
            assert.equal((await stored()).contenido.identificacion.length, 1);
        } finally { await control('recover-save'); }
        await expect.poll(async () => (await stored()).contenido.identificacion.length, { timeout: 15000 }).toBe(0);
        await expect(page.getByRole('dialog')).toHaveCount(0); assert.equal((await stored()).contenido.identificacion.length, 0);
        await expect(page.getByRole('button', { name: 'Resolver eliminación', exact: true })).toHaveCount(0);
    });
    await reset();
}
