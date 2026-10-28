import assert from 'node:assert/strict';

// Comprueba el flujo normal con contenido real y con estados breves sintéticos.
export async function verifyFooterFlow(page, content, footer) {
    await page.evaluate(() => scrollTo(0, 0));
    const measure = () => page.evaluate(({ content, footer }) => {
        const main = document.querySelector(content).getBoundingClientRect();
        const foot = document.querySelector(footer).getBoundingClientRect();
        return { mainBottom: main.bottom, top: foot.top, bottom: foot.bottom,
            viewport: innerHeight, page: document.documentElement.scrollHeight,
            position: getComputedStyle(document.querySelector(footer)).position };
    }, { content, footer });
    const actual = await measure();
    assert(!['fixed', 'absolute'].includes(actual.position));
    assert(actual.top >= actual.mainBottom - 1, 'El pie sigue al contenido sin cubrirlo');
    assert(Math.abs(actual.bottom - actual.page) <= 2, 'No queda una franja vacía debajo');
    const result = { actual, short: [] };
    for (const text of ['', 'Cargando…', 'Sin resultados', 'No fue posible cargar los datos. Inténtalo de nuevo.']) {
        await page.locator(content).evaluate((el, text) => { el.replaceChildren(); if (text) { const p = document.createElement('p'); p.textContent = text; el.append(p); } }, text);
        // Menú completo y firmas apiladas pueden superar legítimamente la ventana
        // móvil. Medir la altura natural sin el relleno flexible, no asumir que caben.
        const minimum = await page.evaluate(({ content, footer }) => {
            const main = document.querySelector(content), previous = main.style.flex;
            main.style.flex = '0 0 auto';
            const bottom = document.querySelector(footer).getBoundingClientRect().bottom + scrollY;
            main.style.flex = previous; scrollTo(0, 0);
            return Math.max(innerHeight, bottom);
        }, { content, footer });
        const short = await measure();
        assert(short.top >= short.mainBottom - 1, 'El pie breve tampoco cubre contenido');
        assert(Math.abs(short.bottom - minimum) <= 2, 'Pie al fondo o después del contenido cuando no cabe');
        assert(short.page <= minimum + 2, 'Sin desplazamiento artificial');
        result.short.push({ text, minimum, ...short });
    }
    // Restauración completa del documento real; nunca persiste estados sintéticos.
    await page.reload();
    return result;
}
