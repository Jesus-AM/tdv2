import { createContext, useCallback, useContext, useEffect, useRef, useState, useSyncExternalStore, type ReactNode } from 'react';
import { Avatar, Box, Tooltip } from '@mui/material';
import axios from 'axios';
import { ParticipantPhotoStore } from '@/lib/participant-photos';

const Photos = createContext<ParticipantPhotoStore | null>(null);
export function ParticipantPhotos({ children }: { children: ReactNode }) {
    const [store] = useState(() => new ParticipantPhotoStore(async (url, signal) => (await axios.get(url, { signal })).data));
    useEffect(() => () => store.dispose(), [store]);
    return <Photos.Provider value={store}>{children}</Photos.Provider>;
}
export default function ParticipantAvatar({ name, color, url }: { name: string; color: string; url?: string | null }) {
    const [open, setOpen] = useState(false);
    const touchTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
    useEffect(() => () => clearTimeout(touchTimer.current), []);
    const store = useContext(Photos);
    const subscribe = useCallback((notify: () => void) => store?.subscribe(url, notify) ?? (() => {}), [store, url]);
    const photo = useSyncExternalStore(subscribe, () => store?.photo(url) ?? null);
    return <Tooltip title={name} describeChild open={open} onOpen={() => setOpen(true)} onClose={() => setOpen(false)}
        disableTouchListener disableInteractive>
        <Box component="span" role="img" tabIndex={0} aria-label={`Participante: ${name}`} data-participant-avatar="true"
            // Consultar la foto conserva cursor/selección. Tab permite enfocarla sin iniciar captura.
            onPointerDown={event => {
                event.preventDefault();
                // Evitar el foco del toque también suprime eventos compatibles de ratón.
                // Abrir explícitamente el Tooltip conserva el cursor sin depender de esos eventos.
                if (event.pointerType === 'touch' || event.pointerType === 'pen') {
                    clearTimeout(touchTimer.current); setOpen(true);
                    touchTimer.current = setTimeout(() => setOpen(false), 2500);
                }
            }}
            sx={{ display: 'inline-flex', borderRadius: '50%', flexShrink: 0, '&:focus-visible': { outline: `2px solid ${color}`, outlineOffset: 2 } }}>
            <Avatar src={photo || undefined} aria-hidden="true" sx={{ width: 26, height: 26, bgcolor: color, color: '#fff', fontSize: 11 }}>
                {name.trim().split(/\s+/).slice(0, 2).map(word => word[0]).join('').toLocaleUpperCase('es')}
            </Avatar>
        </Box>
    </Tooltip>;
}
