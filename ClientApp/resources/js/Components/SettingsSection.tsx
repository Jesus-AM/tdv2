import { useId, type ReactNode } from 'react';
import { Box, Typography } from '@mui/material';

/** Agrupa una configuración con su explicación y sus acciones, sin alterar el editor. */
export default function SettingsSection({ title, description, icon, children, className = '' }: {
    title: string; description?: string; icon?: ReactNode; children?: ReactNode; className?: string;
}) {
    const titleId = useId();
    return <Box component="section" className={`surface settings-section ${className}`} aria-labelledby={titleId}>
        <div className="settings-section-heading">
            {icon && <span className="settings-section-icon" aria-hidden="true">{icon}</span>}
            <div className="settings-section-copy">
                <Typography id={titleId} component="h2" variant="h2">{title}</Typography>
                {description && <Typography variant="body2" color="text.secondary">{description}</Typography>}
            </div>
        </div>
        {children && <div className="settings-section-content">{children}</div>}
    </Box>;
}

