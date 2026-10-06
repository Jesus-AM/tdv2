import type { ReactNode } from 'react';
import { Button, Typography } from '@mui/material';
import { ArrowBack } from '@mui/icons-material';
import { router } from '@/lib/navigation';
export default function PageHeading({
    title,
    description,
    actions,
    back,
    className,
}: {
    title: string;
    description?: string;
    actions?: ReactNode;
    back?: { label: string; href: string };
    className?: string;
}) {
    return (
        <header className={['page-header', className].filter(Boolean).join(' ')}>
            {back && (
                <Button className="page-back" startIcon={<ArrowBack />} onClick={() => router.visit(back.href)}>
                    {back.label}
                </Button>
            )}
            <div className="page-heading">
                <div className="page-heading-copy">
                    <Typography component="h1" variant="h1">
                        {title}
                    </Typography>
                    {description && (
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                            {description}
                        </Typography>
                    )}
                </div>
                {actions && <div className="row-actions">{actions}</div>}
            </div>
        </header>
    );
}
