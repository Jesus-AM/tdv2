import { memo, useEffect, useRef, useState } from 'react';
import { ParticipantPhotos } from '@/Components/ParticipantAvatar';
import UsersServedSelect from '@/Components/UsersServedSelect';
import { participantColor } from '@/lib/participant-colors';
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
    Switch,
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
import BlockEditingStatus from '@/Components/BlockEditingStatus';
import { changedBlocks, splitBlocks } from '@/lib/form-blocks';
import PrioritySelect from '@/Components/PrioritySelect';
import ProcedureFieldHelp from '@/Components/ProcedureFieldHelp';
import SystemField, { type ModuleCatalog } from '@/Components/SystemField';
import IdentificationInstructions from '@/Components/IdentificationInstructions';
import RemoveRowDialog from '@/Components/RemoveRowDialog';
import FormSubmissionReview from '@/Components/FormSubmissionReview';
import RowEditingActions from '@/Components/RowEditingActions';
import RowEditingPresence from '@/Components/RowEditingPresence';
import { isScrollbarPointer, RecordEditingContext } from '@/lib/record-editing-context';
import type { FormPending, StageReview } from '@/Components/FormSubmissionReview';
import { removalBlocks, removalSnapshot, removeRow, rowLabel, targetRow } from '@/lib/form-deletion';
import type { RowTarget } from '@/lib/form-deletion';
import { displayUnitCode } from '@/lib/area-directory';
import { PendingFieldInput } from '@/lib/pending-field-input';
import { BlockEditor } from '@/lib/block-editor';
import type { EditorState, LiveForm } from '@/lib/block-editor';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { FormContent, Unit, ProcessRow, SystemRow } from '@/types/tdv2';
type Definition = {
    criterios: string[];
    medios: string[];
    preguntas: { texto: string; tipo: string; opciones?: string[] }[];
};
// El motor conserva las propuestas fuera de las pestañas. Una tecla sólo renderiza la fila afectada.
const CaptureRow = memo(function CaptureRow({ render }: { dependencies: unknown[]; render: () => React.ReactNode }) { return render(); },
    (a, b) => a.dependencies.length === b.dependencies.length && a.dependencies.every((value, i) => Object.is(value, b.dependencies[i])));
type Props = {
    entrega?: StageReview;
    unidad: Unit;
    contenido: FormContent;
    plantilla: FormContent;
    editable: boolean;
    permisoEdicion: boolean;
    version: number;
    porcentaje: number;
    porcentajeEtapa: number;
    seccionesPosteriores: boolean;
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
    ['revision', 'Revisar y enviar'],
    ['datos', 'Datos'],
    ['evaluacion', 'Evaluación'],
    ['preguntas', 'Preguntas'],
    ['acuerdos', 'Acuerdos'],
];
const validation = [
    { value: 'V', label: 'V · Vigente' },
    { value: 'A', label: 'A · Ajustar' },
];
const id = () => crypto.randomUUID();
export default function FormatoUR(props: Props) {
    const page = usePage();
    return (
        <AuthenticatedLayout>
            <ParticipantPhotos key={`${props.unidad.id_ur}:${page.props.contextoEdicion}:${page.props.simulacion?.expiresAt || ''}`}>
                <Editor {...props} />
            </ParticipantPhotos>
        </AuthenticatedLayout>
    );
}
function Editor(props: Props) {
    const { props: shared } = usePage();
    const [modules, setModules] = useState<ModuleCatalog>({ estado: 'cargando', modulos: [] });
    useEffect(() => {
        const abort = new AbortController();
        setModules({ estado: 'cargando', modulos: [] });
        void axios.get(`${props.guardarUrl}/modulos-sii`, { signal: abort.signal, timeout: 15000 }).then(({ data }) => {
            const rows = data.modulos as { id: string; descripcion: string }[];
            const counts = new Map<string, number>();
            for (const row of rows) counts.set(row.descripcion, (counts.get(row.descripcion) || 0) + 1);
            setModules({ estado: data.estado, modulos: rows.map(row => ({ ...row,
                label: counts.get(row.descripcion)! > 1 ? `${row.descripcion} · ID ${row.id}` : row.descripcion,
            })).sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base', numeric: true }) || a.id.localeCompare(b.id, 'es', { numeric: true })) });
        }).catch(() => { if (!abort.signal.aborted) setModules({ estado: 'error', modulos: [] }); });
        return () => abort.abort();
    }, [props.guardarUrl, shared.contextoEdicion]);
    const [active, setActive] = useState('contexto'),
        [showLater, setShowLater] = useState(false),
        [evalCode, setEvalCode] = useState(''),
        [notice, setNotice] = useState(''),
        [confirmSend, setConfirmSend] = useState<{ version: number; stage: number } | null>(null),
        [removal, setRemoval] = useState<(RowTarget & { snapshot: string; label: string }) | null>(null),
        [sending, setSending] = useState(false),
        [finishing, setFinishing] = useState(false);
    const modalBlocks = useRef<string[]>([]);
    const editingContext = useRef<RecordEditingContext | null>(null);
    const removing = useRef(false);
    const [removingRow, setRemovingRow] = useState(false), [removalError, setRemovalError] = useState('');
    const [failedRemovals, setFailedRemovals] = useState<Record<string, string>>({});
    const [openSelect, setOpenSelect] = useState<{ key: string; field: string } | null>(null);
    const redraw = useRef<(s: EditorState) => void>(() => {});
    const [engine] = useState<BlockEditor>(
        () => {
            const editor: BlockEditor = new BlockEditor(
                props.contenido,
                props.version,
                props.porcentaje,
                props.editable,
                {
                    read: async () => (await axios.get(`${props.guardarUrl}/estado`, { params: { tab: editor.tabId }, timeout: 15000 })).data,
                    reserve: async body => (await axios.post(`${props.guardarUrl}/reservas`, body, { timeout: 15000 })).data,
                    renew: async body => (await axios.post(`${props.guardarUrl}/reservas/actividad`, body, { timeout: 15000 })).data,
                    save: async body => (await axios.patch(`${props.guardarUrl}/bloques`, body, { timeout: 15000 })).data,
                    release: async body => (await axios.post(`${props.guardarUrl}/reservas/liberar`, body, { timeout: 15000 })).data,
                    submit: async body => (await axios.post(`${props.guardarUrl}/enviar`, body, { timeout: 15000 })).data,
                    position: async section => (await axios.post(`${props.guardarUrl}/posicion`, { section }, { timeout: 15000 })).data,
                },
                (s) => redraw.current(s),
            );
            return editor;
        },
    );
    const [firstInput] = useState(() => new PendingFieldInput(engine));
    const [state, setState] = useState({ ...engine.state });
    redraw.current = setState;
    const c = state.content,
        locked = state.locked || !state.initialized;
    const administrator = state.laterSections ?? props.seccionesPosteriores;
    const visibleTabs = administrator && showLater ? tabs : tabs.slice(0, 4);
    const showingLater = administrator && showLater;
    const delivery = state.delivery || props.entrega || null;
    const settled = state.initialized && !state.dirty && !state.saving && !finishing && !state.error && !state.conflict
        && !state.preparing.length && !Object.keys(state.issues).length && !Object.keys(state.unaccepted).length;
    const otherEditing = Object.keys(state.blocks).some(key => engine.busy(key));
    const autosave = locked ? 'Solo consulta' : state.saving || finishing ? 'Guardando…'
        : state.error || Object.keys(state.issues).length ? 'No se pudo guardar'
        : state.dirty ? 'Cambios pendientes' : state.updatedAt ? 'Guardado' : 'Sin cambios pendientes';
    const initializedSection = useRef(false);
    useEffect(() => {
        // Perder la reserva cierra el menú; una adquisición posterior no debe reabrir una intención antigua.
        if (openSelect && !engine.canEdit(openSelect.key)) setOpenSelect(null);
    }, [engine, openSelect, state]);
    useEffect(() => {
        if (state.initialized && !initializedSection.current) {
            initializedSection.current = true;
            const allowed = visibleTabs.some(([key]) => key === state.section) ? state.section : 'contexto';
            setActive(allowed); engine.setSection(allowed);
        }
    }, [state.initialized, state.section, engine, showingLater]);
    useEffect(() => {
        if (!visibleTabs.some(([key]) => key === active)) { setActive('contexto'); engine.setSection('contexto'); }
    }, [showingLater, active, engine]);
    useEffect(() => {
        const context = new RecordEditingContext(document, (key, stillOutside) => {
            void firstInput.finish(key).then(() => { if (stillOutside()) return engine.endBlock(key); });
        }, () => modalBlocks.current);
        editingContext.current = context;
        return () => { editingContext.current = null; context.dispose(); };
    }, [engine, firstInput]);
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
    function blockNotice(key: string) {
        return <BlockEditingStatus engine={engine} state={state} blockKey={key} />;
    }
    function blockEvents(key: string) {
        // Las acciones de la fila no son campos: enfocar Eliminar o volver de Cancelar no toma una reserva.
        const isField = (target: EventTarget) => target instanceof Element && !!target.closest('input, textarea, label, [role="combobox"]');
        return { 'data-edit-block': key, 'data-edit-state': engine.busy(key) ? 'occupied' : engine.canEdit(key) ? 'owned' : 'idle',
            style: { '--participant-color': participantColor(state.blocks[key]?.reserva?.color) } as React.CSSProperties,
            onFocusCapture: (event: React.FocusEvent<HTMLElement>) => {
                if (!editingContext.current?.scrolling && isField(event.target)) void engine.focus(key);
            },
            onPointerDownCapture: (event: React.PointerEvent<HTMLElement>) => {
                // Un campo que conservó el foco tras vencer o liberarse una reserva también puede retomarse.
                // Consultar el código o seleccionar texto fuera de los campos no inicia una reserva.
                // El inicio de un gesto táctil o arrastre de scrollbar tampoco pide otra reserva.
                if (event.pointerType !== 'touch' && !isScrollbarPointer(event) && !engine.canEdit(key) && isField(event.target)) void engine.focus(key);
            },
            onClickCapture: (event: React.MouseEvent<HTMLElement>) => {
                // Un toque completado permite retomar un campo que ya tenía foco y cuya reserva venció.
                if (event.nativeEvent instanceof PointerEvent && event.nativeEvent.pointerType === 'touch'
                    && !engine.canEdit(key) && isField(event.target)) void engine.focus(key);
            },
            onKeyDownCapture: (event: React.KeyboardEvent<HTMLElement>) => {
                // El buscador filtra opciones; su texto nunca es una respuesta ni un ID de módulo.
                if (!engine.canEdit(key) && !(event.target instanceof Element && event.target.closest('[data-catalog-search]'))) firstInput.key(key, event);
            },
            onPasteCapture: (event: React.ClipboardEvent<HTMLElement>) => {
                if (!(event.target instanceof Element && event.target.closest('[data-catalog-search]'))) firstInput.paste(key, event);
            },
        };
    }
    function choiceDisabled(key: string) { return locked || engine.busy(key) || state.conflicts.includes(key); }
    async function changeChoice<T extends object>(key: string, edit: (data: T) => void) {
        // Las casillas y radios reciben foco sin habilitar escrituras: confirmar primero y usar el contenido vigente.
        if (engine.canEdit(key) || await engine.focus(key)) engine.changeBlock(key, edit);
    }
    function selectEvents(key: string, field: string) {
        return { readOnly: choiceDisabled(key), open: openSelect?.key === key && openSelect.field === field && engine.canEdit(key),
            onOpen: async () => { if (await engine.focus(key)) setOpenSelect({ key, field }); },
            onClose: () => setOpenSelect(null) };
    }
    const coded = c.identificacion.filter((r) => r.codigo);
    const selectedCode = coded.some((r) => r.codigo === evalCode) ? evalCode : coded[0]?.codigo || '';
    const processOptions = coded.map((r) => ({ value: r.codigo, label: `${r.codigo} · ${r.tramite}` }));
    // El servidor confirma existencia, campos obligatorios y validación dentro de la escritura.
    const procedureOptions = state.procedures
        .map(r => ({ value: r.codigo, label: `${r.codigo} · ${r.tramite}` }));
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
    function collaborationStamp(rowId: string) {
        // La renovación de otra fila no cambia ésta. El borrado sí observa sus relaciones reales.
        return removalBlocks(c, { section: 'identificacion', id: rowId }).map(key =>
            `${key}:${state.blocks[key]?.version}:${JSON.stringify(state.blocks[key]?.reserva)}:${engine.busy(key)}:${state.issues[key] || ''}`).join('|');
    }
    function download() {
        const blob = new Blob([JSON.stringify({ ur: props.unidad, version: state.version, contenido: c, textoSinConfirmar: state.unaccepted }, null, 2)], {
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
        // El servidor asigna el código y los criterios en el mismo commit; no ofrecer referencias provisionales.
        engine.changeBlock<Record<string, string>>(`identificacion:${rowId}`, row => { row[key] = value; });
    }
    function removalReason(target: RowTarget) {
        const current = engine.state;
        if (!current.initialized) return 'Espera a que termine de cargar el formato.';
        if (current.submitted || props.enviadoEn) return 'El formato enviado está bloqueado para edición.';
        if (current.locked) return 'Tu acceso actual es de solo consulta.';
        const row = targetRow(current.content, target);
        if (!row) return 'Otra sesión ya retiró este registro.';
        const keys = removalBlocks(current.content, target), occupied = keys.find(key => engine.busy(key));
        if (occupied) {
            const lease = current.blocks[occupied]?.reserva;
            const subject = occupied === keys[0] ? 'este registro' : 'un registro relacionado';
            return lease?.otraPestana ? `Estás editando ${subject} en otra pestaña.`
                : `${lease?.titular || 'Otra persona'} está editando ${subject}.`;
        }
        // Poder confirmar la intención no concede escritura: prepare/canEdit se comprueban al ejecutar.
        if (keys.some(key => current.conflicts.includes(key))) return 'Hay cambios sin guardar en este registro. La conexión o su contenido cambió.';
        return '';
    }
    function removeButton(target: RowTarget) {
        const reason = removingRow || sending || state.saving || state.preparing.includes(`${target.section}:${target.id}`)
            ? 'Espera a que termine la operación en curso.' : removalReason(target);
        return <Tooltip title={reason || 'Eliminar registro'} describeChild><span tabIndex={reason ? 0 : undefined}
            role={reason ? 'group' : undefined} aria-label={reason ? `Eliminar registro. ${reason}` : undefined}>
            <IconButton aria-label="Eliminar registro" color="error" disabled={!!reason} sx={{ width: 44, height: 44 }}
                onClick={() => {
                    if (removalReason(target)) return;
                    const current = engine.state;
                    modalBlocks.current = removalBlocks(current.content, target); setRemovalError('');
                    setRemoval({ ...target, label: rowLabel(current.content, target), snapshot: removalSnapshot(current.content, target, current.blocks) });
                }}>
                <DeleteOutlined fontSize="small" />
            </IconButton>
        </span></Tooltip>;
    }
    async function confirmRemoval() {
        if (!removal || removing.current || removalReason(removal)) return;
        const target = removal, keys = removalBlocks(engine.state.content, target);
        const unchanged = () => removalSnapshot(engine.state.content, target, engine.state.blocks) === target.snapshot;
        if (!unchanged()) return;
        removing.current = true; setRemovingRow(true); setRemovalError('');
        try {
            // La reserva es atómica e incluye evaluación y vínculos. Abrir/Cancelar no la adquiere.
            if (changedBlocks(engine.proposalBase(), engine.state.content).some(key => keys.includes(key))) {
                if (!await engine.flush(false, keys)) { setRemovalError('Espera a confirmar los cambios del registro antes de eliminar.'); return; }
                // Un código o relación confirmados durante el guardado requieren una nueva confirmación visible.
                if (!unchanged()) { setRemovalError('Se guardaron cambios del registro. Cierra y revisa su versión vigente antes de eliminar.'); return; }
            }
            if (!await engine.prepareRemoval(keys, target)) {
                setRemovalError(removalReason(target) || 'El registro cambió mientras confirmabas. Revisa su contenido antes de eliminar.');
                return;
            }
            if (!await engine.edit(data => removeRow(data, target), () => !removalReason(target) && unchanged())) {
                setRemovalError('No se pudo eliminar. Revisa la versión vigente y las reservas del registro y sus relaciones.');
                return;
            }
            // La fila puede desaparecer de la propuesta, pero sólo el servidor confirma la eliminación.
            // Si falla, conservar la intención y su recibo; sólo se reintenta cuando sigue siendo seguro.
            let saved = await engine.flush(true, keys);
            // Confirmar un recibo pendiente anterior no basta: esperar también los bloques de esta eliminación.
            while (saved && changedBlocks(engine.proposalBase(), engine.state.content).some(key => keys.includes(key)))
                saved = await engine.flush(true, keys);
            if (!saved) setFailedRemovals(previous => ({ ...previous, [keys[0]]: target.label }));
            modalBlocks.current = []; setRemoval(null);
        } finally {
            await engine.releaseClean(keys); // Nunca libera bloques con propuestas sin confirmar.
            engine.finishRemoval(keys);
            removing.current = false; setRemovingRow(false);
        }
    }
    async function navigatePending(pending: FormPending) {
        if (!visibleTabs.some(([key]) => key === pending.seccion)) return;
        // Termina el guardado/liberación anterior antes de enfocar y reservar el destino.
        await engine.flush(true); setActive(pending.seccion); engine.setSection(pending.seccion);
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
        if (sending) return;
        setSending(true);
        try {
            if (await engine.flush(true) && await engine.refresh() && !engine.state.dirty && !engine.state.saving
                && engine.state.canSubmit && engine.state.review?.listo && !Object.keys(engine.state.blocks).some(key => engine.busy(key)))
                setConfirmSend({ version: engine.state.version, stage: engine.state.delivery?.etapa.id ?? 1 });
        } finally { setSending(false); }
    }
    function field(value: string, def: Field, label: string, onChange: (value: string) => void, blockKey?: string) {
        const readOnly = locked || !!blockKey && !engine.canEdit(blockKey);
        if (def.key === 'prioridad') return <PrioritySelect value={value} label={label} onChange={onChange} {...selectEvents(blockKey!, def.key)} />;
        const isValidation = def.key === 'validacion';
        const retiredValidation = isValidation && (value === 'D' || value === 'N');
        const validationError = blockKey ? state.validation[blockKey]?.[def.key] : undefined;
        // Conserva el valor histórico en el borrador sin agregarlo como opción ni cambiarlo al renderizar.
        const validationLabel = validation.find(option => option.value === value)?.label
            || (value === 'D' ? 'D · Duplicado o relacionado' : value === 'N' ? 'N · No corresponde' : value)
            || 'Selecciona una opción';
        return (
            <>
                <TextField
                    className={def.wide ? 'wide-field' : ''}
                    fullWidth
                    select={!!def.options}
                    multiline={!def.options && !def.type}
                    minRows={!def.options && !def.type ? 2 : undefined}
                    maxRows={!def.options && !def.type ? (blockKey?.startsWith('identificacion:') || blockKey?.startsWith('sistemas:') ? 4 : 6) : undefined}
                    type={def.type || 'text'}
                    value={isValidation && !validation.some(option => option.value === value) ? '' : value || ''}
                    error={!!validationError}
                    helperText={validationError || (retiredValidation && !locked
                        ? 'Validación pendiente de actualizar.' : undefined)}
                    onChange={(e) => onChange(e.target.value)}
                    slotProps={{
                        input: { readOnly },
                        select: { ...(blockKey ? selectEvents(blockKey, def.key) : { readOnly }),
                            ...(isValidation ? { displayEmpty: true, renderValue: () => validationLabel } : {}),
                            SelectDisplayProps: { 'aria-label': label, ...{ 'data-field': def.key } } },
                        htmlInput: {
                            'aria-label': label,
                            'data-field': def.key,
                            min: def.type === 'number' ? 1 : undefined,
                            max: def.type === 'number' ? 9999 : undefined,
                        },
                    }}
                >
                    {def.options && [
                        !isValidation && <MenuItem key="empty" value="">
                            Sin seleccionar
                        </MenuItem>,
                        ...def.options.map((o) => (
                            <MenuItem key={o.value} value={o.value}>
                                {o.label}
                            </MenuItem>
                        )),
                        ...(!isValidation && value && !def.options.some(o => o.value === value) ? [<MenuItem key="historical" value={value} disabled>{value} · Vínculo histórico</MenuItem>] : []),
                    ]}
                </TextField>
            </>
        );
    }
    function table(section: 'sistemas' | 'datos' | 'acuerdos', fields: Field[]) {
        const rows = c[section];
        return (
            <>
                <div className={section === 'sistemas' ? 'capture-scroll capture-row-scroll' : 'capture-scroll'} data-edit-scroll>
                    <Table size="small" className={section === 'sistemas' ? 'systems-table' : undefined} aria-label={tabs.find((t) => t[0] === section)?.[1]}>
                        <TableHead>
                            <TableRow>
                                {section === 'sistemas' && <TableCell className="row-editing-cell">Edición</TableCell>}
                                {fields.map((f) => (
                                    <TableCell key={f.key}>{f.label}{section === 'sistemas' && <ProcedureFieldHelp field={f.key} />}</TableCell>
                                ))}
                                <TableCell>Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {rows.map((r, i) => (
                                <CaptureRow key={r.id} dependencies={[r, i, locked, state.initialized, state.saving, removingRow, sending, state.preparing.includes(`${section}:${r.id}`), state.issues[`${section}:${r.id}`], state.validation[`${section}:${r.id}`], modules, engine.canEdit(`${section}:${r.id}`), engine.busy(`${section}:${r.id}`), JSON.stringify(state.blocks[`${section}:${r.id}`]?.reserva), openSelect?.key === `${section}:${r.id}` ? openSelect.field : null, c.identificacion, state.procedures]} render={() => <TableRow {...blockEvents(`${section}:${r.id}`)}>
                                    {section === 'sistemas' && <RowEditingPresence engine={engine} state={state} blockKey={`${section}:${r.id}`} />}
                                    {fields.map((f) => (
                                        <TableCell key={f.key}>
                                            {section === 'sistemas' && (f.key === 'sistema' || f.key === 'uso' || f.key === 'estado')
                                                ? <SystemField row={r as SystemRow} field={f.key} index={i} catalog={modules}
                                                    errors={state.validation[`sistemas:${r.id}`]} readOnly={locked || !engine.canEdit(`sistemas:${r.id}`)}
                                                    events={name => selectEvents(`sistemas:${r.id}`, name)}
                                                    change={(name, value) => engine.changeBlock<Record<string, string>>(`sistemas:${r.id}`, row => {
                                                        row[name] = value;
                                                        if (name === 'moduloSiiId') row.moduloSiiDescripcion = modules.modulos.find(m => m.id === value)?.descripcion || '';
                                                    })} />
                                                : field(
                                                String((r as unknown as Record<string, string>)[f.key] || ''),
                                                f,
                                                `${f.label} ${i + 1}`,
                                                (value) =>
                                                    engine.changeBlock<Record<string, string>>(`${section}:${r.id}`, row => { row[f.key] = value; }),
                                                `${section}:${r.id}`,
                                            )}
                                            {section === 'sistemas' && f.key === 'proceso' && (r as SystemRow).proceso
                                                && !state.procedures.some(p => p.codigo === (r as SystemRow).proceso) && <Box sx={{ mt: .5 }}>
                                                    <Typography variant="caption">El procedimiento relacionado está pendiente de completar o validar.</Typography>
                                                    <Button size="small" onClick={() => navigatePending({ seccion: 'identificacion', bloque: 'identificacion:' + (c.identificacion.find(p => p.codigo === (r as SystemRow).proceso)?.id || ''), campo: 'tramite', mensaje: '' })}>Revisar Identificación general</Button>
                                                </Box>}
                                        </TableCell>
                                    ))}
                                    <TableCell className={section === 'sistemas' ? 'row-actions-cell' : undefined}>
                                        {section === 'sistemas' ? removeButton({ section, id: r.id }) :
                                        <RowEditingActions engine={engine} state={state} blockKey={`${section}:${r.id}`}>
                                            {removeButton({ section, id: r.id })}
                                        </RowEditingActions>}
                                    </TableCell>
                                </TableRow>} />
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
        { key: 'tramite', label: 'Trámite o servicio', wide: true },
        { key: 'usuario', label: '¿A quién atiende?' },
        { key: 'resultado', label: '¿Qué entrega?', wide: true },
        { key: 'responsable', label: 'Área responsable' },
        { key: 'validacion', label: 'Validación', options: validation },
        { key: 'prioridad', label: 'Prioridad' },
    ];
    const sections: Record<string, () => React.ReactNode> = {
        contexto: () => (
            <div className="stack">
                <Typography variant="h2">Primera etapa</Typography>
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
                        'Completa Trámite o servicio, ¿A quién atiende?, ¿Qué entrega? y Área responsable.',
                        'Asigna validación y prioridad a cada procedimiento.',
                        'Relaciona los procedimientos validados con sus sistemas y herramientas e indica los medios utilizados.',
                    ].map((t) => (
                        <li key={t}>{t}</li>
                    ))}
                </Box>
                <Typography variant="body2" color="text.secondary">Los cambios se guardan como borrador. Completar esta etapa no envía ni bloquea el formato completo.</Typography>
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
                <IdentificationInstructions />
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
                <div className="capture-scroll capture-row-scroll" data-edit-scroll>
                    <Table size="small" className="identification-table" aria-label="Identificación general">
                        <TableHead>
                            <TableRow>
                                <TableCell className="row-editing-cell">Edición</TableCell>
                                <TableCell>Código<ProcedureFieldHelp field="codigo" /></TableCell>
                                {processFields.map((f) => (
                                    <TableCell key={f.key}>{f.label}<ProcedureFieldHelp field={f.key} /></TableCell>
                                ))}
                                <TableCell>Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {c.identificacion.map((r, i) => (
                                <CaptureRow key={r.id} dependencies={[r, i, locked, state.initialized, state.saving, removingRow, sending, state.preparing.includes(`identificacion:${r.id}`), state.issues[`identificacion:${r.id}`], state.validation[`identificacion:${r.id}`], engine.canEdit(`identificacion:${r.id}`), collaborationStamp(r.id), openSelect?.key === `identificacion:${r.id}` ? openSelect.field : null, c.sistemas, c.datos, c.evaluaciones]} render={() => <TableRow {...blockEvents(`identificacion:${r.id}`)}>
                                    <RowEditingPresence engine={engine} state={state} blockKey={`identificacion:${r.id}`} />
                                    <TableCell>
                                        <Typography
                                            variant="body2"
                                            sx={{ fontWeight: 400, mb: 1 }}
                                        >
                                            {r.codigo || 'Por validar'}
                                        </Typography>
                                        <Chip
                                            label={r.fuente || (r.id.startsWith('ilda:') ? 'ILDA' : 'Nuevo')}
                                            color={r.fuente === 'ILDA' ? 'primary' : 'default'}
                                            variant="outlined"
                                        />
                                    </TableCell>
                                    {processFields.map((f) => (
                                        <TableCell key={f.key}>
                                            {f.key === 'usuario' ? <><UsersServedSelect value={r.usuario} label={`${f.label} ${i + 1}`}
                                                {...selectEvents(`identificacion:${r.id}`, 'usuario')}
                                                onChange={values => {
                                                    // Compartir la reserva de la fila; el portal no concede permiso de escritura.
                                                    if (engine.canEdit(`identificacion:${r.id}`)) {
                                                        setNotice('');
                                                        engine.changeBlock<ProcessRow>(`identificacion:${r.id}`, row => { row.usuario = values; });
                                                    }
                                                }} />
                                                {locked && r.usuarioOtro && <Typography sx={{ mt: 1, whiteSpace: 'pre-wrap' }}>Otro: {r.usuarioOtro}</Typography>}
                                            </> : field(r[f.key as Exclude<keyof ProcessRow, 'usuario'>] || '', f, `${f.label} ${i + 1}`, (value) => {
                                                setNotice('');
                                                processEdit(r.id, f.key, value);
                                            }, `identificacion:${r.id}`)}
                                        </TableCell>
                                    ))}
                                    <TableCell className="row-actions-cell">
                                        {removeButton({ section: 'identificacion', id: r.id })}
                                    </TableCell>
                                </TableRow>} />
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
                </Box>
            </>
        ),
        sistemas: () => (
            <>
                <Typography variant="h2" sx={{ mb: 1 }}>
                    Sistemas y herramientas
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 700, mb: 1 }}>¿Qué sistemas, programas o herramientas utiliza tu área?</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    Para cada procedimiento institucional registrado en el paso 2, indica qué sistemas, programas o herramientas utiliza tu área para realizarlo y cómo funcionan.
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    Registra una herramienta por fila. Puedes agregar varias filas para el mismo procedimiento, cada una con su propio uso y funcionamiento.
                </Typography>
                {procedureOptions.length === 0 && <Typography variant="body2" sx={{ mb: 2 }}>Completa y valida los procedimientos en Identificación general para seleccionarlos aquí</Typography>}
                {table('sistemas', [
                    { key: 'proceso', label: 'Procedimiento', options: procedureOptions, wide: true },
                    { key: 'sistema', label: 'Sistema o herramienta' },
                    { key: 'uso', label: '¿Para qué se usa?' },
                    { key: 'estado', label: '¿Cómo funciona?' },
                    { key: 'fallas', label: 'Fallas o comentarios' },
                ])}
                <Box component="section" aria-labelledby="media-title" sx={{ mt: 3, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
                <Typography id="media-title" variant="h3" sx={{ mb: 1 }}>
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
                                        void changeChoice<Pick<FormContent, 'medios' | 'medioOtro'>>('medios', d => { d.medios[m] = checked; }); }}
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
                        engine.changeBlock<Pick<FormContent, 'medios' | 'medioOtro'>>('medios', (d) => {
                            d.medioOtro = e.target.value;
                        })
                    }
                    slotProps={{ input: { readOnly: !engine.canEdit('medios') }, htmlInput: { 'data-field': 'medioOtro' } }}
                    sx={{ mt: 1, minWidth: 250 }}
                />
                </Box>
                </Box>
            </>
        ),
        revision: () => <FormSubmissionReview delivery={delivery} area={`${displayUnitCode(props.unidad.cve_ur)} · ${props.unidad.desc_ur}`}
            canSubmit={state.canSubmit} settled={settled} busy={otherEditing} autosave={autosave}
            submitting={sending} onNavigate={navigatePending} onSubmit={() => void reviewForSubmission()} />,
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
                                                void changeChoice<FormContent['evaluaciones'][string][number]>(`evaluaciones:${code}:${i}`, (d) => {
                                                    d.valor = value;
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
                                            slotProps={{ input: { readOnly: !engine.canEdit(`evaluaciones:${code}:${i}`) }, htmlInput: { 'data-field': 'obs' } }}
                                            onChange={(e) =>
                                                engine.changeBlock<FormContent['evaluaciones'][string][number]>(`evaluaciones:${code}:${i}`, (d) => {
                                                    d.obs = e.target.value;
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
                        <CaptureRow key={q.pregunta} dependencies={[q, i, locked, state.initialized, state.preparing.includes(`preguntas:${i}`), state.issues[`preguntas:${i}`], engine.canEdit(`preguntas:${i}`), engine.busy(`preguntas:${i}`), JSON.stringify(state.blocks[`preguntas:${i}`]?.reserva), openSelect?.key === `preguntas:${i}` ? openSelect.field : null]} render={() => <Box component="fieldset" {...blockEvents(`preguntas:${i}`)}
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
                                                void changeChoice<FormContent['preguntas'][number]>(`preguntas:${i}`, d => { d.marcada = checked; }); }}
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
                                slotProps={{ input: { readOnly: !engine.canEdit(`preguntas:${i}`) }, select: selectEvents(`preguntas:${i}`, 'respuesta'), htmlInput: { 'data-field': 'respuesta' } }}
                                onChange={(e) =>
                                    engine.changeBlock<FormContent['preguntas'][number]>(`preguntas:${i}`, (d) => {
                                        d.respuesta = e.target.value;
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
                        </Box>} />
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

            </>
        ),
    };
    return (
        <>
            <Head title={`Procedimientos Institucionales · ${displayUnitCode(props.unidad.cve_ur)}`} />
            <div className="page">
                <Button

                    startIcon={<ArrowBack />}
                    sx={{ mb: 2 }}
                    onClick={() => router.visit('/inicio')}
                >
                    Procedimientos Institucionales
                </Button>
                <PageHeading
                    title="Procedimientos Institucionales"
                    description={`${displayUnitCode(props.unidad.cve_ur)} · ${props.unidad.desc_ur}`}
                    actions={
                        <Box sx={{ minWidth: 135 }}>
                            <Typography variant="caption" color="text.secondary">
                                {state.submitted || props.enviadoEn ? 'Avance guardado' : `Avance de la ${delivery?.etapa.nombre.toLocaleLowerCase('es-MX') || 'primera etapa'}`}
                            </Typography>
                            <Typography variant="h2" color="primary" sx={{ my: 0.5 }}>
                                {state.submitted || props.enviadoEn ? state.progress : state.initialized ? state.stageProgress : props.porcentajeEtapa}%
                            </Typography>
                            <LinearProgress
                                variant="determinate"
                                value={state.submitted || props.enviadoEn ? state.progress : state.initialized ? state.stageProgress : props.porcentajeEtapa}
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
                {administrator && <FormControlLabel sx={{ mb: 1 }}
                    control={<Switch size="small" checked={showLater} onChange={(_, value) => { void engine.flush(true); setShowLater(value); }} />}
                    label={<Typography variant="body2">Mostrar secciones posteriores</Typography>} />}
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
                        {visibleTabs.map(([key, label]) => (
                            <Tab id={`tab-${key}`} aria-controls={`panel-${key}`} value={key} key={key} label={label}
                                // Pulsar Contexto durante la carga también es una elección, aunque ya aparezca seleccionado.
                                onClick={() => engine.setSection(key)} />
                        ))}
                    </Tabs>
                    {visibleTabs.filter(([key]) => key === active).map(([key]) => (
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
                {state.dirty && Object.keys(state.issues).length > 0 && <Button size="small" onClick={download} startIcon={<DownloadOutlined />}>Descargar cambios sin guardar</Button>}
                {Object.entries(failedRemovals).filter(([key]) => state.issues[key]).map(([key, label]) => <Box key={key} role="status" sx={{ mt: 2 }}>
                    <Typography variant="body2" color="error.main">No se confirmó la eliminación de «{label}». Tu propuesta se conserva.</Typography>
                </Box>)}
                <RemoveRowDialog open={!!removal} label={removal?.label || ''}
                    process={removal?.section === 'identificacion'} reason={!removingRow && removal ? removalReason(removal) : ''}
                    changed={!removingRow && !!removal && removal.snapshot !== removalSnapshot(c, removal, state.blocks)}
                    busy={removingRow} error={removalError}
                    onCancel={() => {
                        if (removing.current) return;
                        const keys = modalBlocks.current; modalBlocks.current = []; setRemoval(null); setRemovalError('');
                        for (const key of keys) void engine.endBlock(key);
                    }} onConfirm={() => void confirmRemoval()} />
                <Dialog open={confirmSend !== null} onClose={() => !sending && setConfirmSend(null)} aria-labelledby="confirm-send-title">
                    <DialogTitle id="confirm-send-title">Enviar {delivery?.etapa.nombre.toLocaleLowerCase('es-MX') || 'primera etapa'}</DialogTitle>
                    <DialogContent>
                        <Typography sx={{ fontWeight: 600, mb: 2 }}>{displayUnitCode(props.unidad.cve_ur)} · {props.unidad.desc_ur}</Typography>
                        <Typography>Al enviar la {delivery?.etapa.nombre.toLocaleLowerCase('es-MX') || 'primera etapa'}, sus respuestas quedarán disponibles para consulta y ya no podrán editarse.</Typography>
                        {confirmSend && (confirmSend.version !== state.version || confirmSend.stage !== delivery?.etapa.id) && <Typography role="status" sx={{ mt: 2 }}>Las respuestas o la etapa cambiaron. Cierra esta confirmación y revisa el estado actualizado antes de enviar.</Typography>}
                        {otherEditing && <Typography role="status" sx={{ mt: 2 }}>Otra sesión está editando. Espera a que termine antes de enviar.</Typography>}
                        {state.error && <Alert severity="error" sx={{ mt: 2 }}>{state.error}</Alert>}
                    </DialogContent>
                    <DialogActions><Button autoFocus disabled={sending} onClick={() => setConfirmSend(null)}>Cancelar</Button>
                        <Button variant="contained" disabled={sending || !state.canSubmit || !settled || otherEditing || !delivery?.revision.listo || confirmSend?.version !== state.version || confirmSend?.stage !== delivery?.etapa.id} onClick={async () => {
                            setSending(true); try { if (await engine.submit(confirmSend!.version)) setConfirmSend(null); } finally { setSending(false); }
                        }}>{sending ? 'Enviando…' : 'Confirmar envío'}</Button></DialogActions>
                </Dialog>

            </div>
        </>
    );
}
