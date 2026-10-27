import assert from 'node:assert/strict';
import { expect } from '@playwright/test';

export async function verifySignatures(page, selector, uacj, singleRowAt = 1200) {
    const footer = page.locator(selector);
    await expect(footer.locator('.institutional-signature strong')).toHaveText([
        'Tecnologías de la Información', 'Desarrollo de Sistemas', 'Tecnología Educativa', 'Operación y Redes', 'Inteligencia de Datos',
    ]);
    await expect(footer.locator('img')).toHaveCount(uacj ? 1 : 0);
    const values = await footer.evaluate(el => {
        const items = [...el.querySelectorAll('.institutional-signature')];
        return { background: getComputedStyle(el).backgroundColor, items: items.map(e => {
            const prefix = getComputedStyle(e.querySelector('span')), name = getComputedStyle(e.querySelector('strong'));
            return { size: prefix.fontSize, line: prefix.lineHeight, family: prefix.fontFamily, weight: prefix.fontWeight,
                strong: name.fontWeight, nameSize: name.fontSize, color: name.color, align: name.textAlign };
        }) };
    });
    assert.equal(values.background, 'rgb(33, 52, 85)');
    assert(parseFloat(values.items[0].size) > parseFloat(values.items[1].size));
    for (const item of values.items) {
        assert(item.family.includes('Noto Sans')); assert.equal(item.weight, '400'); assert.equal(item.strong, '700');
        assert.equal(item.size, item.nameSize); assert.equal(item.color, 'rgb(255, 255, 255)');
    }
    for (const item of values.items.slice(2)) assert.deepEqual(item, values.items[1]);
    if (page.viewportSize().width >= singleRowAt) {
        const centers = await footer.locator('.institutional-signatures > *').evaluateAll(es => es.map(e=>{const r=e.getBoundingClientRect();return r.y+r.height/2;}));
        assert(Math.max(...centers)-Math.min(...centers)<2, 'Todas las firmas caben en una franja en escritorio');
    }
    if (uacj) {
        assert(await footer.locator('img').evaluate(e => {const r=e.getBoundingClientRect();return e.complete && e.naturalWidth===1186 && Math.abs(r.width/r.height-e.naturalWidth/e.naturalHeight)<.02;}));
        const starts = await footer.locator('.institutional-signatures > *').evaluateAll(es => es.map(e => {const r=e.getBoundingClientRect();return {x:r.x,y:r.y,center:r.y+r.height/2,width:r.width};}));
        assert(Math.abs(starts[0].center-starts[1].center)<2 && starts[0].x<starts[1].x || starts[0].y<starts[1].y);
    }
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    return values;
}
