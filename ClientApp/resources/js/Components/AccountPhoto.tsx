import { createContext, useCallback, useContext, useEffect, useMemo, useSyncExternalStore, type ReactNode } from 'react';
import axios from 'axios';
import { router } from '@/lib/navigation';
import { ParticipantPhotoStore } from '@/lib/participant-photos';

const Photos = createContext<ParticipantPhotoStore | null>(null);
export function AccountPhotoProvider({ context, children }: { context?: string | null; children: ReactNode }) {
    // Vive sobre las páginas. Cambiar identidad/contexto crea un almacén vacío en el mismo render,
    // sin esperar un efecto ni mostrar un frame con la fotografía de la cuenta anterior.
    const store = useMemo(() => context ? new ParticipantPhotoStore(async (url, signal) =>
        (await axios.get(url, { signal, headers: { 'X-TDV2-Photo-Context': context } })).data, context) : null, [context]);
    useEffect(() => {
        // Sólo una notificación efímera; no transmite imágenes, identidades ni credenciales.
        const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('tdv2-photo-context') : null;
        if (channel) channel.onmessage = () => store?.clear();
        const off = router.on('identity-changing', () => { store?.clear(); channel?.postMessage('clear'); });
        return () => { off(); channel?.close(); store?.dispose(); };
    }, [store]);
    return <Photos.Provider value={store}>{children}</Photos.Provider>;
}
export function useAccountPhoto() {
    const store = useContext(Photos);
    const subscribe = useCallback((notify: () => void) => store?.subscribe('/user/photo', notify) ?? (() => {}), [store]);
    return useSyncExternalStore(subscribe, () => store?.photo('/user/photo') ?? null);
}
