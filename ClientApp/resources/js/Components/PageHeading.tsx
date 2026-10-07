import type { ReactNode } from 'react';
import { Breadcrumbs, Button, Link, Typography } from '@mui/material';
import { ArrowBack, NavigateNext } from '@mui/icons-material';
import { router } from '@/lib/navigation';
export default function PageHeading({
    title,
    description,
    actions,
    back,
    breadcrumbs,
    className,
}: {
    title: string;
    description?: string;
    actions?: ReactNode;
    back?: { label: string; href: string };
    breadcrumbs?: { label: string; href: string }[];
    className?: string;
}) {
    return (
        <header className={['page-header', className].filter(Boolean).join(' ')}>
            {breadcrumbs?.length ? (
                <Breadcrumbs className="page-breadcrumbs" aria-label="Ubicación" separator={<NavigateNext sx={{ fontSize: 16 }} />} maxItems={8}>
                    {breadcrumbs.map(crumb => <Link key={crumb.href} href={crumb.href} underline="hover" color="inherit"
                        onClick={event => { if (!event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) { event.preventDefault(); router.visit(crumb.href); } }}>{crumb.label}</Link>)}
                    <Typography component="span" aria-current="page">{title}</Typography>
                </Breadcrumbs>
            ) : back && (
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
                        <Typography className="page-description" variant="body2" color="text.secondary">
                            {description}
                        </Typography>
                    )}
                </div>
                {actions && <div className="row-actions">{actions}</div>}
            </div>
        </header>
    );
}
