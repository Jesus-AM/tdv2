import { optionLabels, sectionInstructions } from './surveyInstructions';

/** Texto de presentación: no se incorpora a respuestas ni a informes definitivos. */
export default function SurveyIntroduction({ section }: { section: string }) {
    const content = sectionInstructions[section];
    if (!content) return null;
    return <div className={`cd-survey-intro${section === 'perfil' ? '' : ' cd-block-intro'}`}>
        <h2>{content.title}</h2>
        {content.subtitle && <h3>{content.subtitle}</h3>}
        {content.paragraphs.map(paragraph => <p key={paragraph}>{paragraph}</p>)}
        {content.required && <p><strong>{content.required}</strong></p>}
        {content.options && <ul>{content.options.map((text, index) => <li key={optionLabels[index]}><strong>{optionLabels[index]}</strong> {text}</li>)}</ul>}
        {content.consecutiveLevels && <p className="cd-instruction-note">Nota: Para alcanzar <strong>Intermedio</strong>, debes cumplir también <strong>Básico</strong>. Para alcanzar <strong>Avanzado</strong>, debes cumplir <strong>Básico</strong> e <strong>Intermedio</strong>.</p>}
    </div>;
}
