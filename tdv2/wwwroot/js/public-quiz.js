// El portal Razor no monta React. Dialog nativo conserva foco/modalidad sin cargar el editor.
const opener = document.getElementById('open-microsoft-quiz');
const dialog = document.getElementById('microsoft-quiz-dialog');
const frame = document.getElementById('microsoft-quiz-frame');
const closeButton = document.getElementById('close-microsoft-quiz');
const loading = document.getElementById('microsoft-quiz-loading');
const status = document.getElementById('microsoft-quiz-status');
const retry = document.getElementById('retry-microsoft-quiz');
let loadingQuiz;
let loaded = false;
let previous;

async function loadQuiz() {
    if (loaded || loadingQuiz) return;
    status.textContent = 'Cargando reto…'; retry.hidden = true;
    loadingQuiz = fetch('/quiz/microsoft-365/index.html', { credentials: 'omit' });
    try {
        const response = await loadingQuiz;
        if (!response.ok) throw new Error('Quiz unavailable');
        // Documento local revisado; nunca HTML del usuario ni una URL parametrizable.
        // srcdoc conserva X-Frame-Options: DENY de todas las respuestas del servidor.
        frame.srcdoc = await response.text();
    } catch {
        status.textContent = 'No fue posible cargar el reto. Inténtalo de nuevo.';
        retry.hidden = false;
    } finally { loadingQuiz = null; }
}
frame.addEventListener('load', () => {
    const document = frame.contentDocument;
    if (!document?.getElementById('welcome')) return;
    loaded = true; loading.hidden = true; frame.hidden = false;
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && !event.defaultPrevented && dialog.open) { event.preventDefault(); dialog.close(); }
    });
});
opener.addEventListener('click', () => {
    if (dialog.open) return;
    previous = { x: scrollX, y: scrollY, overflow: document.documentElement.style.overflow, padding: document.body.style.paddingRight };
    const gutter = innerWidth - document.documentElement.clientWidth;
    document.body.style.paddingRight = `${parseFloat(getComputedStyle(document.body).paddingRight) + gutter}px`;
    document.documentElement.style.overflow = 'hidden';
    dialog.showModal(); closeButton.focus({ preventScroll: true });
    void loadQuiz();
});
closeButton.addEventListener('click', () => dialog.close());
retry.addEventListener('click', () => void loadQuiz());
dialog.addEventListener('close', () => {
    if (!previous) return;
    document.documentElement.style.overflow = previous.overflow;
    document.body.style.paddingRight = previous.padding;
    scrollTo({ left: previous.x, top: previous.y, behavior: 'instant' });
    opener.focus({ preventScroll: true }); previous = null;
});
