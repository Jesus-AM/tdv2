import https from 'node:https';
import http from 'node:http';
import net from 'node:net';
import readline from 'node:readline';

// Test-only bridge for Windows Schannel environments without an installed dev certificate.
// Certificate and ephemeral key arrive on stdin, are never written to disk or trusted.
const input = readline.createInterface({ input: process.stdin });
input.once('line', line => {
    const { upstream, token, pfx } = JSON.parse(line), target = new URL(upstream);
    if (target.protocol !== 'http:' || target.hostname !== '127.0.0.1' || !token) throw new Error('Loopback only.');
    const headers = incoming => ({ ...incoming, 'x-synthetic-tls': token });
    const server = https.createServer({ pfx: Buffer.from(pfx, 'base64') }, (request, response) => {
        const forward = http.request({ host: target.hostname, port: target.port, method: request.method, path: request.url, headers: headers(request.headers) }, result => {
            response.writeHead(result.statusCode, result.headers); result.pipe(response);
        });
        forward.on('error', () => { response.writeHead(502); response.end(); });
        request.pipe(forward);
    });
    server.on('upgrade', (request, socket, head) => {
        const remote = net.connect(Number(target.port), target.hostname, () => {
            const lines = Object.entries(headers(request.headers)).map(([key, value]) => `${key}: ${value}`);
            remote.write(`${request.method} ${request.url} HTTP/${request.httpVersion}\r\n${lines.join('\r\n')}\r\n\r\n`);
            if (head.length) remote.write(head);
            socket.pipe(remote); remote.pipe(socket);
        });
        remote.on('error', () => socket.destroy()); socket.on('error', () => remote.destroy());
        socket.on('close', () => remote.destroy());
    });
    server.listen(0, '127.0.0.1', () => process.stdout.write(`https://127.0.0.1:${server.address().port}\n`));
    input.once('close', () => { server.close(); server.closeAllConnections(); process.exit(0); });
});
