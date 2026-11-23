import assert from 'node:assert/strict';
import { expect } from '@playwright/test';

// Cada caso se ejecuta con un clúster desechable, una sesión sintética y el backend real.
export async function verifyLastAnswer({ page, origin, base, getSurvey, api, scenario, auditCount }) {
    assert.match(scenario, /^(m365|ciber)-(inicial|na)$/);
    const [blockId, choice] = scenario.split('-');
    const rubric = blockId === 'ciber', initial = choice === 'inicial';
    const surveyUrl = base + '/encuesta';
    await page.setViewportSize({ width: initial ? 1366 : 390, height: 900 });
    await page.goto(origin + '/connect?returnUrl=' + encodeURIComponent(surveyUrl));
    await expect(page.getByRole('dialog')).toBeVisible();
    let view = await getSurvey();
    assert.equal((await api('POST', '/api/aviso', { version: view.instrument.notice.version })).status(), 200);
    view = await getSurvey();
    const profile = Object.fromEntries(view.instrument.profile.map(f => [f.id, f.multiple ? [f.options[0]] : f.options[0]]));
    async function save(answers) {
        const response = await api('PUT', '/api/encuesta', { version: view.version, profile, answers });
        assert.equal(response.status(), 200);
        view = await response.json();
    }
    await save({});
    for (const block of view.instrument.modules) {
        for (const tool of block.tools) if (!tool.opt && !tool.doc) view.answers[tool.id] = { state: 'na' };
        await save(view.answers);
    }
    const toolId = rubric ? 'c-contrasenas-y-autenticacion' : 'm-excel';
    const distribution = async () => {
        const response = await page.request.get(origin + base + '/api/resultados?unit=A');
        assert.equal(response.status(), 200);
        return (await response.json()).tools.find(t => t.id === toolId);
    };
    const before = await distribution(), saves = await auditCount();
    view.answers[toolId] = { state: 'pending' };
    await save(view.answers);
    assert.deepEqual(view.evaluation.pending, [toolId]);
    await page.goto(origin + surveyUrl + '?paso=' + blockId);
    const card = page.locator('#' + toolId), chip = card.locator('.MuiChip-label');
    const option = card.getByRole(rubric ? 'button' : 'radio', { name: initial ? 'Inicial' : 'No aplica', exact: true });
    const outside = card.getByRole('button', { name: 'No aplica a mis funciones, pero sé hacerlo', exact: true });
    const next = page.getByRole('button', { name: 'Guardar y continuar', exact: true });
    await option.click();
    await expect(chip).toHaveText(initial ? 'Inicial' : 'No aplica');
    assert.equal(await page.locator('.cd-save-actions small').textContent(), 'Sección completa');
    await outside.click();
    if (rubric) {
        for (const i of [0, 1, 4, 5, 6]) await card.getByRole('checkbox').nth(i).check();
    }
    await next.click();
    await expect(chip).toHaveText('Nivel pendiente');
    await expect(page.locator('.cd-pending')).toBeVisible();
    view = await getSurvey();
    assert.equal(view.answers[toolId].state, 'outside');
    assert.deepEqual(view.evaluation.pending, [toolId]);
    assert.equal((await api('POST', '/api/encuesta/finalizar', { version: view.version })).status(), 422);
    await option.click();
    await expect(outside).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.cd-pending')).toHaveCount(0);
    await expect(page.locator('.cd-feedback [role="alert"]')).toHaveCount(0);
    await expect(chip).toHaveText(initial ? 'Inicial' : 'No aplica');
    // El resumen del apartado debe reaccionar aun antes de los 1500 ms del autoguardado.
    assert.equal(await page.locator('.cd-save-actions small').textContent(), 'Sección completa');
    await expect(page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', {
        name: rubric ? 'Gobernanza de datos' : 'Ciberseguridad', exact: true
    })).toBeEnabled();
    await next.click();
    const nextStep = rubric ? 'datos' : 'ciber';
    await expect(page).toHaveURL(origin + surveyUrl + '?paso=' + nextStep);
    view = await getSurvey();
    const verifyChoice = () => {
        const answer = view.answers[toolId];
        assert.equal(answer.state, initial ? 'level' : 'na');
        assert.equal(answer.level ?? null, initial && !rubric ? 0 : null);
        assert.equal(answer.checks ?? null, initial && rubric ? '000000000000' : null);
        assert.equal(view.evaluation.answered, view.evaluation.total);
        assert.deepEqual(view.evaluation.pending, []);
    };
    verifyChoice();
    await page.goto(origin + surveyUrl + '?paso=' + blockId);
    await page.reload();
    if (rubric) await expect(option).toHaveAttribute('aria-pressed', 'true');
    else await expect(option).toBeChecked();
    await outside.click();
    await expect(chip).toHaveText('Nivel pendiente');
    if (rubric) {
        for (const i of [0, 1, 2, 4, 5, 6, 8, 9, 10]) await card.getByRole('checkbox').nth(i).check();
    } else await card.getByRole('radio', { name: 'Avanzado', exact: true }).check();
    await expect(chip).toHaveText('Avanzado');
    await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado');
    assert.equal((await getSurvey()).answers[toolId].level, 3);
    await option.click();
    if (rubric) await expect(card.locator('input[type="checkbox"]:checked')).toHaveCount(0);
    else await expect(card.getByRole('radio', { name: 'Avanzado', exact: true })).not.toBeChecked();
    await next.click();
    await page.getByRole('navigation', { name: 'Bloques de la encuesta' }).getByRole('button', { name: 'Inteligencia artificial', exact: true }).click();
    await page.getByRole('button', { name: 'Finalizar encuesta', exact: true }).click();
    await page.getByRole('button', { name: 'Confirmar finalización' }).click();
    await expect(page.getByRole('heading', { name: 'Tu encuesta ya fue completada' })).toBeVisible();
    view = await getSurvey();
    verifyChoice();
    assert.ok(view.completedAt);
    const report = view.evaluation.report;
    assert.equal(report.global, initial ? 0 : null);
    assert.equal(report.knowledge.score, initial ? 0 : null);
    const result = report.tools.find(t => t.id === toolId);
    assert.equal(result.value, initial ? 0 : -1);
    assert.ok(!result.outsideRole && result.outsideLevel == null);
    const after = await distribution();
    assert.equal(after.count - before.count, initial ? 1 : 0);
    assert.equal(after.levels[0] - before.levels[0], initial ? 1 : 0);
    assert.equal(after.notApplicable - before.notApplicable, initial ? 0 : 1);
    assert.deepEqual(after.outside, before.outside);
    assert.equal(after.knowledge.evaluated - before.knowledge.evaluated, initial ? 1 : 0);
    assert.equal(await auditCount(), saves);
}
