import type { ReactNode } from 'react';
import { Typography } from '@mui/material';
export default function PageHeading({
    title,
    description,
    actions,
}: {
    title: string;
    description?: string;
    actions?: ReactNode;
}) {
    return (
        <div className="page-heading">
            <div>
                <Typography component="h1" variant="h1">
                    {title}
                </Typography>
                {description && (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        {description}
                    </Typography>
                )}
            </div>
            {actions && <div className="row-actions">{actions}</div>}
        </div>
    );
}
