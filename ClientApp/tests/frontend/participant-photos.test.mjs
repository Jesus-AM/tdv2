import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source = readFileSync(new URL('../../resources/js/lib/participant-photos.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { ParticipantPhotoStore } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const url = '/formatos/A/participantes/' + 'A'.repeat(64) + '/foto';
const settle = () => new Promise(resolve => setImmediate(resolve));
test('varios registros del participante comparten petición y caché; una nueva sesión de formulario no la hereda', async () => {
    let requests = 0, notify = 0, reply;
    const fetch = () => { requests++; return new Promise(resolve => { reply = resolve; }); };
    const store = new ParticipantPhotoStore(fetch);
    const stop = [store.subscribe(url, () => notify++), store.subscribe(url, () => notify++)];
    assert.equal(requests, 1); assert.equal(store.photo(url), null);
    reply({ photo: 'data:image/png;base64,sintetica', renuevaEn: new Date(Date.now() + 900000).toISOString() }); await settle();
    assert.equal(notify, 2); stop.forEach(f => f());
    const again = store.subscribe(url, () => notify++); assert.equal(requests, 1); assert.ok(store.photo(url)); again(); store.dispose();
    const other = new ParticipantPhotoStore(fetch); const unsubscribe = other.subscribe(url, () => {});
    assert.equal(requests, 2); assert.equal(other.photo(url), null); unsubscribe(); other.dispose(); reply({ photo: null, renuevaEn: '' }); await settle();
});
test('fallos usan iniciales sin reintentos por render; respuestas de contexto desmontado se ignoran', async () => {
    let requests = 0;
    const store = new ParticipantPhotoStore(async () => { requests++; throw new Error('synthetic offline'); });
    const off = store.subscribe(url, () => assert.fail('No cambió la miniatura')); await settle();
    off(); store.subscribe(url, () => {}); assert.equal(requests, 1); assert.equal(store.photo(url), null); store.dispose();
    let reply, notifications = 0;
    const stale = new ParticipantPhotoStore(() => new Promise(resolve => { reply = resolve; }));
    stale.subscribe(url, () => notifications++); stale.dispose();
    reply({ photo: 'synthetic', renuevaEn: new Date().toISOString() }); await settle();
    assert.equal(notifications, 0); assert.equal(stale.photo(url), null);
});
test('el almacén no solicita rutas arbitrarias ni un directorio de fotografías', () => {
    const store = new ParticipantPhotoStore(() => { assert.fail('Ruta no autorizada'); });
    for (const path of ['https://example.test/foto', '/user/photo', '/formatos/A/participantes/email/foto', null]) store.subscribe(path, () => {});
    store.dispose();
});
