import assert from 'node:assert/strict';
import { expect } from '@playwright/test';
import { watchStream, streamState } from './watch-stream.mjs';

// Usa exclusivamente los controles del host de pruebas, identidad sintética y PostgreSQL desechable.
export async function verifyCentralCollaboration({ page, context, origin, control, check }) {
    await check('Nexo central sin colaboración local muestra sólo su formato y permite autoguardado', async () => {
        await control('central-collaborator'); await page.goto(origin + '/inicio');
        await expect(page.getByRole('button', { name: 'Continuar llenado', exact: true })).toHaveCount(1);
        await expect(page.getByRole('button', { name: 'Colaboradores', exact: true })).toHaveCount(0);
        await page.getByRole('button', { name: 'Continuar llenado', exact: true }).click();
        await page.waitForURL(origin + '/formatos/A3');
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const input = page.getByLabel('Trámite / servicio 1', { exact: true });
        await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
        const response = page.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A3/bloques' && r.request().method() === 'PATCH');
        await input.fill('Captura central sintética'); assert.equal((await response).status(), 200);
        await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toHaveAttribute('aria-busy', 'false');
        await page.getByRole('tab', { name: 'Acuerdos', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toHaveCount(0);
        assert.equal((await context.request.get(origin + '/formatos/B')).status(), 403);
    });
    await check('retiro central durante edición termina SignalR, bloquea reserva/guardado y conserva la versión compartida', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const input = page.getByLabel('Trámite / servicio 1', { exact: true });
        await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
        const stream = await watchStream(page, 'A3');
        let release; const gate = new Promise(resolve => { release = resolve; }); let pending = false;
        await page.route('**/formatos/A3/bloques', async route => { pending = true; await gate; await route.continue(); });
        try {
            await input.fill('Propuesta pendiente tras retiro'); await expect.poll(() => pending).toBe(true);
            await control('central-remove');
            const response = page.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A3/bloques');
            release(); assert.equal((await response).status(), 403);
            await expect(input).toHaveValue('Propuesta pendiente tras retiro'); await expect(input).toHaveJSProperty('readOnly', true);
            await expect.poll(async () => (await streamState(page, stream)).completed, { timeout: 20000 }).toBe(true);
            assert.ok((await streamState(page, stream)).error);
            assert.equal((await context.request.get(origin + '/formatos/A3')).status(), 403);
            await control('central-collaborator');
            const live = await (await context.request.get(origin + '/formatos/A3/estado?tab=' + crypto.randomUUID())).json();
            assert.equal(live.contenido.identificacion[0].tramite, 'Captura central sintética');
        } finally { release(); await page.unroute('**/formatos/A3/bloques'); }
    });
    await check('identidad sin empleado explica la ausencia de formatos sin conceder alcance', async () => {
        await control('central-no-employee');
        // La propuesta anterior ya fue comprobada; este cambio de documento sólo ocurre en el fixture.
        page.once('dialog', dialog => dialog.accept()); await page.goto(origin + '/inicio');
        await expect(page.getByText('No se pudo validar tu cuenta individual y número de empleado. Solicita su revisión en Nexo.', { exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Continuar llenado', exact: true })).toHaveCount(0);
    });
    await control('central-restore-responsible'); await page.goto(origin + '/formatos/A');
}
