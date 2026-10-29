import { useState } from 'react';
import { Alert, Button } from '@mui/material';
import { router } from '@/lib/navigation';
import type { ApplicationOption } from '@/types/page';

export default function AccessActions({ destination, options = [] }: { destination: string; options?: ApplicationOption[] }) {
    const [busy, setBusy] = useState(false), [error, setError] = useState('');
    return <>
        <div className="row-actions">
            <Button variant="contained" href="/">Volver al portal</Button>
            <Button variant="outlined" disabled={busy} onClick={() => {
                setBusy(true); setError('');
                router.post('/session/use-another-account?returnUrl=' + encodeURIComponent(destination), {}, {
                    onError: errors => setError(errors.general || 'No fue posible cambiar de cuenta. Inténtalo de nuevo.'),
                    onFinish: () => setBusy(false),
                });
            }}>Cambiar de cuenta</Button>
            {options.map(option => <Button key={option.path} href={option.path}>{option.name}</Button>)}
        </div>
        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
    </>;
}
