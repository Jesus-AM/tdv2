import { Box, Typography } from '@mui/material';
import FieldHelp from './FieldHelp';
import { priorities, PriorityValue } from './PrioritySelect';

export default function ProcedureFieldHelp({ field }: { field: string }) {
    switch (field) {
        case 'codigo': return <FieldHelp title="Código" explanation={<>
            <Typography>Es el número de identificación del procedimiento. El sistema lo pone automáticamente; no tienes que escribirlo.</Typography>
            <Typography sx={{ mt: 1 }}>La etiqueta de abajo indica su origen: una norma (como ISO 21001) o «Nuevo» si tú lo agregaste.</Typography>
        </>} example="PO-01, PO-02, PO-03…" advice="Usa este código si necesitas mencionar el procedimiento en otras secciones." />;
        case 'sistema': return <FieldHelp title="Sistema o herramienta" explanation={<>
            <Typography>Elige de la lista el sistema, portal o programa que usas para ese procedimiento.</Typography>
            <Typography sx={{ mt: 1 }}>Si no aparece, elige «Otra (escríbela)» y se abrirá un campo para escribir su nombre.</Typography>
        </>} example="Sistema Integral de Información · Microsoft 365" />;
        case 'uso': return <FieldHelp title="¿Para qué se usa?"
            explanation="Elige qué haces con la herramienta dentro de ese procedimiento. Si ninguna opción describe el uso, elige «Otro (escríbelo)»."
            example="Agendar o programar" />;
        case 'estado': return <FieldHelp title="¿Cómo funciona?" explanation={<>
            <Typography>Elige la opción que mejor describe la herramienta hoy:</Typography>
            <Typography sx={{ whiteSpace: 'pre-line', mt: 1 }}>{'✓ Funciona bien\n! Funciona con fallas\n✕ No funciona\n– Ya no se usa'}</Typography>
        </>} example="Funciona con fallas" advice="Si eliges «con fallas» o «no funciona», descríbelo en la siguiente columna." />;
        case 'fallas': return <FieldHelp title="Áreas de oportunidad"
            explanation="Escribe con tus palabras los problemas de la herramienta o cualquier comentario útil. Si funciona bien, puedes dejarlo vacío."
            example="Se cae en horas pico · No permite exportar a Excel" advice="Sé específico: cuándo pasa y qué consecuencia tiene." />;
        case 'tramite': return <FieldHelp title="Procedimiento"
            explanation="Identifica el procedimiento que realiza tu área para atender una necesidad o cumplir una función." />;
        case 'usuario': return <FieldHelp title="¿A quién atiende?"
            explanation={<>
                <Typography>Selecciona a quién atiende este procedimiento. Puedes elegir varias opciones.</Typography>
                <Box component="dl" sx={{ my: 2, '& dt': { fontWeight: 700, mt: 1 }, '& dd': { m: 0 } }}>
                    <dt>Público en general</dt><dd>Personas externas a la UACJ que participan en el procedimiento a título personal.</dd>
                    <dt>Instituciones públicas externas</dt><dd>Dependencias de gobierno, organismos públicos y otras instituciones educativas públicas.</dd>
                    <dt>Empresas y organizaciones privadas</dt><dd>Empresas, asociaciones y otras organizaciones privadas.</dd>
                </Box>
            </>}
            example="Estudiantes · Docentes" advice="Piensa: ¿quién se beneficia cuando terminas este procedimiento?" />;
        case 'resultado': return <FieldHelp title="¿Qué entrega?"
            explanation="Lo que se obtiene al terminar el procedimiento: un documento, reporte, sistema, constancia, etc."
            example="Reporte estadístico · Prototipo · Constancia" advice="Si el procedimiento genera varios resultados, sepáralos con comas." />;
        case 'responsable': return <FieldHelp title="Área responsable"
            explanation="El área, departamento o puesto que se encarga de realizar el procedimiento."
            advice="Usa el nombre completo del área responsable." />;
        case 'validacion': return <FieldHelp title="Validación" explanation={<Box sx={{ display: 'grid', gap: 2 }}>
            {[
                ['¿Está correcto? → Vigente', 'El procedimiento se sigue haciendo tal como está escrito.'],
                ['¿Necesita cambios? → Ajustar', 'Edita el registro con los ajustes necesarios y márcalo como Ajustar.'],
                ['¿No es de tu área o ya no aplica? → Elimínalo', 'No lo valides: bórralo con el botón de «Eliminar» de la fila.'],
            ].map(([title, text]) => <Box key={title}><Typography sx={{ fontWeight: 600 }}>{title}</Typography><Typography>{text}</Typography></Box>)}
        </Box>} advice="Completa los campos obligatorios y valida el procedimiento para poder relacionarlo con sistemas y herramientas." />;
        case 'prioridad': return <FieldHelp title="Prioridad" explanation={<>
            <Typography sx={{ mb: 2 }}>Indica la urgencia de mejorar o automatizar el procedimiento:</Typography>
            {priorities.map((option, index) => <Box key={option[0]} sx={{ mb: 2 }}>
                <PriorityValue option={option} />
                <Typography sx={{ mt: .5 }}>{[
                    'Se hace todos los días o atiende a toda la comunidad. Si se retrasa, afecta a estudiantes o al cumplimiento de normas.',
                    'Se hace cada semana o atiende a varias dependencias, y cada atención toma más de una hora.',
                    'Se hace cada mes o atiende a una sola dependencia. Mejorarlo ahorraría tiempo, pero hoy funciona sin problemas graves.',
                    'Se hace pocas veces al año o toma poco tiempo. Mejorarlo traería un beneficio pequeño.',
                    'Es ocasional y funciona bien como está. Es importante para tu área, pero no necesita cambios por ahora.',
                ][index]}</Typography>
            </Box>)}
        </>} />;
        case 'proceso': return <FieldHelp title="Procedimiento"
            explanation="Elige un procedimiento de Identificación general. Solo aparecen los guardados y completos, marcados como Vigente o Ajustar."
            example="PO-02 · Agendar una cita para un evento de videoconferencia"
            advice="¿No aparece el procedimiento? Revisa que esté completo y validado en Identificación general." />;
        default: return null;
    }
}
