import { Box, Button, Typography } from '@mui/material';
import { CheckCircleOutlined, PendingOutlined } from '@mui/icons-material';
import type { ProcedureCoverage } from './FormSubmissionReview';

/** Proyección del servidor: no crea filas ni calcula una segunda regla de avance. */
export default function ProcedureCoverageList({ items, pendingSave, readOnly, preparing, onCapture, onNavigate }: {
    items: ProcedureCoverage[]; pendingSave: boolean; readOnly: boolean;
    preparing: string | null;
    onCapture: (id: string) => void; onNavigate: (item: ProcedureCoverage) => void;
}) {
    if (!items.length) return null;
    return <Box component="section" aria-label="Cobertura de procedimientos" sx={{ mb: 2 }}>
        <Typography variant="h3">Procedimientos y captura de herramientas</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ my: .5 }}>
            {items.filter(p => p.aplicable && !p.completa).length} pendientes de herramientas · {items.filter(p => !p.aplicable).length} por completar o validar en Identificación general
        </Typography>
        {pendingSave && <Typography variant="caption">Cobertura según el último guardado confirmado.</Typography>}
        <Box component="ul" sx={{ listStyle: 'none', p: 0, m: 0, maxHeight: 260, overflowY: 'auto' }}>
            {items.map(item => <Box component="li" key={item.id} data-coverage-id={item.id}
                sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1, py: .75, borderBottom: '1px solid', borderColor: 'divider' }}>
                {item.completa ? <CheckCircleOutlined color="success" sx={{ fontSize: 18 }} /> : <PendingOutlined color="warning" sx={{ fontSize: 18 }} />}
                <Box sx={{ flex: '1 1 260px', minWidth: 0, overflowWrap: 'anywhere' }}>
                    <Typography variant="body2">{item.codigo || 'Sin código'} · {item.nombre || 'Procedimiento sin nombre'}</Typography>
                    <Typography variant="caption" color="text.secondary">{!item.aplicable ? 'Completa o actualiza su validación antes de asociar herramientas'
                        : item.completa ? `Captura cubierta · ${item.registros} registros` : `Captura pendiente · ${item.registros} registros`}</Typography>
                </Box>
                <Button size="small" data-coverage-action={item.id} loading={preparing === item.id} disabled={preparing !== null && preparing !== item.id}
                    onClick={() => item.aplicable && !item.registros && !readOnly ? onCapture(item.id) : onNavigate(item)}
                    aria-label={`${item.aplicable && !item.registros && !readOnly ? 'Capturar herramientas' : 'Revisar captura'} de ${item.codigo || item.nombre}`}>
                    {!item.aplicable ? 'Ir a Identificación' : item.registros ? 'Ir al registro' : readOnly ? 'Consultar procedimiento' : 'Capturar herramientas'}
                </Button>
            </Box>)}
        </Box>
    </Box>;
}
