// Una fuente de colores para MUI y los documentos públicos independientes.
import { readFile, writeFile } from 'node:fs/promises';
const tokens = JSON.parse(await readFile(new URL('../ClientApp/resources/js/design-tokens.json', import.meta.url), 'utf8'));
const css = '/* Generado por scripts/build-design-tokens.mjs. Editar design-tokens.json. */\n:root{\n' +
    Object.entries(tokens).map(([key, value]) => {
        if (!/^#[0-9a-f]{6}$/i.test(value)) throw new Error('Color institucional inválido: ' + key);
        return `  --uacj-${key.replace(/[A-Z]/g, c => '-' + c.toLowerCase())}:${value};`;
    }).join('\n') + '\n}\n';
await writeFile(new URL('../tdv2/wwwroot/css/design-tokens.css', import.meta.url), css);
