// Adaptadores de eventos/foco. Preguntas, puntuación y diseño permanecen en quiz.js/css.
const questionHeading = document.getElementById('question');
const resultHeading = document.getElementById('resultTitle');
for (const heading of [questionHeading, resultHeading]) heading.tabIndex = -1;
const focusQuestion = () => questionHeading.focus();
document.querySelector('[data-quiz-action="start"]').addEventListener('click', () => { startGame(); focusQuestion(); });
document.querySelector('[data-quiz-action="help"]').addEventListener('click', showHow);
document.querySelector('[data-quiz-action="next"]').addEventListener('click', () => {
    nextQuestion();
    (document.getElementById('result').classList.contains('hidden') ? questionHeading : resultHeading).focus();
});
document.querySelector('[data-quiz-action="restart"]').addEventListener('click', () => { restart(); focusQuestion(); });
document.getElementById('answers').addEventListener('click', event => {
    if (event.target.closest('.answer') && answered) document.getElementById('nextBtn').focus();
});
document.getElementById('feedback').setAttribute('role', 'status');
