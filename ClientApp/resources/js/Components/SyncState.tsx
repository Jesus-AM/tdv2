import { Chip } from '@mui/material';
import { CheckCircleOutlined, ErrorOutlined, PendingOutlined, SyncOutlined, WarningAmber } from '@mui/icons-material';

const states = {
    completada: { label: 'Completada', color: 'success', Icon: CheckCircleOutlined },
    ejecutando: { label: 'En curso', color: 'primary', Icon: SyncOutlined },
    pendiente: { label: 'En espera', color: 'default', Icon: PendingOutlined },
    fallida: { label: 'Fallida', color: 'error', Icon: ErrorOutlined },
    parcial: { label: 'Parcial', color: 'warning', Icon: WarningAmber },
    sin_resultado: { label: 'Sin resultado disponible', color: 'default', Icon: PendingOutlined },
    sin_intento: { label: 'Sin intento disponible', color: 'default', Icon: PendingOutlined },
    no_disponible: { label: 'No disponible', color: 'error', Icon: ErrorOutlined },
} as const;
export default function SyncState({ state }: { state: string }) {
    const view = states[state as keyof typeof states] || states.sin_resultado;
    return <Chip icon={<view.Icon />} label={view.label} color={view.color} variant="outlined" size="small" />;
}
