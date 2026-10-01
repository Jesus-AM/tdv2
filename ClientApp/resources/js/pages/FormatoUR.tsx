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
} from '@mui/material';
import {
    Add,
    ArrowBack,
    DeleteOutlined,
    DownloadOutlined,
    PrintOutlined,
    SaveOutlined,
    CheckCircleOutlined,
    CloudSyncOutlined,
} from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import { FormEditor } from '@/lib/form-editor';
import type { EditorState } from '@/lib/form-editor';
import type { FormContent, Unit, SaveResponse, ProcessRow } from '@/types/tdv2';
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
    ['encabezado', 'Encabezado'],
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
    const [active, setActive] = useState('identificacion'),
        [printing, setPrinting] = useState(false),
        [evalCode, setEvalCode] = useState(''),
        [notice, setNotice] = useState('');
    const redraw = useRef<(s: EditorState) => void>(() => {});
    const [engine] = useState(
        () =>
            new FormEditor(
                props.contenido,
                props.version,
                props.porcentaje,
                props.editable,
                async (body) =>
                    (await axios.put<SaveResponse>(props.guardarUrl, body, { headers: { Accept: 'application/json' } }))
                        .data,
                (s) => redraw.current(s),
            ),
    );
    const [state, setState] = useState({ ...engine.state });
    redraw.current = setState;
    const c = state.content,
        locked = state.locked;
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
        const afterPrint = () => setPrinting(false);
        window.addEventListener('afterprint', afterPrint);
        return () => {
            engine.dispose();
            remove();
            window.removeEventListener('beforeunload', before);
            window.removeEventListener('afterprint', afterPrint);
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
    function reload() {
        if (
            (state.dirty || state.saving) &&
            !window.confirm('¿Ya conservaste tus cambios? Se cargará la última versión guardada.')
        )
            return;
        engine.state.dirty = false;
        window.location.reload();
    }
    function print() {
        setPrinting(true);
        requestAnimationFrame(() => requestAnimationFrame(() => window.print()));
    }
    function processEdit(index: number, key: string, value: string) {
        change((d) => {
            const row = d.identificacion[index];
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
    function removeProcess(index: number) {
        const row = c.identificacion[index];
        if (row.id.startsWith('ilda:')) return;
        if (
            row.codigo &&
            !window.confirm('¿Retirar este proceso? Se quitará su evaluación y se desvinculará de sistemas y datos.')
        )
            return;
        change((d) => {
            d.identificacion.splice(index, 1);
            if (row.codigo) {
                delete d.evaluaciones[row.codigo];
                d.sistemas.forEach((r) => {
                    if (r.proceso === row.codigo) r.proceso = '';
                });
                d.datos.forEach((r) => {
                    if (r.proceso === row.codigo) r.proceso = '';
                });
            }
        });
    }
    function field(value: string, def: Field, label: string, onChange: (value: string) => void) {
        return (
            <>
                <span className="print-only">{def.options?.find((o) => o.value === value)?.label || value || '—'}</span>
                <TextField
                    className={`screen-only ${def.wide ? 'wide-field' : ''}`}
                    fullWidth
                    select={!!def.options}
                    multiline={!def.options && !def.type}
                    minRows={!def.options && !def.type ? 2 : undefined}
                    maxRows={!def.options && !def.type ? 6 : undefined}
                    type={def.type || 'text'}
                    value={value || ''}
                    onChange={(e) => onChange(e.target.value)}
                    slotProps={{
                        input: { readOnly: locked },
                        select: { readOnly: locked, SelectDisplayProps: { 'aria-label': label } },
                        htmlInput: {
                            'aria-label': label,
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
                                <TableCell className="no-print">Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {rows.map((r, i) => (
                                <TableRow key={r.id}>
                                    {fields.map((f) => (
                                        <TableCell key={f.key}>
                                            {field(
                                                String((r as unknown as Record<string, string>)[f.key] || ''),
                                                f,
                                                `${f.label} ${i + 1}`,
                                                (value) =>
                                                    change((d) => Object.assign(d[section][i], { [f.key]: value })),
                                            )}
                                        </TableCell>
                                    ))}
                                    <TableCell className="no-print">
                                        <IconButton
                                            aria-label={`Retirar fila ${i + 1} de ${section}`}
                                            disabled={locked}
                                            onClick={() =>
                                                change((d) => {
                                                    d[section].splice(i, 1);
                                                })
                                            }
                                        >
                                            <DeleteOutlined fontSize="small" />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
                <Button
                    className="no-print"
                    sx={{ mt: 2 }}
                    startIcon={<Add />}
                    disabled={locked || rows.length >= 200}
                    onClick={() =>
                        change((d) => {
                            const blank = props.plantilla[section][0];
                            const row = { ...blank, id: id() };
                            if (section === 'sistemas') d.sistemas.push(row as FormContent['sistemas'][0]);
                            else if (section === 'datos') d.datos.push(row as FormContent['datos'][0]);
                            else d.acuerdos.push(row as FormContent['acuerdos'][0]);
                        })
                    }
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
        { key: 'prioridad', label: 'Prioridad', type: 'number' },
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
        encabezado: () => (
            <>
                <Typography variant="h2" sx={{ mb: 2 }}>
                    Datos de la sesión
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 2fr 2fr' }, gap: 2 }}>
                    <TextField
                        label="Fecha de sesión"
                        type="date"
                        value={c.encabezado.fecha}
                        onChange={(e) =>
                            change((d) => {
                                d.encabezado.fecha = e.target.value;
                            })
                        }
                        slotProps={{ input: { readOnly: locked }, inputLabel: { shrink: true } }}
                    />
                    <TextField
                        label="Área participante"
                        value={c.encabezado.area}
                        slotProps={{ input: { readOnly: true } }}
                    />
                    <TextField
                        label="Responsable del llenado"
                        value={c.encabezado.responsable}
                        onChange={(e) =>
                            change((d) => {
                                d.encabezado.responsable = e.target.value;
                            })
                        }
                        slotProps={{ input: { readOnly: locked } }}
                    />
                </Box>
            </>
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
                                <TableCell className="no-print">Acciones</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {c.identificacion.map((r, i) => (
                                <TableRow key={r.id}>
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
                                    </TableCell>
                                    {processFields.map((f) => (
                                        <TableCell key={f.key}>
                                            {field(r[f.key as keyof ProcessRow], f, `${f.label} ${i + 1}`, (value) => {
                                                if (
                                                    f.key === 'prioridad' &&
                                                    value &&
                                                    c.identificacion.some((o, j) => j !== i && o.prioridad === value)
                                                ) {
                                                    setNotice(`La prioridad ${value} ya está asignada a otro proceso.`);
                                                    return;
                                                }
                                                setNotice('');
                                                processEdit(i, f.key, value);
                                            })}
                                        </TableCell>
                                    ))}
                                    <TableCell className="no-print">
                                        {r.id.startsWith('ilda:') ? (
                                            <Tooltip title="Si este registro no corresponde, selecciona N en Validación.">
                                                <Typography variant="caption" color="text.secondary">
                                                    Origen ILDA
                                                </Typography>
                                            </Tooltip>
                                        ) : (
                                            <IconButton
                                                aria-label={`Retirar proceso ${i + 1}`}
                                                disabled={locked}
                                                onClick={() => removeProcess(i)}
                                            >
                                                <DeleteOutlined fontSize="small" />
                                            </IconButton>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
                <Box className="row-actions no-print" sx={{ mt: 2, justifyContent: 'space-between' }}>
                    <Button
                        startIcon={<Add />}
                        disabled={locked || c.identificacion.length >= 200}
                        onClick={() =>
                            change((d) => {
                                d.identificacion.push({ ...props.plantilla.identificacion[0], id: id() });
                            })
                        }
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
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    {Object.keys(c.medios).map((m) => (
                        <FormControlLabel
                            key={m}
                            control={
                                <Checkbox
                                    checked={c.medios[m]}
                                    disabled={locked}
                                    onChange={(e) =>
                                        change((d) => {
                                            d.medios[m] = e.target.checked;
                                        })
                                    }
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
                    slotProps={{ input: { readOnly: locked } }}
                    sx={{ mt: 1, minWidth: 250 }}
                />
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
                            className="no-print"
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
                        {(printing ? coded.map((r) => r.codigo) : [selectedCode]).map((code) => (
                            <Box key={code}>
                                <Typography variant="h3" sx={{ mb: 1 }}>
                                    {processOptions.find((o) => o.value === code)?.label}
                                </Typography>
                                {props.definicion.criterios.map((criterion, i) => (
                                    <Box
                                        key={criterion}
                                        sx={{
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
                                        <Typography variant="body2">{criterion}</Typography>
                                        <RadioGroup
                                            row
                                            aria-label={`${criterion} de ${code}`}
                                            value={c.evaluaciones[code]?.[i]?.valor || ''}
                                            onChange={(e) =>
                                                change((d) => {
                                                    if (!d.evaluaciones[code])
                                                        d.evaluaciones[code] = props.definicion.criterios.map(
                                                            (criterio) => ({ criterio, valor: '', obs: '' }),
                                                        );
                                                    d.evaluaciones[code][i].valor = e.target.value;
                                                })
                                            }
                                        >
                                            {['1', '2', '3', '4', '5'].map((v) => (
                                                <FormControlLabel
                                                    key={v}
                                                    value={v}
                                                    disabled={locked}
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
                                            slotProps={{ input: { readOnly: locked } }}
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
                        <Box key={q.pregunta} sx={{ pb: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
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
                                            disabled={locked}
                                            onChange={(e) =>
                                                change((d) => {
                                                    d.preguntas[i].marcada = e.target.checked;
                                                })
                                            }
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
                                slotProps={{ input: { readOnly: locked } }}
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
            </>
        ),
    };
    return (
        <>
            <Head title={`Procesos operativos · ${props.unidad.cve_ur}`} />
            <div className="page">
                <Button
                    className="no-print"
                    startIcon={<ArrowBack />}
                    sx={{ mb: 2 }}
                    onClick={() => router.visit('/inicio')}
                >
                    Procesos operativos
                </Button>
                <PageHeading
                    title="Identificación de procesos operativos"
                    description={`${props.unidad.cve_ur} · ${props.unidad.desc_ur}`}
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
                {!props.editable && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        {shared.simulacion
                            ? 'Esta vista de prueba es de solo lectura.'
                            : 'Puedes consultar las respuestas y el avance de esta área.'}
                    </Typography>
                )}
                {state.error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {state.error}
                        <Box className="row-actions" sx={{ mt: 1 }}>
                            <Button onClick={download} startIcon={<DownloadOutlined />}>
                                Descargar mis cambios
                            </Button>
                            {state.conflict && <Button onClick={reload}>Cargar versión compartida</Button>}
                            {!state.locked && <Button onClick={() => void engine.flush()}>Reintentar guardado</Button>}
                        </Box>
                    </Alert>
                )}
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
                        className="no-print"
                        value={active}
                        onChange={(_, v) => setActive(v)}
                        variant="scrollable"
                        scrollButtons="auto"
                        allowScrollButtonsMobile
                        sx={{ borderBottom: '1px solid', borderColor: 'divider', px: 1 }}
                    >
                        {tabs.map(([key, label]) => (
                            <Tab id={`tab-${key}`} aria-controls={`panel-${key}`} value={key} key={key} label={label} />
                        ))}
                    </Tabs>
                    {tabs
                        .filter(([key]) => printing || key === active)
                        .map(([key]) => (
                            <Box
                                key={key}
                                id={`panel-${key}`}
                                role="tabpanel"
                                aria-labelledby={`tab-${key}`}
                                sx={{ p: { xs: 2, md: 3 }, pageBreakBefore: printing ? 'auto' : undefined }}
                            >
                                {sections[key]()}
                            </Box>
                        ))}
                </Box>
                <div className="form-actions no-print">
                    <Box sx={{ display: 'flex', gap: 1.2, alignItems: 'center' }}>
                        {state.saving ? (
                            <CloudSyncOutlined color="primary" />
                        ) : (
                            <CheckCircleOutlined color={state.error ? 'error' : state.dirty ? 'warning' : 'success'} />
                        )}
                        <Box>
                            <Typography variant="body2" sx={{ fontWeight: 500 }}>
                                {!props.editable
                                    ? 'Solo consulta'
                                    : state.saving
                                      ? 'Guardando…'
                                      : state.error
                                        ? 'Cambios sin guardar'
                                        : state.dirty
                                          ? 'Cambios pendientes'
                                          : 'Sin cambios pendientes'}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                                {state.updatedAt
                                    ? `Guardado a las ${new Date(state.updatedAt).toLocaleTimeString('es-MX')}`
                                    : 'Tus respuestas se guardan automáticamente al editar.'}
                            </Typography>
                        </Box>
                    </Box>
                    <div className="row-actions">
                        <Button variant="outlined" startIcon={<PrintOutlined />} onClick={print}>
                            Imprimir
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={<SaveOutlined />}
                            disabled={locked || state.saving}
                            onClick={() => void engine.flush(true)}
                        >
                            Guardar
                        </Button>
                    </div>
                </div>
            </div>
        </>
    );
}
