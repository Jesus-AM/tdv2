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

const currentReply = (photo, extra = {}) => ({ photo, servidorEn: new Date().toISOString(),
    renuevaEn: new Date(Date.now() + 30000).toISOString(), vigenteHasta: new Date(Date.now() + 120000).toISOString(), ...extra });
test('renovación en segundo plano conserva foto, evita duplicados y distingue ausencia confirmada', async t => {
    t.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: Date.parse('2026-10-07T00:00:00Z') });
    let requests = 0, finish;
    const store = new ParticipantPhotoStore(async () => ++requests === 1 ? currentReply('photo-one') : new Promise(resolve => { finish = resolve; }));
    const off = store.subscribe(url, () => {}); await settle(); off();
    store.subscribe(url, () => {}); assert.equal(requests, 1); assert.equal(store.photo(url), 'photo-one');
    t.mock.timers.tick(30000); await settle();
    store.subscribe(url, () => {}); assert.equal(requests, 2); assert.equal(store.photo(url), 'photo-one');
    finish(currentReply('photo-two')); await settle(); assert.equal(store.photo(url), 'photo-two');
    t.mock.timers.tick(30000); await settle(); finish(currentReply(null, { estado: 'ausente' })); await settle();
    assert.equal(store.photo(url), null); store.dispose();
});
test('red caída conserva imagen hasta límite absoluto y reintenta con espera progresiva', async t => {
    t.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: Date.parse('2026-10-07T00:00:00Z') });
    let requests = 0;
    const store = new ParticipantPhotoStore(async () => { if (++requests === 1) return currentReply('valid'); throw new Error('offline'); });
    store.subscribe(url, () => {}); await settle();
    t.mock.timers.tick(30000); await settle(); assert.equal(requests, 2); assert.equal(store.photo(url), 'valid');
    t.mock.timers.tick(30000); await settle(); assert.equal(requests, 3);
    t.mock.timers.tick(59999); await settle(); assert.equal(requests, 3); assert.equal(store.photo(url), 'valid');
    t.mock.timers.tick(1); await settle(); assert.equal(requests, 4); assert.equal(store.photo(url), null);
    store.dispose();
});
test('401/403/409 y fallo de autorización del servidor descartan la foto en vez de conservarla', async t => {
    t.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: Date.parse('2026-10-07T00:00:00Z') });
    for (const status of [401, 403, 409, 503]) {
        let calls = 0;
        const store = new ParticipantPhotoStore(async () => { if (!calls++) return currentReply('valid'); throw { response: { status } }; });
        store.subscribe(url, () => {}); await settle(); assert.equal(store.photo(url), 'valid');
        t.mock.timers.tick(30000); await settle(); assert.equal(store.photo(url), null); store.dispose();
    }
});
test('respuesta conservada del servidor no amplía vigencia al suscribirse desde otra página', async t => {
    const start = Date.parse('2026-10-07T00:00:00Z');
    t.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: start });
    const store = new ParticipantPhotoStore(async () => currentReply('same', { estado: 'conservada', vigenteHasta: new Date(start + 45000).toISOString() }));
    let off = store.subscribe(url, () => {}); await settle(); t.mock.timers.tick(30000); await settle();
    off(); off = store.subscribe(url, () => {}); assert.equal(store.photo(url), 'same');
    t.mock.timers.tick(15000); assert.equal(store.photo(url), null); off(); store.dispose();
});
test('identidad/contexto inválidos y limpiar mientras llega una respuesta jamás recuperan la foto anterior', async t => {
    t.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: Date.parse('2026-10-07T00:00:00Z') });
    const account = '/user/photo'; let resolve, requests = 0;
    const store = new ParticipantPhotoStore(() => { requests++; return new Promise(done => { resolve = done; }); }, 'context-one');
    store.subscribe(account, () => {}); store.clear();
    resolve(currentReply('old-photo', { contexto: 'context-one' })); await settle(); assert.equal(store.photo(account), null);
    t.mock.timers.tick(3600001); await settle(); assert.equal(requests, 1); assert.equal(store.photo(account), null); store.dispose();
    const other = new ParticipantPhotoStore(async () => currentReply('old-photo', { contexto: 'context-one' }), 'context-two');
    other.subscribe(account, () => {}); await settle(); assert.equal(other.photo(account), null); other.dispose();
});
