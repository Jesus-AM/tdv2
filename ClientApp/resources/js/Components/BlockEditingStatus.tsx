import { Box, CircularProgress, Typography } from '@mui/material';
import { EditOutlined, LockOutlined } from '@mui/icons-material';
import type { BlockEditor, EditorState } from '@/lib/block-editor';
import { participantColor } from '@/lib/participant-colors';
import ParticipantAvatar from './ParticipantAvatar';

/** Un solo estado por registro; los campos siguen disponibles para consultar y copiar. */
export default function BlockEditingStatus({ engine, state, blockKey, compact = false }: {
    engine: BlockEditor; state: EditorState; blockKey: string; compact?: boolean;
}) {
    const lease = state.blocks[blockKey]?.reserva, preparing = state.preparing.includes(blockKey);
    const busy = engine.busy(blockKey), own = engine.canEdit(blockKey), issue = state.issues[blockKey];
    const color = participantColor(lease?.color);
    const name = lease?.titular || 'Otra persona';
    return <Box className={compact ? 'row-presence' : undefined} sx={{ minWidth: compact ? 0 : 150, maxWidth: compact ? 140 : 290, py: 0.7 }}>
        <Box role="status" aria-live="polite" sx={{ display: 'flex', alignItems: 'center', gap: 0.6, minHeight: compact ? 40 : undefined,
            color: busy || own ? color : 'text.secondary', fontSize: '0.78rem' }}>
            {(busy || own) && <ParticipantAvatar name={name} color={color} url={lease?.foto} />}
            {busy ? <><LockOutlined fontSize="inherit" /><span>{lease?.otraPestana ? (compact ? 'Otra pestaña' : 'Estás editando este registro en otra pestaña') : compact ? 'Ocupado' : `${lease?.titular || 'Otra persona'} está editando`}</span></>
                : preparing ? <><CircularProgress size={13} /><span>Preparando edición…</span></>
                : own ? <><EditOutlined fontSize="inherit" /><span>Estás editando</span></> : null}
        </Box>
        {issue && <Typography role="status" variant="caption" color="error.main" sx={{ display: 'block', mt: 0.5 }}>{issue}</Typography>}
        {Object.entries(state.unaccepted[blockKey] || {}).map(([field, text]) =>
            <Typography key={field} variant="caption" component="div" sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{text}</Typography>)}
    </Box>;
}
