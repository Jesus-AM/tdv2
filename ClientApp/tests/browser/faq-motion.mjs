import assert from 'node:assert/strict';
import { expect } from '@playwright/test';

// Examina la geometría y el texto en puntos intermedios de las transiciones CSS reales.
export async function verifyFaqMotion(page, width) {
    const items = page.locator('#preguntas details');
    const answers = await items.locator('p').allTextContents();
    const byLength = answers.map((text, index) => ({ index, length: text.length })).sort((a, b) => a.length - b.length);
    const tested = [];
    for (const {index} of [byLength.at(-1), byLength[0]]) {
        const item = items.nth(index), summary = item.locator('summary');
        await page.emulateMedia({reducedMotion:'reduce'});
        const originallyOpen = await item.evaluate(el => el.open);
        if (!originallyOpen) await summary.press('Enter');
        await page.emulateMedia({reducedMotion:'no-preference'});
        await summary.focus();
        const result = await item.evaluate(async details => {
            const question = details.querySelector('summary'), answer = details.querySelector('p');
            const indicator = question.querySelector('.pm');
            function sample() {
                const box = details.getBoundingClientRect(), questionBox = question.getBoundingClientRect();
                return {
                    height:box.bottom - questionBox.bottom - details.clientTop,
                    below:details.nextElementSibling?.getBoundingClientRect().top - questionBox.top,
                    visible:answer.checkVisibility(),
                    indicator:Number(getComputedStyle(indicator, '::after').opacity),
                };
            }
            const full = sample();
            const transitions = [];
            // El siguiente clic llega antes de que termine el anterior en las tres primeras inversiones.
            for (const fraction of [0.5, 0.5, 0.5, 1, 1]) {
                const before = sample();
                question.click();
                const start = sample();
                const goal = details.open ? full.height : 0;
                const samples = [];
                const deadline = performance.now() + 3000;
                do {
                    await new Promise(requestAnimationFrame);
                    samples.push(sample());
                    if (performance.now() > deadline) throw new Error('La respuesta no llegó a su altura final.');
                } while (Math.abs(goal - samples.at(-1).height) > Math.abs(goal - start.height) * (1 - fraction));
                transitions.push({opening:details.open, before, start, samples, fraction});
            }
            return {full, transitions};
        });
        const {full, transitions} = result;
        assert(full.height > 0);
        for (const {opening, before, start, samples, fraction} of transitions) {
            const end = samples.at(-1);
            assert(Math.abs(start.height - before.height) < 1, 'Continuidad al invertir la transición');
            assert(opening ? end.height > start.height : end.height < start.height);
            if (fraction < 1) assert(end.height > 0 && end.height < full.height);
            for (const frame of samples) {
                if (frame.height > 0) assert(frame.visible, 'La respuesta conserva el texto mientras ocupa espacio');
                if (Number.isFinite(full.below)) assert(Math.abs(frame.below - full.below - frame.height + full.height) < 1);
                assert(Math.abs(frame.indicator - (1 - frame.height / full.height)) < 0.025, 'Indicador sincronizado');
            }
        }
        assert.equal(transitions.at(-2).samples.at(-1).height, full.height);
        assert.equal(transitions.at(-1).samples.at(-1).height, 0);
        await summary.scrollIntoViewIfNeeded();
        const box = await summary.boundingBox();
        const center = [box.x + box.width / 2, box.y + box.height / 2];
        for (let click = 0; click < 3; click++) {
            if (width < 500) await page.touchscreen.tap(...center);
            else await page.mouse.click(...center);
        }
        await expect(item).toHaveAttribute('open', '');
        await expect(summary).toBeFocused();
        await summary.press('Enter');
        await page.emulateMedia({reducedMotion:'reduce'});
        await summary.press('Space');
        await expect(item.locator('p')).toBeVisible();
        assert(await item.evaluate(el => el.open && !el.getAnimations({subtree:true}).length));
        await summary.press('Space');
        await expect(summary).toBeFocused();
        assert(await item.evaluate(el => !el.open && !el.querySelector('p').checkVisibility() && !el.getAnimations({subtree:true}).length));
        if (originallyOpen) await summary.press('Enter');
        tested.push({index, height:full.height});
    }
    return tested;
}
