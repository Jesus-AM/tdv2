// Contraste WCAG a partir de colores calculados, compuestos sobre sus superficies.
// Gradientes: evalúa todos los extremos; imágenes: asume también blanco/negro detrás.
export async function renderedContrast(page, root = 'body') {
    return page.evaluate(rootSelector => {
        const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        const rgba = value => {
            ctx.clearRect(0, 0, 1, 1); ctx.fillStyle = value; ctx.fillRect(0, 0, 1, 1);
            const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data; return [r, g, b, a / 255];
        };
        const composite = (fg, bg) => fg.slice(0, 3).map((c, i) => c * fg[3] + bg[i] * (1 - fg[3])).concat(1);
        const luminance = color => color.slice(0, 3).map(c => c / 255).map(c => c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4)
            .reduce((sum, c, i) => sum + c * [.2126, .7152, .0722][i], 0);
        const ratio = (a, b) => (Math.max(luminance(a), luminance(b)) + .05) / (Math.min(luminance(a), luminance(b)) + .05);
        const backgrounds = el => {
            let choices = [[255, 255, 255, 1]];
            const parents = []; for (let p = el; p; p = p.parentElement) parents.unshift(p);
            for (const p of parents) {
                const s = getComputedStyle(p), background = rgba(s.backgroundColor);
                choices = choices.map(c => composite(background, c));
                if (s.backgroundImage.includes('url(')) choices.push([0, 0, 0, 1], [255, 255, 255, 1]);
                const colors = s.backgroundImage.match(/(?:rgba?|color)\([^)]*\)/g);
                if (colors?.length) choices = colors.flatMap(color => choices.map(c => composite(rgba(color), c)));
            }
            return choices;
        };
        return [...document.querySelectorAll(`${rootSelector}, ${rootSelector} *`)].filter(el => {
            const s = getComputedStyle(el), r = el.getBoundingClientRect();
            return r.width && r.height && s.visibility !== 'hidden' && [...el.childNodes].some(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim());
        }).map(el => {
            const s = getComputedStyle(el), fg = rgba(s.color), bg = backgrounds(el);
            return { text: [...el.childNodes].filter(n => n.nodeType === Node.TEXT_NODE).map(n => n.textContent).join('').trim().slice(0, 100),
                selector: `${el.tagName}.${el.className}`, color: s.color, backgrounds: bg,
                ratio: Math.min(...bg.map(c => ratio(composite(fg, c), c))),
                required: parseFloat(s.fontSize) >= 24 || parseFloat(s.fontSize) >= 18.66 && parseInt(s.fontWeight) >= 700 ? 3 : 4.5 };
        });
    }, root);
}
