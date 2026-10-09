import assert from 'node:assert/strict';
import { expect } from '@playwright/test';
import path from 'node:path';

export async function verifyParticipants({ page, context, origin, check, finish, artifacts, touch = false }) {
    await check('primera escritura confirmada, inserción de texto, selección y Tab conservan foco y reserva', async () => {
        await finish(); await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const input = page.getByLabel('Trámite / servicio 1', { exact: true });
        await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
        const requests = [];
        const capture = r => { if (/\/reservas(?:\/liberar)?$/.test(new URL(r.url()).pathname)) requests.push(r.url()); };
        page.on('request', capture);
        try {
            await input.fill('ABCD'); await input.evaluate(el => el.setSelectionRange(2, 2));
            await input.press('x'); await expect(input).toHaveValue('ABxCD');
            assert.equal(await input.evaluate(el => el.selectionStart), 3);
            // Input.insertText ejercita la inserción múltiple sin leer ni sustituir el portapapeles del operador.
            await page.keyboard.insertText(' área '); await expect(input).toHaveValue('ABx área CD');
            const next = page.getByRole('combobox', { name: 'Usuarios que atiende 1', exact: true });
            await page.keyboard.press('Tab'); await expect(next).toBeFocused();
            await expect(input.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'owned');
            assert.equal(requests.length, 0);
            await finish(); await page.getByRole('tab', { name: 'Datos', exact: true }).click();
            await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
            await expect(input).toHaveValue('ABx área CD');
        } finally { page.off('request', capture); }
    });
    await check('foto protegida y avatar: caché entre filas, tooltip sin perder cursor ni liberar reserva', async () => {
        await finish();
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const field = page.getByLabel('Trámite / servicio 1', { exact: true });
        await field.focus(); await expect(field).toHaveJSProperty('readOnly', false);
        const row = field.locator('xpath=ancestor::tr'), avatar = row.locator('[data-participant-avatar]');
        await expect(avatar.locator('img')).toHaveAttribute('src', /^data:image\/png;base64,/);
        const color = await row.evaluate(el => getComputedStyle(el).getPropertyValue('--participant-color'));
        const changes = [];
        const capture = request => { if (/\/formatos\/A\/(reservas|participantes)/.test(new URL(request.url()).pathname)) changes.push(request.url()); };
        page.on('request', capture);
        try {
            await field.evaluate(el => el.setSelectionRange(1, 3));
            await avatar.hover(); await expect(page.getByRole('tooltip')).toHaveText('Persona sintética');
            await avatar.click(); await expect(field).toBeFocused();
            if (touch) { await avatar.tap(); await expect(field).toBeFocused(); await expect(page.getByRole('tooltip')).toHaveText('Persona sintética'); }
            assert.deepEqual(await field.evaluate(el => [el.selectionStart, el.selectionEnd]), [1, 3]);
            await expect(row).toHaveAttribute('data-edit-state', 'owned');
            await page.mouse.move(0, 0);
            await avatar.focus(); await expect(page.getByRole('tooltip')).toHaveText('Persona sintética');
            await expect(row).toHaveAttribute('data-edit-state', 'owned');
            await field.focus(); await expect(field).toHaveJSProperty('readOnly', false);
            assert.equal(changes.length, 0);
            const other = await context.newPage();
            try {
                await other.goto(origin + '/formatos/A');
                await other.getByRole('tab', { name: 'Identificación general', exact: true }).click();
                const occupied = other.locator('[data-edit-state=occupied]').first();
                await expect(occupied.locator('img')).toHaveAttribute('src', /^data:image\/png;base64,/);
                assert.equal(await occupied.evaluate(el => getComputedStyle(el).getPropertyValue('--participant-color')), color);
                await expect(occupied).toContainText('Estás editando este registro en otra pestaña');
                for (const [width, height] of [[1920,1080], [1366,768], [768,1024], [390,844]]) {
                    await other.setViewportSize({ width, height });
                    assert.equal(await other.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                    await other.screenshot({ path: path.join(artifacts, `captura-avatar-${width}.png`), fullPage: false });
                }
                await other.emulateMedia({ reducedMotion: 'reduce' });
                await expect(occupied).toHaveCSS('transition-duration', '0s');
                const image = occupied.locator('[data-participant-avatar]');
                if (touch) await image.tap();
                else {
                    await image.dispatchEvent('pointerdown', { pointerType: 'touch', pointerId: 1 });
                    await image.dispatchEvent('pointerup', { pointerType: 'touch', pointerId: 1 });
                }
                await expect(other.getByRole('tooltip')).toHaveText('Persona sintética');
                await page.getByRole('tab', { name: 'Contexto', exact: true }).click();
                await expect(other.locator('[data-edit-state=occupied]')).toHaveCount(0);
                await expect(other.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeEnabled();
                await expect(other.locator('[data-participant-avatar]')).toHaveCount(0);
            } finally { await other.close(); }
        } finally { page.off('request', capture); }
    });
}
