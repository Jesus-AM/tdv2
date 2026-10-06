import { useEffect, useRef, useState } from 'react';
import { Head, router, usePage } from '@/lib/navigation';
import axios from 'axios';
import {
    Alert,
    Box,
    Button,
    Checkbox,
    Chip,
    FormControlLabel,
    IconButton,
    MenuItem,
    Tab,
    Tabs,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    TextField,
    Typography,
    LinearProgress,
    Tooltip,
    Radio,
    RadioGroup,
    Dialog, DialogTitle, DialogContent, DialogActions,
} from '@mui/material';
import {
    Add,
    ArrowBack,
    DeleteOutlined,
    DownloadOutlined,
    SaveOutlined,
    CheckCircleOutlined,
    CloudSyncOutlined,
} from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import FormProposalComparison from '@/Components/FormProposalComparison';
import BlockEditingStatus from '@/Components/BlockEditingStatus';
import { splitBlocks } from '@/lib/form-blocks';
import PrioritySelect from '@/Components/PrioritySelect';
import RemoveRowDialog from '@/Components/RemoveRowDialog';
import FormSubmissionReview from '@/Components/FormSubmissionReview';
import type { FormPending } from '@/Components/FormSubmissionReview';
import { removalBlocks, removeRow, rowLabel, targetRow } from '@/lib/form-deletion';
import type { RowTarget } from '@/lib/form-deletion';
import { displayUnitCode } from '@/lib/area-directory';
import { BlockEditor } from '@/lib/block-editor';
import type { EditorState, LiveForm } from '@/lib/block-editor';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { FormContent, Unit, ProcessRow } from '@/types/tdv2';
type Definition = {
    criterios: string[];
    medios: string[];
    preguntas: { texto: string; tipo: string; opciones?: string[] }[];
};
type Props = {
    unidad: Unit;
    contenido: FormContent;
    plantilla: FormContent;
    editable: boolean;
    permisoEdicion: boolean;
    version: number;
    porcentaje: number;
    actualizadoEn: string | null;
    actualizadoPor: string | null;
    guardarUrl: string;
    puedeEnviar: boolean;
    enviadoEn: string | null;
    enviadoPor: string | null;
    definicion: Definition;
    ilda?: { estado: string; nuevos: number; total: number; aviso: string | null };
};
type Field = {
    key: string;
    label: string;
    options?: { value: string; label: string }[];
    type?: string;
    wide?: boolean;
};
const tabs = [
    ['contexto', 'Contexto'],
    ['identificacion', 'Identificación general'],
    ['sistemas', 'Sistemas y herramientas'],
    ['datos', 'Datos'],
    ['evaluacion', 'Evaluación'],
    ['preguntas', 'Preguntas'],
    ['acuerdos', 'Acuerdos'],
];
const validation = [
    { value: 'V', label: 'V · Vigente' },
    { value: 'A', label: 'A · Ajustar' },
    { value: 'D', label: 'D · Duplicado o relacionado' },
    { value: 'N', label: 'N · No corresponde' },
];
const id = () => crypto.randomUUID();
export default function FormatoUR(props: Props) {
    const page = usePage();
    return (
        <AuthenticatedLayout>
            <Editor
                key={`${props.unidad.id_ur}:${page.props.contextoEdicion}:${page.props.simulacion?.expiresAt || ''}`}
                {...props}
            />
        </AuthenticatedLayout>
    );
}
function Editor(props: Props) {
    const { props: shared } = usePage();
    const [active, setActive] = useState('contexto'),
        [evalCode, setEvalCode] = useState(''),
        [notice, setNotice] = useState(''),
        [confirmSend, setConfirmSend] = useState<number | null>(null),
        [removal, setRemoval] = useState<(RowTarget & { snapshot: string }) | null>(null),
        [sending, setSending] = useState(false),
        [finishing, setFinishing] = useState(false),
        [compare, setCompare] = useState<{ live: LiveForm; keys: string[] } | null>(null);
    const modalBlocks = useRef<string[]>([]);
    const [openSelect, setOpenSelect] = useState<{ key: string; field: string } | null>(null);
    const [resolving, setResolving] = useState(false), [resolutionError, setResolutionError] = useState('');
    const redraw = useRef<(s: EditorState) => void>(() => {});
    const [engine] = useState<BlockEditor>(
        () => {
            const editor: BlockEditor = new BlockEditor(
                props.contenido,
                props.version,
                props.porcentaje,
                props.editable,
                {
                    read: async () => (await axios.get(`${props.guardarUrl}/estado`, { params: { tab: editor.tabId } })).data,
                    reserve: async body => (await axios.post(`${props.guardarUrl}/reservas`, body)).data,
                    renew: async body => (await axios.post(`${props.guardarUrl}/reservas/actividad`, body)).data,
                    save: async body => (await axios.patch(`${props.guardarUrl}/bloques`, body)).data,
                    release: async body => (await axios.post(`${props.guardarUrl}/reservas/liberar`, body)).data,
                    submit: async body => (await axios.post(`${props.guardarUrl}/enviar`, body)).data,
                },
                (s) => redraw.current(s),
            );
            return editor;
        },
    );
    const [state, setState] = useState({ ...engine.state });
    redraw.current = setState;
    const c = state.content,
        locked = state.locked || !state.initialized;
    const initializedSection = useRef(false);
    useEffect(() => {
        // Perder la reserva cierra el menú; una adquisición posterior no debe reabrir una intención antigua.
        if (openSelect && !engine.canEdit(openSelect.key)) setOpenSelect(null);
    }, [engine, openSelect, state]);
    useEffect(() => {
        if (state.initialized && !initializedSection.current) {
            initializedSection.current = true;
            setActive(tabs.some(([key]) => key === state.section) ? state.section : 'contexto');
        }
    }, [state.initialized, state.section]);
    useEffect(() => {
        let disposed = false;
        const connection = new HubConnectionBuilder().withUrl('/form-events', {
            headers: { 'X-CSRF-TOKEN': String(shared.csrfToken || ''), 'X-TDV2-Context': shared.contextoEdicion || 'own' },
        }).withAutomaticReconnect([0, 2000, 5000, 10000]).configureLogging(LogLevel.None).build();
        const watch = () => connection.stream<LiveForm>('Watch', props.unidad.id_ur, shared.contextoEdicion || 'own', engine.tabId)
            .subscribe({ next: value => engine.receive(value), complete: () => {}, error: () => { engine.connectionLost(); void engine.refresh(); } });
        void engine.refresh();
        connection.onreconnecting(() => engine.connectionLost());
        connection.onreconnected(() => { void engine.refresh(); watch(); });
        connection.onclose(() => { if (!disposed) engine.connectionLost(); });
        const offline = () => engine.connectionLost(), online = () => { void engine.refresh(); };
        window.addEventListener('offline', offline); window.addEventListener('online', online);
        void connection.start().then(() => { if (!disposed) watch(); }).catch(() => engine.connectionLost());
        return () => { disposed = true; window.removeEventListener('offline', offline); window.removeEventListener('online', online); void connection.stop(); };
    }, [engine, props.unidad.id_ur, shared.contextoEdicion, shared.csrfToken]);
    async function resolveBlock(key: string) {
        modalBlocks.current = engine.proposalKeys(key); setResolutionError('');
        try {
            const live = (await axios.get(`${props.guardarUrl}/estado`, { params: { tab: engine.tabId } })).data;
            setCompare({ live, keys: modalBlocks.current });
        } catch { setNotice('No se pudo consultar la respuesta guardada. Tu propuesta se conserva en este registro.'); modalBlocks.current = []; }
    }
    async function refreshComparison() {
        try {
            const live = (await axios.get(`${props.guardarUrl}/estado`, { params: { tab: engine.tabId } })).data;
            setCompare(previous => previous && { ...previous, live });
        } catch { /* Conservar la comparación y la propuesta si sigue desconectado. */ }
    }
    function blockNotice(key: string) {
        return <BlockEditingStatus engine={engine} state={state} blockKey={key} onResolve={() => void resolveBlock(key)} />;
    }
    function blockEvents(key: string) {
        return { 'data-edit-block': key, 'data-edit-state': engine.busy(key) ? 'occupied' : engine.canEdit(key) ? 'owned' : 'idle',
            onFocusCapture: () => { void engine.focus(key); },
            onPointerDownCapture: (event: React.PointerEvent<HTMLElement>) => {
                // Un campo que conservó el foco tras vencer o liberarse una reserva también puede retomarse.
                // Consultar el código o seleccionar texto fuera de los campos no inicia una reserva.
                if (!engine.canEdit(key) && event.target instanceof Element
                    && event.target.closest('input, textarea, label, [role="combobox"]')) void engine.focus(key);
            },
            onKeyDownCapture: (event: React.KeyboardEvent<HTMLElement>) => {
                if (!engine.canEdit(key) && !['Tab', 'Escape'].includes(event.key)) void engine.focus(key);
            },
            onBlurCapture: () => {
                // Select y Dialog usan portales: comprobar el foco final, no el blur intermedio hacia body.
                setTimeout(() => {
                    const target = document.activeElement;
                    if (modalBlocks.current.includes(key) || target?.closest('[role="listbox"]') || document.querySelector('[role="listbox"]')) return;
                    if (target?.closest('[data-edit-block]')?.getAttribute('data-edit-block') !== key) void engine.endBlock(key);
                }, 0);
            } };
    }
    function choiceDisabled(key: string) { return locked || engine.busy(key) || !!state.issues[key]; }
    async function changeChoice(key: string, edit: (data: FormContent) => void) {
        // Las casillas y radios reciben foco sin habilitar escrituras: confirmar primero y usar el contenido vigente.
        if (engine.canEdit(key) || await engine.focus(key)) engine.change(edit);
    }
    function selectEvents(key: string, field: string) {
        return { readOnly: choiceDisabled(key), open: openSelect?.key === key && openSelect.field === field && engine.canEdit(key),
            onOpen: async () => { if (await engine.focus(key)) setOpenSelect({ key, field }); },
            onClose: () => setOpenSelect(null) };
    }
    const coded = c.identificacion.filter((r) => r.codigo);
    const selectedCode = coded.some((r) => r.codigo === evalCode) ? evalCode : coded[0]?.codigo || '';
    const processOptions = coded.map((r) => ({ value: r.codigo, label: `${r.codigo} · ${r.tramite}` }));
    useEffect(() => {
        const before = (e: BeforeUnloadEvent) => {
            if (engine.state.dirty || engine.state.saving) {
                e.preventDefault();
                e.returnValue = '';
            }
        };
        const remove = router.on('before', (event) => {
            if (
                (engine.state.dirty || engine.state.saving) &&
                !window.confirm('Tienes cambios sin guardar. ¿Salir de este formato?')
            )
                event.preventDefault();
        });
        window.addEventListener('beforeunload', before);
        return () => {
            engine.dispose();
            remove();
            window.removeEventListener('beforeunload', before);
        };
    }, [engine]);
    const change = (edit: (data: FormContent) => void) => engine.change(edit);
    function download() {
        const blob = new Blob([JSON.stringify({ ur: props.unidad, version: state.version, contenido: c }, null, 2)], {
            type: 'application/json',
        });
        const url = URL.createObjectURL(blob),
            a = document.createElement('a');
        a.href = url;
        a.download = `borrador-UR-${props.unidad.cve_ur.replace(/[^a-z0-9_-]/gi, '_')}.json`;
        a.click();
        URL.revokeObjectURL(url);
    }
    function processEdit(rowId: string, key: string, value: string) {
        if (!engine.canEdit(`identificacion:${rowId}`)) return;
        void engine.edit((d) => {
            const row = d.identificacion.find(item => item.id === rowId);
            if (!row) return;
            Object.assign(row, { [key]: value });
            if (key === 'validacion' && value && !row.codigo) {
                const max = Math.max(0, ...d.identificacion.map((r) => Number(r.codigo.match(/^PO-(\d+)$/)?.[1] || 0)));
                row.codigo = `PO-${String(max + 1).padStart(2, '0')}`;
            }
            if (row.codigo && !d.evaluaciones[row.codigo])
                d.evaluaciones[row.codigo] = props.definicion.criterios.map((criterio) => ({
                    criterio,
                    valor: '',
                    obs: '',
                }));
        });
    }
    function removalReason(target: RowTarget) {
        if (!state.initialized) return 'Espera a que termine de cargar el formato.';
        if (state.submitted || props.enviadoEn) return 'El formato enviado está bloqueado para edición.';
        if (locked) return 'Tu acceso actual es de solo consulta.';
        const row = targetRow(c, target);
        if (!row) return 'Otra sesión ya retiró este registro.';
        if (row.id.startsWith('ilda:')) return 'Los registros de ILDA se conservan. Selecciona N en Validación si no corresponde.';
        if (removalBlocks(c, target).some(key => engine.busy(key))) return 'Otra sesión está editando este registro o una de sus relaciones.';
        if (!engine.canEdit(`${target.section}:${target.id}`)) return 'Entra en un campo del registro y espera la confirmación de la reserva.';
        if (removalBlocks(c, target).some(key => state.issues[key])) return 'Resuelve los cambios pendientes de este registro antes de eliminarlo.';
        return '';
    }
    function removeButton(target: RowTarget, label: string) {
        const reason = removalReason(target);
        return <Tooltip title={reason || 'Eliminar registro'}><span tabIndex={reason ? 0 : undefined}>
            <IconButton aria-label={label} color="error" disabled={!!reason}
                onClick={() => { modalBlocks.current = removalBlocks(c, target); setRemoval({ ...target, snapshot: JSON.stringify(targetRow(c, target)) }); }}>
                <DeleteOutlined fontSize="small" />
            </IconButton>
        </span></Tooltip>;
    }
    function navigatePending(pending: FormPending) {
        void engine.flush(true); setActive(pending.seccion); engine.setSection(pending.seccion);
        if (pending.seccion === 'evaluacion') setEvalCode(pending.bloque.split(':')[1]);
        requestAnimationFrame(() => requestAnimationFrame(() => {
            const panel = document.getElementById('panel-' + pending.seccion);
            const block = Array.from(panel?.querySelectorAll<HTMLElement>('[data-edit-block]') || [])
                .find(element => element.dataset.editBlock === pending.bloque);
            const field = block?.querySelector<HTMLElement>('[data-field="' + pending.campo + '"]');
            const focus = field?.matches('[tabindex="-1"]') ? field.parentElement?.querySelector<HTMLElement>('[role="combobox"]') : field;
            (focus || block?.querySelector<HTMLElement>('input:not([type="hidden"]), textarea, [role="combobox"]') || panel)?.focus();
            (block || panel)?.scrollIntoView({ block: 'center' });
        }));
    }
    async function reviewForSubmission() {
        setSending(true);
        try {
            if (await engine.flush(true) && await engine.refresh() && !engine.state.dirty && !engine.state.saving
                && engine.state.canSubmit && engine.state.review?.listo) setConfirmSend(engine.state.version);
        } finally { setSending(false); }
    }
    function field(value: string, def: Field, label: string, onChange: (value: string) => void, blockKey?: string) {
        const readOnly = locked || !!blockKey && !engine.canEdit(blockKey);
        if (def.key === 'prioridad') return <PrioritySelect value={value} label={label} onChange={onChange} {...selectEvents(blockKey!, def.key)} />;
        return (
            <>
                <TextField
                    className={def.wide ? 'wide-field' : ''}
                    fullWidth
                    select={!!def.options}
                    multiline={!def.options && !def.type}
                    minRows={!def.options && !def.type ? 2 : undefined}
                    maxRows={!def.options && !def.type ? 6 : undefined}
                    type={def.type || 'text'}
                    value={value || ''}
                    onChange={(e) => onChange(e.target.value)}
                    slotProps={{
                        input: { readOnly },
                        select: { ...(blockKey ? selectEvents(blockKey, def.key) : { readOnly }), SelectDisplayProps: { 'aria-label': label, ...{ 'data-field': def.key } } },
                        htmlInput: {
                            'aria-label': label,
                            'data-field': def.key,
                            min: def.type === 'number' ? 1 : undefined,
                            max: def.type === 'number' ? 9999 : undefined,
                        },
                    }}
                >
                    {def.options && [
                        <MenuItem key="empty" value="">
                            Sin seleccionar
                        </MenuItem>,
                        ...def.options.map((o) => (
                            <MenuItem key={o.value} value={o.value}>
                                {o.label}
                            </MenuItem>
                        )),
                    ]}
                </TextField>
            </>
        );
    }
    function table(section: 'sistemas' | 'datos' | 'acuerdos', fields: Field[]) {
        const rows = c[section];
        return (
            <>
                <div className="capture-scroll">
                    <Table size="small" aria-label={tabs.find((t) => t[0] === section)?.[1]}>
                        <TableHead>
                            <TableRow>
                                {fields.map((f) => (
                                    <TableCell key={f.key}>{f.label}</TableCell>
                                ))}
                                <TableCell>Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {rows.map((r, i) => (
                                <TableRow key={r.id} {...blockEvents(`${section}:${r.id}`)}>
                                    {fields.map((f) => (
                                        <TableCell key={f.key}>
                                            {field(
                                                String((r as unknown as Record<string, string>)[f.key] || ''),
                                                f,
                                                `${f.label} ${i + 1}`,
                                                (value) =>
                                                    change((d) => { const row = d[section].find(item => item.id === r.id); if (row) Object.assign(row, { [f.key]: value }); }),
                                                `${section}:${r.id}`,
                                            )}
                                        </TableCell>
                                    ))}
                                    <TableCell>
                                        {blockNotice(`${section}:${r.id}`)}
                                        {removeButton({ section, id: r.id }, `Retirar fila ${i + 1} de ${section}`)}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
                <Button

                    sx={{ mt: 2 }}
                    startIcon={<Add />}
                    disabled={locked || rows.length >= 200}
                    onClick={() => {
                        const row = { ...props.plantilla[section][0], id: id() };
                        void engine.edit((d) => {
                            if (section === 'sistemas') d.sistemas.push(row as FormContent['sistemas'][0]);
                            else if (section === 'datos') d.datos.push(row as FormContent['datos'][0]);
                            else d.acuerdos.push(row as FormContent['acuerdos'][0]);
                        });
                    }}
                >
                    Agregar fila
                </Button>
            </>
        );
    }
    const processFields: Field[] = [
        { key: 'tramite', label: 'Trámite / servicio', wide: true },
        { key: 'usuario', label: 'Usuario que atiende' },
        { key: 'resultado', label: 'Resultado o documento' },
        { key: 'responsable', label: 'Responsable' },
        { key: 'validacion', label: 'Validación', options: validation },
        { key: 'prioridad', label: 'Prioridad' },
    ];
    const sections: Record<string, () => React.ReactNode> = {
        contexto: () => (
            <div className="stack">
                <Typography variant="h2">Objetivo de la sesión</Typography>
                <Typography variant="body2">
                    Validar el inventario preliminar e identificar quién realiza cada trámite, servicio o actividad, a
                    quién atiende, qué resultado genera y qué observaciones deben considerarse.
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    Este formato pertenece a {props.unidad.desc_ur}. El responsable y los colaboradores autorizados
                    comparten las respuestas y el avance.
                </Typography>
                <Typography variant="h2">Cómo llenar el formato</Typography>
                <Box component="ol" sx={{ pl: 3, m: 0, color: 'text.secondary', fontSize: 14, lineHeight: 2 }}>
                    {[
                        'Revisa el inventario del área y confirma la vigencia de cada registro.',
                        'Ajusta los nombres y completa usuario, resultado y responsable.',
                        'Asigna validación y prioridad a cada proceso.',
                        'Relaciona sus sistemas y datos, completa la evaluación y registra acuerdos.',
                    ].map((t) => (
                        <li key={t}>{t}</li>
                    ))}
                </Box>
            </div>
        ),
        identificacion: () => (
            <>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, mb: 1, flexWrap: 'wrap' }}>
                    <Typography variant="h2" component="h2">
                        Identificación general
                    </Typography>
                    <Chip label={`${c.identificacion.length} registros`} variant="outlined" />
                </Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Revisa un trámite o servicio por fila. La validación asigna su código para relacionarlo con
                    sistemas, datos y evaluación.
                </Typography>
                {props.ilda?.aviso && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        {props.ilda.aviso}
                    </Alert>
                )}
                {!!props.ilda?.nuevos && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {props.ilda.nuevos} registros nuevos de ILDA se incorporaron al inventario de esta área.
                        {props.editable ? ' Se guardarán al guardar el formato o completar una respuesta.' : ''}
                    </Typography>
                )}
                <div className="capture-scroll">
                    <Table size="small" aria-label="Identificación general">
                        <TableHead>
                            <TableRow>
                                <TableCell>Código / Fuente</TableCell>
                                {processFields.map((f) => (
                                    <TableCell key={f.key}>{f.label}</TableCell>
                                ))}
                                <TableCell>Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {c.identificacion.map((r, i) => (
                                <TableRow key={r.id} {...blockEvents(`identificacion:${r.id}`)}>
                                    <TableCell>
                                        <Typography
                                            variant="body2"
                                            sx={{ fontWeight: 600, mb: 1, whiteSpace: 'nowrap' }}
                                        >
                                            {r.codigo || 'Por validar'}
                                        </Typography>
                                        <Chip
                                            label={r.fuente || 'Nuevo'}
                                            color={r.fuente === 'ILDA' ? 'primary' : 'default'}
                                            variant="outlined"
                                        />
                                        {blockNotice(`identificacion:${r.id}`)}
                                    </TableCell>
                                    {processFields.map((f) => (
                                        <TableCell key={f.key}>
                                            {field(r[f.key as keyof ProcessRow], f, `${f.label} ${i + 1}`, (value) => {
                                                setNotice('');
                                                processEdit(r.id, f.key, value);
                                            }, `identificacion:${r.id}`)}
                                        </TableCell>
                                    ))}
                                    <TableCell>
                                        {removeButton({ section: 'identificacion', id: r.id }, `Retirar proceso ${i + 1}`)}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
                <Box className="row-actions" sx={{ mt: 2, justifyContent: 'space-between' }}>
                    <Button
                        startIcon={<Add />}
                        disabled={locked || c.identificacion.length >= 200}
                        onClick={() => {
                            const row = { ...props.plantilla.identificacion[0], id: id() };
                            void engine.edit(d => { d.identificacion.push(row); });
                        }}
                    >
                        Agregar trámite o servicio
                    </Button>
                    <Typography variant="caption" color="text.secondary">
                        V: vigente · A: ajustar · D: duplicado o relacionado · N: no corresponde
                    </Typography>
                </Box>
            </>
        ),
        sistemas: () => (
            <>
                <Typography variant="h2" sx={{ mb: 1 }}>
                    Sistemas y herramientas
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Relaciona cada herramienta con un proceso validado.
                </Typography>
                {table('sistemas', [
                    { key: 'proceso', label: 'Proceso', options: processOptions, wide: true },
                    { key: 'sistema', label: 'Sistema o herramienta' },
                    { key: 'uso', label: 'Uso' },
                    {
                        key: 'estado',
                        label: 'Estado',
                        options: ['Funciona', 'Parcial', 'No funciona', 'No se usa'].map((v) => ({
                            value: v,
                            label: v,
                        })),
                    },
                    { key: 'fallas', label: 'Fallas u observaciones' },
                ])}
                <Typography variant="h3" sx={{ mt: 3, mb: 1 }}>
                    Medios utilizados
                </Typography>
                <Box component="fieldset" {...blockEvents('medios')} sx={{ border: 0, p: 0, m: 0, minWidth: 0 }}>
                {blockNotice('medios')}
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    {Object.keys(c.medios).map((m) => (
                        <FormControlLabel
                            key={m}
                            control={
                                <Checkbox
                                    checked={c.medios[m]}
                                    disabled={choiceDisabled('medios')}
                                    onChange={(e) => { const checked = e.target.checked;
                                        void changeChoice('medios', d => { d.medios[m] = checked; }); }}
                                />
                            }
                            label={<Typography variant="body2">{m}</Typography>}
                        />
                    ))}
                </Box>
                <TextField
                    label="Otro medio"
                    value={c.medioOtro}
                    onChange={(e) =>
                        change((d) => {
                            d.medioOtro = e.target.value;
                        })
                    }
                    slotProps={{ input: { readOnly: !engine.canEdit('medios') } }}
                    sx={{ mt: 1, minWidth: 250 }}
                />
                </Box>
            </>
        ),
        datos: () => (
            <>
                <Typography variant="h2" sx={{ mb: 2 }}>
                    Datos e información
                </Typography>
                {table('datos', [
                    { key: 'proceso', label: 'Proceso', options: processOptions, wide: true },
                    { key: 'dato', label: 'Dato o información' },
                    { key: 'fuente', label: 'Fuente' },
                    {
                        key: 'origen',
                        label: 'Origen',
                        options: [
                            'Se origina en este proceso',
                            'Se origina en otro proceso',
                            'Se genera en otra instancia',
                        ].map((v) => ({ value: v, label: v })),
                    },
                    { key: 'detalle', label: 'Detalle del origen' },
                ])}
            </>
        ),
        evaluacion: () => (
            <>
                <Typography variant="h2" sx={{ mb: 1 }}>
                    Evaluación por proceso
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Valora cada criterio de 1 a 5. Registra observaciones que ayuden a mejorar el proceso.
                </Typography>
                {!coded.length ? (
                    <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                        Valida al menos un trámite en Identificación general para habilitar su evaluación.
                    </Typography>
                ) : (
                    <>
                        <TextField

                            select
                            label="Proceso a evaluar"
                            value={selectedCode}
                            onChange={(e) => setEvalCode(e.target.value)}
                            fullWidth
                            sx={{ mb: 3, maxWidth: 650 }}
                        >
                            {processOptions.map((o) => (
                                <MenuItem key={o.value} value={o.value}>
                                    {o.label}
                                </MenuItem>
                            ))}
                        </TextField>
                        {[selectedCode].map((code) => (
                            <Box key={code}>
                                <Typography variant="h3" sx={{ mb: 1 }}>
                                    {processOptions.find((o) => o.value === code)?.label}
                                </Typography>
                                {props.definicion.criterios.map((criterion, i) => (
                                    <Box
                                        key={criterion}
                                        component="fieldset"
                                        {...blockEvents(`evaluaciones:${code}:${i}`)}
                                        sx={{
                                            border: 0, m: 0, px: 0, minWidth: 0,
                                            display: 'grid',
                                            gridTemplateColumns: {
                                                xs: '1fr',
                                                lg: 'minmax(170px,1fr) 255px minmax(180px,1fr)',
                                            },
                                            gap: 2,
                                            alignItems: 'center',
                                            py: 1.5,
                                            borderBottom: '1px solid',
                                            borderColor: 'divider',
                                        }}
                                    >
                                        <Box><Typography variant="body2">{criterion}</Typography>{blockNotice(`evaluaciones:${code}:${i}`)}</Box>
                                        <RadioGroup
                                            row
                                            aria-label={`${criterion} de ${code}`}
                                            value={c.evaluaciones[code]?.[i]?.valor || ''}
                                            onChange={(e) => { const value = e.target.value;
                                                void changeChoice(`evaluaciones:${code}:${i}`, (d) => {
                                                    if (!d.evaluaciones[code])
                                                        d.evaluaciones[code] = props.definicion.criterios.map(
                                                            (criterio) => ({ criterio, valor: '', obs: '' }),
                                                        );
                                                    d.evaluaciones[code][i].valor = value;
                                                }); }}
                                        >
                                            {['1', '2', '3', '4', '5'].map((v) => (
                                                <FormControlLabel
                                                    key={v}
                                                    value={v}
                                                    disabled={choiceDisabled(`evaluaciones:${code}:${i}`)}
                                                    control={<Radio size="small" sx={{ p: 0.6 }} />}
                                                    label={<Typography variant="caption">{v}</Typography>}
                                                    sx={{ mr: 1, ml: 0 }}
                                                />
                                            ))}
                                        </RadioGroup>
                                        <TextField
                                            label={`Observación: ${criterion}`}
                                            value={c.evaluaciones[code]?.[i]?.obs || ''}
                                            multiline
                                            maxRows={4}
                                            slotProps={{ input: { readOnly: !engine.canEdit(`evaluaciones:${code}:${i}`) } }}
                                            onChange={(e) =>
                                                change((d) => {
                                                    if (!d.evaluaciones[code])
                                                        d.evaluaciones[code] = props.definicion.criterios.map(
                                                            (criterio) => ({ criterio, valor: '', obs: '' }),
                                                        );
                                                    d.evaluaciones[code][i].obs = e.target.value;
                                                })
                                            }
                                        />
                                    </Box>
                                ))}
                            </Box>
                        ))}
                    </>
                )}
            </>
        ),
        preguntas: () => (
            <>
                <Typography variant="h2" sx={{ mb: 2 }}>
                    Preguntas generales
                </Typography>
                <Box className="stack">
                    {c.preguntas.map((q, i) => (
                        <Box key={q.pregunta} component="fieldset" {...blockEvents(`preguntas:${i}`)}
                            sx={{ p: 0, m: 0, minWidth: 0, border: 0, pb: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
                            {blockNotice(`preguntas:${i}`)}
                            <Box
                                sx={{
                                    display: 'flex',
                                    justifyContent: 'space-between',
                                    gap: 2,
                                    alignItems: 'center',
                                    mb: 1,
                                }}
                            >
                                <Typography variant="body2" sx={{ fontWeight: 500 }}>
                                    {i + 1}. {q.pregunta}
                                </Typography>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            size="small"
                                            checked={q.marcada}
                                            disabled={choiceDisabled(`preguntas:${i}`)}
                                            onChange={(e) => { const checked = e.target.checked;
                                                void changeChoice(`preguntas:${i}`, d => { d.preguntas[i].marcada = checked; }); }}
                                        />
                                    }
                                    label={<Typography variant="caption">Revisada</Typography>}
                                />
                            </Box>
                            <TextField
                                fullWidth
                                select={q.tipo === 'opcion'}
                                multiline={q.tipo === 'abierta'}
                                minRows={q.tipo === 'abierta' ? 2 : undefined}
                                label={`Respuesta ${i + 1}`}
                                value={q.respuesta}
                                slotProps={{ input: { readOnly: !engine.canEdit(`preguntas:${i}`) }, select: selectEvents(`preguntas:${i}`, 'respuesta') }}
                                onChange={(e) =>
                                    change((d) => {
                                        d.preguntas[i].respuesta = e.target.value;
                                    })
                                }
                            >
                                {q.tipo === 'opcion' && [
                                    <MenuItem key="none" value="">
                                        Sin responder
                                    </MenuItem>,
                                    ...(q.opciones || []).map((o) => (
                                        <MenuItem value={o} key={o}>
                                            {o}
                                        </MenuItem>
                                    )),
                                ]}
                            </TextField>
                        </Box>
                    ))}
                </Box>
            </>
        ),
        acuerdos: () => (
            <>
                <Typography variant="h2" sx={{ mb: 2 }}>
                    Acuerdos y próximos pasos
                </Typography>
                {table('acuerdos', [
                    { key: 'acuerdo', label: 'Acuerdo', wide: true },
                    { key: 'responsable', label: 'Responsable' },
                    { key: 'fecha', label: 'Fecha compromiso', type: 'date' },
                ])}
                <FormSubmissionReview review={state.review} canSubmit={state.canSubmit}
                    settled={state.initialized && !state.dirty && !state.saving && !finishing && !state.error && !state.conflict}
                    submitting={sending} submitted={!!(state.submitted || props.enviadoEn)}
                    onNavigate={navigatePending} onSubmit={() => void reviewForSubmission()} />
            </>
        ),
    };
    return (
        <>
            <Head title={`Procesos operativos · ${displayUnitCode(props.unidad.cve_ur)}`} />
            <div className="page">
                <Button

                    startIcon={<ArrowBack />}
                    sx={{ mb: 2 }}
                    onClick={() => router.visit('/inicio')}
                >
                    Procesos operativos
                </Button>
                <PageHeading
                    title="Identificación de procesos operativos"
                    description={`${displayUnitCode(props.unidad.cve_ur)} · ${props.unidad.desc_ur}`}
                    actions={
                        <Box sx={{ minWidth: 135 }}>
                            <Typography variant="caption" color="text.secondary">
                                Avance guardado
                            </Typography>
                            <Typography variant="h2" color="primary" sx={{ my: 0.5 }}>
                                {state.progress}%
                            </Typography>
                            <LinearProgress
                                variant="determinate"
                                value={state.progress}
                                sx={{ height: 5, borderRadius: '12px' }}
                            />
                        </Box>
                    }
                />
                {!state.initialized && <LinearProgress aria-label="Cargando estado de colaboración" sx={{ mb: 2 }} />}
                {(state.submitted || props.enviadoEn) && <Typography role="status" sx={{ mb: 2 }}>Enviado el {new Date(state.submitted || props.enviadoEn!).toLocaleString('es-MX')} por {state.submittedBy || props.enviadoPor || 'el responsable registrado'}. Este formato está bloqueado para edición.</Typography>}
                {!props.editable && !props.enviadoEn && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {shared.simulacion
                            ? 'Esta vista de prueba es de solo lectura.'
                            : 'Solo consulta. Puedes revisar las respuestas y el avance de esta área.'}
                    </Typography>
                )}
                {state.error && <Typography role="status" color="error.main" sx={{ mb: 2 }}>{state.error}
                    <Button size="small" onClick={() => void engine.refresh()}>Volver a comprobar</Button>
                </Typography>}
                {notice && (
                    <Alert severity="warning" sx={{ mb: 2 }} onClose={() => setNotice('')}>
                        {notice}
                    </Alert>
                )}
                <Box
                    sx={{
                        bgcolor: '#fff',
                        border: '1px solid',
                        borderColor: 'divider',
                        borderRadius: '12px',
                        overflow: 'hidden',
                    }}
                >
                    <Tabs

                        value={active}
                        onChange={(_, v) => { void engine.flush(true); setActive(v); engine.setSection(v); }}
                        variant="scrollable"
                        scrollButtons="auto"
                        allowScrollButtonsMobile
                        sx={{ borderBottom: '1px solid', borderColor: 'divider', px: 1 }}
                    >
                        {tabs.map(([key, label]) => (
                            <Tab id={`tab-${key}`} aria-controls={`panel-${key}`} value={key} key={key} label={label}
                                // Pulsar Contexto durante la carga también es una elección, aunque ya aparezca seleccionado.
                                onClick={() => engine.setSection(key)} />
                        ))}
                    </Tabs>
                    {tabs.map(([key]) => (
                            <Box
                                key={key}
                                id={`panel-${key}`}
                                role="tabpanel"
                                aria-labelledby={`tab-${key}`}
                                hidden={key !== active}
                                className="form-panel"
                                tabIndex={-1}
                                sx={{ p: { xs: 2, md: 3 } }}
                            >
                                {sections[key]()}
                                {Object.keys(state.issues).filter(block => block.startsWith(key + ':') && !splitBlocks(c)[block]).map(block =>
                                    <Box key={block} sx={{ mt: 2 }}><Typography variant="body2">Eliminación pendiente: {rowLabel(engine.proposalBase(), { section: key, id: block.slice(key.length + 1) } as RowTarget)}</Typography>{blockNotice(block)}</Box>)}
                            </Box>
                        ))}
                </Box>
                <div className="form-actions">
                    <Box sx={{ display: 'flex', gap: 1.2, alignItems: 'center' }}>
                        {state.saving || finishing ? (
                            <CloudSyncOutlined color="primary" />
                        ) : (
                            <CheckCircleOutlined color={state.error || Object.keys(state.issues).length ? 'error' : state.dirty ? 'warning' : 'success'} />
                        )}
                        <Box>
                            <Typography variant="body2" role="status" aria-live="polite" sx={{ fontWeight: 500 }}>
                                {locked
                                    ? 'Solo consulta'
                                    : state.saving || finishing
                                      ? 'Guardando…'
                                      : state.error || Object.keys(state.issues).length
                                        ? 'No se pudo guardar'
                                        : state.dirty
                                          ? 'Cambios pendientes'
                                          : state.updatedAt ? 'Guardado' : 'Sin cambios pendientes'}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                                {state.updatedAt
                                    ? `Guardado a las ${new Date(state.updatedAt).toLocaleTimeString('es-MX')}`
                                    : ''}
                            </Typography>
                        </Box>
                    </Box>
                    <div className="row-actions">
                        {/* El blur puede iniciar autoguardado antes del clic. El editor serializa ambos sin deshabilitar el botón a mitad del gesto. */}
                        <Button
                            variant="contained"
                            startIcon={<SaveOutlined />}
                            disabled={locked}
                            aria-busy={state.saving || finishing}
                            onClick={async () => {
                                setFinishing(true);
                                try { await engine.flush(true); } finally { setFinishing(false); }
                            }}
                        >
                            Guardar borrador
                        </Button>
                    </div>
                </div>
                <RemoveRowDialog open={!!removal} label={removal ? rowLabel(c, removal) : ''}
                    process={removal?.section === 'identificacion'} reason={removal ? removalReason(removal) : ''}
                    changed={!!removal && removal.snapshot !== JSON.stringify(targetRow(c, removal))}
                    onCancel={() => { modalBlocks.current = []; setRemoval(null); }} onConfirm={async () => {
                        if (!removal || removalReason(removal)) return;
                        const keys = removalBlocks(engine.state.content, removal);
                        if (!await engine.prepare(keys)) { setNotice('No se pudo reservar el registro o sus relaciones. Revisa quién está editando.'); return; }
                        // Si cambió desde la apertura, renovar la confirmación sobre la fila vigente.
                        const snapshot = JSON.stringify(targetRow(engine.state.content, removal));
                        if (snapshot !== removal.snapshot) { setRemoval({ ...removal, snapshot }); return; }
                        if (await engine.edit(data => removeRow(data, removal))) {
                            modalBlocks.current = []; setRemoval(null);
                            // Eliminar termina el bloque: confirmar y liberar también las relaciones de la cascada.
                            void engine.flush(true);
                        }
                    }} />
                <Dialog open={confirmSend !== null} onClose={() => !sending && setConfirmSend(null)} aria-labelledby="confirm-send-title">
                    <DialogTitle id="confirm-send-title">Enviar formato</DialogTitle>
                    <DialogContent>
                        <Typography sx={{ fontWeight: 600, mb: 2 }}>{displayUnitCode(props.unidad.cve_ur)} · {props.unidad.desc_ur}</Typography>
                        <Typography>Se enviarán las respuestas guardadas de esta UR y ejercicio. Después de enviar, este formato quedará bloqueado para edición.</Typography>
                        {state.error && <Alert severity="error" sx={{ mt: 2 }}>{state.error}</Alert>}
                    </DialogContent>
                    <DialogActions><Button autoFocus disabled={sending} onClick={() => setConfirmSend(null)}>Cancelar</Button>
                        <Button variant="contained" disabled={sending || !state.canSubmit} onClick={async () => {
                            setSending(true); try { if (await engine.submit(confirmSend!)) setConfirmSend(null); } finally { setSending(false); }
                        }}>{sending ? 'Enviando…' : 'Confirmar envío'}</Button></DialogActions>
                </Dialog>
                <Dialog open={!!compare} onClose={() => { if (!resolving) { modalBlocks.current = []; setCompare(null); } }} fullWidth maxWidth="md" aria-labelledby="recover-title">
                    <DialogTitle id="recover-title">Resolver cambios pendientes</DialogTitle>
                    <DialogContent>
                        <Typography sx={{ mb: 2 }}>Revisa tu propuesta y la respuesta guardada antes de confirmar. Sólo se aplicarán los cambios de este registro y sus relaciones.</Typography>
                        {compare && <FormProposalComparison proposal={c} shared={compare.live.contenido} keys={compare.keys} />}
                        {resolutionError && <Typography role="status" color="error.main" sx={{ mt: 2 }}>{resolutionError}</Typography>}
                        <Button size="small" onClick={download} startIcon={<DownloadOutlined />} sx={{ mt: 2 }}>Descargar mi propuesta</Button>
                    </DialogContent>
                    <DialogActions><Button autoFocus disabled={resolving} onClick={() => { modalBlocks.current = []; setCompare(null); }}>Cancelar</Button>
                        <Button disabled={resolving} onClick={async () => {
                            setResolving(true);
                            try {
                                await engine.discard(compare!.live.version, compare!.keys);
                                modalBlocks.current = []; setCompare(null);
                            } catch (error) {
                                setResolutionError(error instanceof Error ? error.message : 'No se pudo confirmar la respuesta guardada.');
                                await refreshComparison();
                            }
                            finally { setResolving(false); }
                        }}>Conservar respuesta guardada</Button>
                        <Button variant="contained" disabled={resolving || !compare?.live.editable} onClick={async () => {
                            setResolving(true);
                            try {
                                await engine.recover(compare!.live.version, compare!.keys);
                                if (!await engine.flush(true)) throw new Error('No se pudo confirmar el guardado. Tu propuesta se conserva.');
                                modalBlocks.current = []; setCompare(null);
                            } catch (error) {
                                setResolutionError(error instanceof Error ? error.message : 'No se pudo confirmar la propuesta.');
                                await refreshComparison();
                            } finally { setResolving(false); }
                        }}>{resolving ? 'Confirmando…' : 'Confirmar propuesta'}</Button>
                    </DialogActions>
                </Dialog>
            </div>
        </>
    );
}
