import { Tabs, Tab } from '@mui/material';
import { router } from '@/lib/navigation';
const modes = [['vista', 'Vista por rol y área', 'rol-area'], ['usuario', 'Actuar como usuario', 'actuar-como-usuario']] as const;
export default function AccessModeTabs({ value }: { value: 'vista' | 'usuario' }) {
    return <Tabs value={value} aria-label="Modalidad de pruebas de acceso" variant="scrollable" scrollButtons="auto" allowScrollButtonsMobile
        sx={{ mb: 2, borderBottom: '1px solid', borderColor: 'divider' }}
        onChange={(_, selected) => router.visit('/configuracion/pruebas-acceso/' + modes.find(m => m[0] === selected)![2])}>
        {modes.map(([key, label]) => <Tab key={key} value={key} label={label} />)}
    </Tabs>;
}
