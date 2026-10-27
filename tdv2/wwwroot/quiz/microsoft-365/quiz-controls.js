// Adaptadores de eventos/foco. Preguntas, puntuación y diseño permanecen en quiz.js/css.
const questionHeading = document.getElementById('question');
const resultHeading = document.getElementById('resultTitle');
for (const heading of [questionHeading, resultHeading]) heading.tabIndex = -1;
const focusQuestion = () => questionHeading.focus();
document.querySelector('[data-quiz-action="start"]').addEventListener('click', () => { startGame(); focusQuestion(); });
const helpButton = document.querySelector('[data-quiz-action="help"]');
const instructions = document.getElementById('quiz-instructions');
const setInstructions = open => {
    instructions.hidden = !open;
    helpButton.setAttribute('aria-expanded', String(open));
    (open ? document.getElementById('quiz-instructions-title') : helpButton).focus();
    if (open) instructions.scrollIntoView({ block: 'start' });
};
helpButton.addEventListener('click', () => setInstructions(instructions.hidden));
document.querySelector('[data-quiz-action="help-close"]').addEventListener('click', () => setInstructions(false));
document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !instructions.hidden && !document.getElementById('welcome').classList.contains('hidden')) {
        event.preventDefault(); setInstructions(false);
    }
});
document.querySelector('[data-quiz-action="next"]').addEventListener('click', () => {
    nextQuestion();
    (document.getElementById('result').classList.contains('hidden') ? questionHeading : resultHeading).focus();
});
document.querySelector('[data-quiz-action="restart"]').addEventListener('click', () => { restart(); focusQuestion(); });
document.getElementById('answers').addEventListener('click', event => {
    if (event.target.closest('.answer') && answered) document.getElementById('nextBtn').focus();
});
document.getElementById('feedback').setAttribute('role', 'status');
