// React/MUI reales, transporte/identidades/reservas en memoria. Sin ASP.NET, bases o servicios institucionales.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const output = path.resolve('../.artifacts/capture-scroll-ui'); await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { mockedTransport: true, aspNet: false, database: false, checks: [], errors: [] };
const check = async (name, work) => { await work(); report.checks.push(name); console.log('UI PASS ' + name); };
try {
    for (const width of [1366, 390]) {
        const fixture = await systemFixture();
        fixture.content.identificacion = [fixture.content.identificacion[0]];
        fixture.content.sistemas[0] = { ...fixture.content.sistemas[0], proceso: 'PO-01', sistema: 'portal_cast', uso: 'consultar', estado: 'bien', fallas: 'Comentario anterior' };
        fixture.catalog = { estado: 'disponible', modulos: [{ id: '77', descripcion: 'Módulo sintético de seguimiento de solicitudes y atención a servicios académicos de la institución' }] };
        const context = await browser.newContext({ viewport: { width, height: 900 }, hasTouch: true, reducedMotion: 'reduce' });
        await fixture.webSockets(context);
        await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
        const page = await context.newPage(); page.setDefaultTimeout(15000);
        page.on('pageerror', error => report.errors.push(error.message));
        const tab = async name => { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); };
        const scroll = () => page.locator('.capture-row-scroll');
        const save = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); };
        const position = () => scroll().evaluate(el => [el.scrollLeft, el.scrollTop]);
        const visibleField = async field => {
            await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
            const geometry = await field.evaluate(el => {
                const r = el.getBoundingClientRect(), container = el.closest('[data-edit-scroll]'), s = container.getBoundingClientRect();
                const h = container.querySelector('th').getBoundingClientRect(), p = container.querySelector('th.row-editing-cell').getBoundingClientRect();
                return { top: r.top, bottom: r.bottom, header: h.bottom, bottomLimit: s.top + container.clientHeight,
                    left: r.left, right: r.right, presence: p.right, rightLimit: s.left + container.clientWidth,
                    navBottom: document.querySelector('header').getBoundingClientRect().bottom, viewport: innerHeight };
            });
            assert.ok(geometry.top >= geometry.header && geometry.bottom <= geometry.bottomLimit, JSON.stringify(geometry));
            assert.ok(geometry.top >= geometry.navBottom && geometry.bottom <= geometry.viewport, JSON.stringify(geometry));
            // En móvil el ancho disponible es menor al de algunas columnas; al menos el inicio debe quedar accesible.
            assert.ok(geometry.left >= geometry.presence - 1 && geometry.left < geometry.rightLimit - 30, JSON.stringify(geometry));
        };
        try {
            await page.goto(fixture.origin + '/formatos/A');
            await check(`${width}: CAST retirado, histórico legible y guardado independiente`, async () => {
                await tab('Sistemas y herramientas');
                const tool = page.getByRole('combobox', { name: 'Sistema o herramienta 1', exact: true });
                await expect(tool).toHaveText('Portal del CAST de uso de técnicos'); await tool.click();
                await expect(page.getByRole('listbox')).toBeVisible();
                await expect(page.getByRole('option', { name: /Portal del CAST/ })).toHaveCount(0);
                await page.keyboard.press('Escape');
                const comments = page.getByLabel('Áreas de oportunidad 1', { exact: true }); await comments.fill('Comentario actualizado');
                await save(); await page.reload(); await tab('Sistemas y herramientas');
                assert.equal(fixture.content.sistemas[0].sistema, 'portal_cast');
                await expect(tool).toHaveText('Portal del CAST de uso de técnicos'); await expect(comments).toHaveValue('Comentario actualizado');
                await tool.click(); await page.getByRole('option', { name: 'Hoja de cálculo (Excel)', exact: true }).click(); await save();
                await tool.click(); await expect(page.getByRole('listbox')).toBeVisible();
                await expect(page.getByRole('option', { name: /Portal del CAST/ })).toHaveCount(0); await page.keyboard.press('Escape'); await save();
            });
            await check(`${width}: tablas cortas naturales y encabezado/ayuda Procedimiento`, async () => {
                for (const name of ['Identificación general', 'Sistemas y herramientas']) {
                    await tab(name);
                    const sizes = await scroll().evaluate(el => ({ height: el.clientHeight, full: el.scrollHeight, table: el.querySelector('table').getBoundingClientRect().height }));
                    assert.ok(sizes.full - sizes.height <= 1 && sizes.height - sizes.table < 4, JSON.stringify(sizes));
                    await expect(scroll()).toHaveCount(1);
                }
                await tab('Identificación general'); await expect(page.getByLabel('Procedimiento 1', { exact: true })).toHaveValue('Actividad sintética');
                await page.getByRole('button', { name: 'Ayuda: Procedimiento', exact: true }).click();
                await expect(page.getByRole('dialog')).toContainText('Identifica el procedimiento que realiza tu área para atender una necesidad o cumplir una función.');
                await page.keyboard.press('Escape');
            });
            const originalProcedure = structuredClone(fixture.content.identificacion[0]);
            const originalTool = structuredClone(fixture.content.sistemas[0]);
            fixture.content.identificacion = Array.from({ length: 30 }, (_, i) => ({ ...originalProcedure, id: `p${i}`, codigo: `PO-${String(i + 1).padStart(2, '0')}`, tramite: `Procedimiento sintético ${i + 1} de atención y seguimiento de solicitudes` }));
            fixture.content.sistemas = Array.from({ length: 30 }, (_, i) => ({ ...originalTool, id: `s${i}`, proceso: fixture.content.identificacion[i].codigo,
                ...(i === 29 ? { sistema: 'sii_v2', moduloSiiId: '77', moduloSiiDescripcion: fixture.catalog.modulos[0].descripcion } : {}) }));
            fixture.delivery = () => ({ etapa: { id: 1, nombre: 'Primera etapa', estado: 'borrador', habilitada: true, ejercicio: 2026 },
                revision: { listo: false, pendientes: [
                    { seccion: 'identificacion', bloque: 'identificacion:p29', campo: 'tramite', mensaje: 'Identificación general · PO-30 · Procedimiento' },
                    { seccion: 'sistemas', bloque: 'sistemas:s29', campo: 'sistemaOtro', mensaje: 'Sistemas y herramientas · PO-30 · Nombre de la herramienta' },
                ] }, secciones: [], resumen: { procedimientos: 30, herramientas: 30, medios: [] } });
            await page.reload();
            for (const [section, title, fieldLabel] of [['identificacion', 'Identificación general', 'Procedimiento'], ['sistemas', 'Sistemas y herramientas', 'Áreas de oportunidad']]) {
                await check(`${width}: ${section} larga, dos ejes, capas, primera/última fila y reserva estable`, async () => {
                    await tab(title); const container = scroll();
                    const metrics = await container.evaluate(el => ({ height: el.clientHeight, full: el.scrollHeight, horizontal: el.scrollWidth > el.clientWidth, max: parseFloat(getComputedStyle(el).maxHeight) }));
                    assert.ok(metrics.full > metrics.height * 2 && metrics.horizontal && metrics.height <= metrics.max, JSON.stringify(metrics));
                    await expect(page.locator(`[data-add-row="${section}"]`)).toBeVisible();
                    assert.equal(await container.locator('[data-add-row]').count(), 0);
                    await container.evaluate(el => { el.scrollTop = el.scrollHeight; el.scrollLeft = el.scrollWidth; });
                    const corners = await container.evaluate(el => {
                        const rect = node => { const r = node.getBoundingClientRect(); return { top: r.top, bottom: r.bottom, left: r.left }; };
                        const head = el.querySelector('th'), presence = el.querySelector('tbody .row-editing-cell');
                        return { frame: rect(el), head: rect(head), last: rect(el.querySelector('tbody tr:last-child')),
                            headZ: Number(getComputedStyle(head).zIndex), presenceZ: Number(getComputedStyle(presence).zIndex) };
                    });
                    assert.ok(Math.abs(corners.head.top - corners.frame.top) < 3 && corners.headZ > corners.presenceZ);
                    assert.ok(corners.last.bottom <= corners.frame.bottom && corners.last.top > corners.head.bottom, JSON.stringify(corners));
                    const field = page.getByLabel(`${fieldLabel} 30`, { exact: true });
                    await field.click(); await expect(field.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'owned');
                    const initial = await position(); await field.fill('Captura que permanece al desplazar');
                    fixture.broadcast(); await expect(field).toHaveValue('Captura que permanece al desplazar');
                    assert.deepEqual(await position(), initial);
                    await container.evaluate(el => { el.scrollTop -= 100; el.scrollLeft -= 70; });
                    const afterScroll = await position(); fixture.broadcast();
                    await expect(field.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'owned');
                    assert.deepEqual(await position(), afterScroll); await save(); assert.deepEqual(await position(), afterScroll);
                    await container.evaluate(el => { el.scrollTop = 0; el.scrollLeft = 0; });
                    const first = page.getByLabel(`${fieldLabel} 1`, { exact: true }); await first.click(); await expect(first.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'owned');
                    await first.fill('Primera fila conservada'); await save();
                    await container.focus(); const beforeKey = (await position())[1]; await page.keyboard.press('PageDown');
                    await expect.poll(async () => (await position())[1]).toBeGreaterThan(beforeKey);
                    await container.hover(); const beforeWheel = (await position())[1]; await page.mouse.wheel(100, 200);
                    await expect.poll(async () => (await position())[1]).toBeGreaterThan(beforeWheel);
                    if (width < 600) {
                        const cdp = await context.newCDPSession(page);
                        const bounds = await container.boundingBox();
                        const x = Math.min(width - 35, bounds.x + bounds.width - 35), y = Math.min(700, bounds.y + bounds.height / 2);
                        const beforeTouch = (await position())[1];
                        await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
                        for (let step = 1; step <= 6; step++) await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x, y: y - step * 25 }] });
                        await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
                        await expect.poll(async () => (await position())[1]).toBeGreaterThan(beforeTouch);
                        await cdp.detach();
                    }
                    // Al llegar al final, la rueda continúa desplazando la página.
                    await container.evaluate(el => { el.scrollTop = el.scrollHeight - el.clientHeight - 40; window.scrollTo({ top: 0, behavior: 'instant' }); });
                    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
                    const wheelBounds = await container.boundingBox();
                    await page.mouse.move(wheelBounds.x + 20, wheelBounds.y + 100);
                    const outerBefore = await page.evaluate(() => scrollY);
                    report.scrollChain = await container.evaluate(el => {
                        const nodes = []; for (let node = el; node; node = node.parentElement) {
                            const s = getComputedStyle(node); nodes.push({ tag: node.tagName, role: node.getAttribute('role'), overflowY: s.overflowY,
                                top: node.scrollTop, height: node.clientHeight, full: node.scrollHeight });
                        } return nodes;
                    });
                    assert.equal(report.scrollChain.filter(node => ['auto', 'scroll', 'hidden'].includes(node.overflowY)).length, 2);
                    // Gesto con inicio/fin explícitos: no continúa el gesto mixto anterior del compositor.
                    const wheelSession = await context.newCDPSession(page);
                    await wheelSession.send('Input.synthesizeScrollGesture', { x: wheelBounds.x + 20, y: wheelBounds.y + 100,
                        yDistance: -300, gestureSourceType: 'mouse', speed: 600 });
                    await wheelSession.send('Input.synthesizeScrollGesture', { x: wheelBounds.x + 20, y: wheelBounds.y + 100,
                        yDistance: -300, gestureSourceType: 'mouse', speed: 600 });
                    await wheelSession.detach();
                    report.afterWheel = await container.evaluate(el => ({ top: el.scrollTop, full: el.scrollHeight, height: el.clientHeight, page: window.scrollY, rootTop: document.documentElement.scrollTop }));
                    await expect.poll(() => page.evaluate(() => scrollY)).toBeGreaterThan(outerBefore);
                    await page.screenshot({ path: path.join(output, `${section}-${width}.png`), fullPage: true });
                });
            }
            await check(`${width}: menús portales, dependientes y navegación hacia el último pendiente`, async () => {
                const last = page.locator('[data-edit-block="sistemas:s29"]');
                const procedure = last.getByRole('combobox', { name: 'Procedimiento 30', exact: true });
                await procedure.click(); await expect(procedure).toHaveAttribute('aria-expanded', 'true');
                assert.equal(await scroll().getByRole('listbox').count(), 0);
                await procedure.fill('PO-30'); await expect(page.getByRole('option')).toHaveCount(1); await page.keyboard.press('Escape');
                const module = last.getByRole('combobox', { name: 'Módulo de SIIv2 30', exact: true });
                if (width < 600) await module.tap(); else await module.click();
                await expect(module).toHaveAttribute('aria-expanded', 'true'); await expect(page.getByRole('option')).toHaveCount(1);
                assert.equal(await scroll().getByRole('listbox').count(), 0); await page.keyboard.press('Escape');
                await last.getByRole('combobox', { name: 'Sistema o herramienta 30', exact: true }).click();
                await page.getByRole('option', { name: 'Otra (escríbela)', exact: true }).click();
                await last.getByLabel('Nombre de la herramienta 30', { exact: true }).fill('Herramienta con nombre completo');
                await last.getByRole('combobox', { name: '¿Para qué se usa? 30', exact: true }).click();
                await page.getByRole('option', { name: 'Otro (escríbelo)', exact: true }).click(); await last.getByLabel('Describe el uso 30', { exact: true }).fill('Uso completo');
                await save();
                for (const [index, name] of [[0, 'Procedimiento 30'], [1, 'Nombre de la herramienta 30']]) {
                    await tab('Revisar y enviar'); await page.getByRole('button', { name: 'Ir al campo', exact: true }).nth(index).click();
                    const field = page.getByLabel(name, { exact: true }); await expect(field).toBeFocused(); await visibleField(field);
                    await field.fill('Corregido desde revisión'); await save();
                }
                assert.equal(fixture.content.sistemas[29].usoOtro, 'Uso completo');
            });
            await check(`${width}: nueva fila visible y foco sin perder captura anterior`, async () => {
                for (const [section, title] of [['identificacion', 'Identificación general'], ['sistemas', 'Sistemas y herramientas']]) {
                    await tab(title); await page.locator(`[data-add-row="${section}"]`).click();
                    const last = page.locator(`[data-edit-block^="${section}:"]`).last();
                    await expect(page.locator(`[data-edit-block^="${section}:"]`)).toHaveCount(31);
                    const firstField = last.locator('input:not([type="hidden"]), textarea, [role="combobox"]').first();
                    await expect(firstField).toBeFocused(); await visibleField(firstField);
                    await page.keyboard.press('Escape'); await save();
                    assert.equal(fixture.content[section].length, 31);
                }
                assert.equal(fixture.content.identificacion[0].tramite, 'Primera fila conservada');
                assert.equal(fixture.content.sistemas[0].fallas, 'Primera fila conservada');
                assert.equal(await scroll().getByText('Medios utilizados', { exact: true }).count(), 0);
                await expect(page.getByText('Medios utilizados', { exact: true })).toBeVisible();
                assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            });
        } catch (error) { await page.screenshot({ path: path.join(output, `failure-${width}.png`), fullPage: true }); throw error; }
        finally { await context.close(); await fixture.close(); }
    }
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.message; throw error; }
finally { await writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
