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
        const short = await measure();
        assert(Math.abs(short.bottom - short.viewport) <= 2, 'Pie al fondo con contenido breve');
        assert(short.page <= short.viewport + 2, 'Sin desplazamiento artificial');
        result.short.push({ text, ...short });
    }
    // Restauración completa del documento real; nunca persiste estados sintéticos.
    await page.reload();
    return result;
}
