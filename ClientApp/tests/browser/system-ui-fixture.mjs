// Sólo pruebas UI: transporte/reservas en memoria. No acredita persistencia ASP.NET/PostgreSQL.
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import path from 'node:path';

export async function systemFixture() {
    const root = path.resolve('..', 'tdv2', 'wwwroot');
    const definition = JSON.parse(await readFile('../tdv2/Contracts/procesos_operativos.json', 'utf8'));
    const blankSystem = { id: 'inicial', proceso: '', sistema: '', uso: '', estado: '', fallas: '', sistemaOtro: '', usoOtro: '', moduloSiiId: '', moduloSiiDescripcion: '' };
    const procedure = { id: 'inicial', codigo: 'PO-01', prioridad: '4', fuente: 'ILDA', area: '', tramite: 'Actividad sintética', usuario: ['Docentes'], resultado: 'Constancia', responsable: 'Área', validacion: 'V' };
    const content = { encabezado: { fecha: '', area: '', responsable: '' }, identificacion: [procedure, { ...procedure, id: 'norma', codigo: 'PO-02', fuente: 'ISO 21001' }, { ...procedure, id: 'manual', codigo: 'PO-03', fuente: 'Nuevo' }], sistemas: [blankSystem], datos: [], acuerdos: [], evaluaciones: {}, preguntas: [], medios: {}, medioOtro: '' };
    const fixture = { content, version: 0, catalog: { estado: 'pendiente', modulos: [] }, requests: [], submitted: false, delay: 0 };
    const blocks = new Map(), watches = new Set();
    const clone = v => structuredClone(v);
    const block = key => { if (!blocks.has(key)) blocks.set(key, { key, version: 0, reserva: null }); return blocks.get(key); };
    const view = (b, tab) => ({ key: b.key, version: b.version, reserva: b.reserva ? { ...b.reserva, propia: b.tab === tab } : null });
    const live = tab => ({ contenido: clone(content), version: fixture.version, porcentaje: 0, porcentajeEtapa: 0, bloques: [...blocks.values()].map(b => view(b, tab)), editable: !fixture.submitted,
        procedimientosDisponibles: content.identificacion.filter(p => ['V', 'A'].includes(p.validacion)).map(({ id, codigo, tramite }) => ({ id, codigo, tramite })),
        puedeEnviar: false, enviadoEn: fixture.submitted ? new Date().toISOString() : null, enviadoPor: null, revisionEnvio: null, revisionEtapa: null, seccion: 'contexto', servidorEn: new Date().toISOString(), actualizadoEn: null, actualizadoPor: null });
    const notify = () => { for (const w of watches) w.socket.send(JSON.stringify({ type: 2, invocationId: w.id, item: live(w.tab) }) + '\x1e'); };
    fixture.broadcast = notify;
    fixture.webSockets = async context => context.routeWebSocket('**/form-events*', socket => {
        let watch;
        socket.onMessage(message => {
            for (const part of String(message).split('\x1e').filter(Boolean)) {
                const data = JSON.parse(part);
                if (data.protocol) socket.send('{}\x1e');
                if (data.type === 4) { watch = { socket, tab: data.arguments[2], id: data.invocationId }; watches.add(watch); notify(); }
            }
        });
        socket.onClose(() => watches.delete(watch));
    });
    const server = createServer(async (req, res) => {
        const url = new URL(req.url, 'http://127.0.0.1');
        const json = (value, status = 200) => { res.writeHead(status, { 'content-type': 'application/json' }); res.end(JSON.stringify(value)); };
        if (url.pathname.startsWith('/assets/') || url.pathname.startsWith('/images/')) {
            const base = root;
            const file = path.resolve(base, '.' + url.pathname);
            if (!file.startsWith(base + path.sep)) { res.writeHead(404); return res.end(); }
            res.setHeader('content-type', file.endsWith('.svg') ? 'image/svg+xml' : file.endsWith('.js') ? 'text/javascript' : 'text/css');
            try { return res.end(await readFile(file)); } catch { res.writeHead(404); return res.end(); }
        }
        if (url.pathname === '/form-events/negotiate') return json({ negotiateVersion: 1, connectionId: randomUUID(), connectionToken: randomUUID(), availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] });
        if (url.pathname.endsWith('/modulos-sii')) {
            if (fixture.delay) await new Promise(resolve => setTimeout(resolve, fixture.delay));
            return json(fixture.catalog);
        }
        if (url.pathname.endsWith('/estado')) return json(live(url.searchParams.get('tab')));
        if (req.method === 'POST' || req.method === 'PATCH') {
            let data = ''; for await (const chunk of req) data += chunk; const body = JSON.parse(data || '{}');
            fixture.requests.push({ path: url.pathname, body: clone(body) });
            if (fixture.submitted) return json({ message: 'Enviado' }, 409);
            if (url.pathname.endsWith('/posicion')) return json({});
            const entries = body.blocks || [];
            if (entries.some(e => { const b = block(e.key); return b.version !== e.version || b.reserva && b.tab !== body.tabId; })) return json({ message: 'Reserva ajena o versión obsoleta' }, 409);
            for (const e of entries) {
                const b = block(e.key);
                if (url.pathname.endsWith('/reservas') || url.pathname.endsWith('/actividad')) {
                    b.tab = body.tabId; b.reserva ||= { id: randomUUID(), titular: 'Persona sintética', color: 2, foto: null };
                    b.reserva.venceEn = new Date(Date.now() + 45000).toISOString();
                } else if (url.pathname.endsWith('/liberar')) b.reserva = null;
                else if (url.pathname.endsWith('/bloques')) {
                    if (!b.reserva || b.reserva.id !== e.leaseId) return json({ message: 'Sin reserva' }, 409);
                    const [section, ...parts] = e.key.split(':'), id = parts.join(':');
                    if (Array.isArray(content[section])) {
                        const index = content[section].findIndex(r => r.id === id);
                        if (e.value === null) { if (index >= 0) content[section].splice(index, 1); }
                        else { const value = clone(e.value); if (index >= 0) content[section][index] = value; else content[section].push(value); }
                    }
                    b.version++; if (body.release || e.value === null) b.reserva = null;
                }
            }
            if (url.pathname.endsWith('/bloques')) fixture.version++;
            json({ ...live(body.tabId), bloques: entries.map(e => {
                const [section, ...parts] = e.key.split(':');
                const value = Array.isArray(content[section]) ? content[section].find(r => r.id === parts.join(':')) ?? null : content[section];
                return { ...view(block(e.key), body.tabId), value: clone(value) };
            }) });
            setTimeout(notify, 10); return;
        }
        if (req.headers['x-tdv2-page']) return json({ component: 'FormatoUR', url: '/formatos/A', props: {
            name: 'TDV2', auth: { user: { id: 1, name: 'Persona sintética', email: 'persona@example.test' }, roles: [], modules: [], canPreview: false, canRepresent: false }, simulacion: null, representacion: null, contextoEdicion: 'own', csrfToken: 'synthetic', session: { lifetime_ms: 7200000 }, routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' },
            unidad: { id_ur: 'A', cve_ur: '100', desc_ur: 'Área sintética', nivel_ur: 2, ejercicio: 2026, id_ur_pertenece: null }, contenido: clone(content), plantilla: { ...clone(content), sistemas: [clone(blankSystem)] }, definicion: definition,
            editable: !fixture.submitted, permisoEdicion: true, version: fixture.version, porcentaje: 0, porcentajeEtapa: 0, seccionesPosteriores: false, puedeEnviar: false, enviadoEn: fixture.submitted ? new Date().toISOString() : null, enviadoPor: null, actualizadoEn: null, actualizadoPor: null, guardarUrl: '/formatos/A',
        } });
        res.setHeader('content-type', 'text/html'); res.end(await readFile(path.join(root, 'index.html')));
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    fixture.origin = `http://127.0.0.1:${server.address().port}`;
    fixture.close = () => new Promise(resolve => { server.closeAllConnections(); server.close(resolve); });
    return fixture;
}
