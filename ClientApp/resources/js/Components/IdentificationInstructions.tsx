import { Box, Typography } from '@mui/material';

export default function IdentificationInstructions() {
    return <Box sx={{ typography: 'body2', mb: 2, color: 'text.primary' }}>
        <Typography component="p" sx={{ fontSize: 'inherit', fontWeight: 700, mb: 1 }}>¿Qué procedimientos institucionales realiza tu área?</Typography>
        <Typography sx={{ fontSize: 'inherit' }}>Cada fila corresponde a un procedimiento institucional que realiza tu área. Algunos registros ya vienen cargados: revísalos uno por uno.</Typography>
        <Box component="ul" sx={{ my: 1, pl: 3 }}>
            <li><strong>¿Está correcto?</strong> Complétalo y márcalo como <strong>Vigente</strong>.</li>
            <li><strong>¿Necesita cambios?</strong> Edita el registro con los ajustes necesarios y márcalo como <strong>Ajustar</strong>.</li>
            <li><strong>¿No es de tu área o ya no aplica?</strong> No lo valides; bórralo con el botón rojo <strong>«Eliminar»</strong> de la fila.</li>
        </Box>
        <Typography sx={{ fontSize: 'inherit' }}>Al final, agrega los <strong>procedimientos institucionales</strong> que falten. Si tienes dudas, toca el botón <strong>ⓘ</strong> junto a cada campo.</Typography>
    </Box>;
}
