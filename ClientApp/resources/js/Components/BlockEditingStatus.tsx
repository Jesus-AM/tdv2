import { Box, Button, CircularProgress, Typography } from '@mui/material';
import { EditOutlined, LockOutlined } from '@mui/icons-material';
import type { BlockEditor, EditorState } from '@/lib/block-editor';

/** Un solo estado por registro; los campos siguen disponibles para consultar y copiar. */
export default function BlockEditingStatus({ engine, state, blockKey, onResolve }: {
    engine: BlockEditor; state: EditorState; blockKey: string; onResolve: () => void;
}) {
    const lease = state.blocks[blockKey]?.reserva, preparing = state.preparing.includes(blockKey);
    const busy = engine.busy(blockKey), own = engine.canEdit(blockKey), issue = state.issues[blockKey];
    return <Box sx={{ minWidth: 150, maxWidth: 290, py: 0.7 }}>
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 0.6,
            color: busy ? '#714600' : own ? 'primary.main' : 'text.secondary', fontSize: '0.78rem' }}>
            {busy ? <><LockOutlined fontSize="inherit" /><span>{lease?.otraPestana ? 'Estás editando este registro en otra pestaña' : `${lease?.titular || 'Otra persona'} está editando`}</span></>
                : preparing ? <><CircularProgress size={13} /><span>Preparando edición…</span></>
                : own ? <><EditOutlined fontSize="inherit" /><span>Estás editando</span></> : null}
        </Box>
        {issue && <Box sx={{ mt: 0.5 }}><Typography variant="caption" color="error.main">{issue}</Typography>
            <Button size="small" onClick={onResolve}>Resolver</Button></Box>}
    </Box>;
}
