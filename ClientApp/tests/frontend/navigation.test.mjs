import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
import axios from 'axios';

// Exercise the actual transport without a browser, network, personal data or React rendering.
const events = new EventTarget();
let address = new URL('https://tdv2.example.test/inicio');
const history = [];
const alerts = [];
globalThis.window = {
    get location() { return { origin: address.origin, pathname: address.pathname, search: address.search,
        href: address.href, assign: value => { address = new URL(value, address); } }; },
    history: Object.fromEntries(['pushState', 'replaceState'].map(method => [method, (state, _, path) => {
        assert.equal(state, null, 'browser history must never contain page data');
        address = new URL(path, address); history.push([method, address.pathname]);
    }])),
    addEventListener: events.addEventListener.bind(events), removeEventListener: events.removeEventListener.bind(events),
    alert: value => alerts.push(value),
};
const source = readFileSync(new URL('../../resources/js/lib/navigation.tsx', import.meta.url), 'utf8');
const javascript = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText
    .replaceAll("from 'react'", `from '${import.meta.resolve('react')}'`)
    .replaceAll("from 'axios'", `from '${import.meta.resolve('axios')}'`);
const { router, startNavigation } = await import(`data:text/javascript;base64,${Buffer.from(javascript).toString('base64')}`);
const props = () => ({ name: 'TDV2', auth: { user: null, roles: [], modules: [], canPreview: false, canRepresent: false },
    csrfToken: 'synthetic-csrf', contextoEdicion: 'own', simulacion: null, representacion: null,
    session: { lifetime_ms: 1 }, routes: {}, estado: { revision: 1 }, configuracion: { draft: 'preservar' } });
let answer = config => ({ component: 'Inicio', props: props(), url: config.url });
let calls = [];
axios.defaults.adapter = async config => {
    calls.push(config);
    return { data: await answer(config), status: 200, statusText: 'OK', headers: {}, config };
};
let published;
let subscriber;
const stop = startNavigation(page => { published = page; subscriber?.(page); subscriber = undefined; });
function nextPage(action) {
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => reject(new Error('No se publicó la página.')), 2000);
        subscriber = value => { clearTimeout(timeout); resolve(value); };
        action?.();
    });
}
await nextPage();

test('navegación usa JSON ASP.NET y no guarda props en historial', async () => {
    calls = [];
    await nextPage(() => router.visit('/formatos/A'));
    assert.equal(calls[0].headers.get('X-TDV2-Page'), '1');
    assert.equal(calls[0].headers.get('Accept'), 'application/json');
    assert.equal(history.at(-1)[1], '/formatos/A');
});
test('recarga parcial conserva borrador y actualiza identidad/contexto', async () => {
    answer = config => ({ component: 'Inicio', url: config.url, props: { ...props(), contextoEdicion: 'effective-2',
        estado: { revision: 2 }, configuracion: { draft: 'servidor' } } });
    await nextPage(() => router.reload({ only: ['estado'] }));
    assert.equal(published.props.configuracion.draft, 'preservar');
    assert.equal(published.props.estado.revision, 2);
    assert.equal(published.props.contextoEdicion, 'effective-2');
});
test('mutación manda CSRF y contexto del documento', async () => {
    answer = config => config.method === 'post' ? { redirect: '/inicio' } : { component: 'Inicio', url: '/inicio', props: props() };
    calls = [];
    await new Promise(resolve => router.post('/vista-prueba', { ur: 'A' }, { onFinish: resolve }));
    const mutation = calls.find(c => c.method === 'post');
    assert.equal(mutation.headers.get('X-CSRF-TOKEN'), 'synthetic-csrf');
    assert.equal(mutation.headers.get('X-TDV2-Context'), 'effective-2');
});
test('protección de borrador cancela navegación y mutación antes de HTTP', async () => {
    const remove = router.on('before', event => event.preventDefault());
    calls = [];
    router.visit('/otra');
    await new Promise(resolve => router.post('/vista-prueba', {}, { onFinish: resolve }));
    assert.equal(calls.length, 0);
    remove();
});
test('respuestas tardías no reemplazan la página vigente', async () => {
    let release;
    const pending = new Promise(resolve => { release = resolve; });
    answer = async config => {
        if (config.url === '/lenta') await pending;
        return { component: 'Inicio', url: config.url, props: props() };
    };
    router.visit('/lenta');
    await nextPage(() => router.visit('/rapida'));
    release();
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(published.url, '/rapida');
});
test('destino externo es rechazado sin enviar credenciales', async () => {
    calls = [];
    await nextPage(() => router.visit('https://external.example.test/steal'));
    assert.equal(calls.length, 0);
    assert.equal(published.component, 'AccesoRestringido');
});
test('error de validación llega al formulario sin desechar la página', async () => {
    answer = config => { throw new axios.AxiosError('invalid', 'ERR_BAD_REQUEST', config, null,
        { data: { errors: { ur: ['UR inválida'] } }, status: 422, headers: {}, config }); };
    let errors;
    const previous = published;
    await new Promise(resolve => router.post('/vista-prueba', {}, { onError: value => { errors = value; }, onFinish: resolve }));
    assert.equal(errors.ur, 'UR inválida');
    assert.equal(published, previous);
});
test('revocación conserva sólo contexto público para terminar representación', async () => {
    const representation = { id: 'context-public', nombre: 'Persona sintética', email: 'sintetica@example.test', escritura: false };
    answer = config => { throw new axios.AxiosError('revoked', 'ERR_BAD_REQUEST', config, null,
        { data: { message: 'Representación revocada', representacion: representation, contextoEdicion: 'context-public' }, status: 409, headers: {}, config }); };
    await nextPage(() => router.visit('/inicio'));
    assert.equal(published.component, 'AccesoRestringido');
    assert.equal(published.props.representacion, representation);
    assert.equal(published.props.contextoEdicion, 'context-public');
    assert.equal(published.props.auth.user, null);
    assert.equal(published.props.configuracion, undefined);
    calls = [];
    answer = config => config.method === 'delete' ? { redirect: '/inicio' } : { component: 'Inicio', url: '/inicio', props: props() };
    await new Promise(resolve => router.delete('/actuar-como-usuario', { onFinish: resolve }));
    assert.equal(calls.find(c => c.method === 'delete').headers.get('X-TDV2-Context'), 'context-public');
    assert.equal(published.props.contextoEdicion, 'own');
});
test('fallo temporal de recarga conserva página/borrador y comunica el error', async () => {
    answer = config => ({ component: 'Sincronizaciones', url: config.url, props: props() });
    await nextPage(() => router.visit('/configuracion/sincronizaciones'));
    const previous = published;
    answer = config => { throw new axios.AxiosError('offline', 'ERR_NETWORK', config); };
    let message, success = false;
    await new Promise(resolve => router.reload({ only: ['estado'], onError: e => { message = e.general; }, onSuccess: () => { success = true; }, onFinish: resolve }));
    assert.equal(published, previous);
    assert.match(message, /actualizar/);
    assert.equal(success, false);
});

test('revocación elimina props privadas también al refrescar con callback de error', async () => {
    answer = config => { throw new axios.AxiosError('denied', 'ERR_BAD_REQUEST', config, null,
        { data: { message: 'Acceso revocado' }, status: 403, headers: {}, config }); };
    await nextPage(() => router.reload({ only: ['estado'], onError: () => assert.fail('403 no conserva permisos antiguos') }));
    assert.equal(published.component, 'AccesoRestringido');
    assert.equal(published.props.access.status, 403);
    assert.equal(published.props.auth.user, null);
    assert.equal(published.props.configuracion, undefined);
    stop();
});
