import { expect } from '@playwright/test';

// Cliente del protocolo real. Vive exclusivamente en las pruebas del host sintético de loopback.
export async function watchStream(page, unit = 'A') {
    const id = await page.evaluate(async unit => {
        const csrf = await (await fetch('/session/csrf')).json();
        const response = await fetch('/form-events/negotiate?negotiateVersion=1', { method: 'POST', headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own' } });
        if (!response.ok) throw new Error('Negotiation HTTP ' + response.status);
        const negotiation = await response.json(), id = crypto.randomUUID();
        const socket = new WebSocket(location.origin.replace('https:', 'wss:') + '/form-events?id=' + encodeURIComponent(negotiation.connectionToken));
        const state = { items: [], completed: false, error: '', socket };
        (window.syntheticStreams ||= {})[id] = state;
        let started = false;
        socket.onopen = () => socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\u001e');
        socket.onmessage = event => {
            for (const part of event.data.split('\u001e').filter(Boolean)) {
                const message = JSON.parse(part);
                if (!started) {
                    started = true;
                    socket.send(JSON.stringify({ type: 4, invocationId: '1', target: 'Watch', arguments: [unit, 'own', crypto.randomUUID()] }) + '\u001e');
                } else if (message.type === 2) state.items.push({ version: message.item.version, at: Date.now() });
                else if (message.type === 3) { state.completed = true; state.error = message.error || ''; }
            }
        };
        socket.onerror = () => { state.error = 'Transport error'; };
        return id;
    }, unit);
    await expect.poll(() => page.evaluate(id => window.syntheticStreams[id].items.length, id)).toBeGreaterThan(0);
    return id;
}
export async function streamState(page, id) {
    return page.evaluate(id => { const { socket, ...state } = window.syntheticStreams[id]; return state; }, id);
}
export async function cancelStream(page, id) {
    await page.evaluate(id => window.syntheticStreams[id].socket.send(JSON.stringify({ type: 5, invocationId: '1' }) + '\u001e'), id);
    await expect.poll(async () => (await streamState(page, id)).completed).toBe(true);
    await page.evaluate(id => window.syntheticStreams[id].socket.close(), id);
}
