# Distribución compacta de Procesos operativos — 2026-10-05

## Implementado

Se conservan la cabecera principal, el logo de 180/150 px, la separación de 32 px al menú, la navegación y las acciones de usuario. El título, descripción, Colaboradores y Actualizar mantienen su presentación y funcionamiento.

- Indicadores de 80 px en escritorio, con altura mínima y crecimiento libre; números de 26 px, contenedores de iconos de 34 px y separación de 14 px. En móvil se conservan los tres indicadores, se omiten sólo los iconos decorativos y las etiquetas completas se distribuyen en dos líneas, sin reducir su letra. Separación de 12 px.
- Menos margen tras indicadores y alcance; pestañas de 40 px. La información de alcance sigue siendo texto secundario. Título del listado, conteo único y Expandir visibles/Contraer se reúnen en una cabecera. Al filtrar se indica cuántas áreas principales coinciden respecto al total, sin repetir el conteo en otra fila.
- Buscador y filtro en una fila, apilados en móvil, con sus etiquetas y controles MUI. Se retira el contenedor con relleno adicional que los rodeaba.
- Filas principales normales de 80 px, separadas 8 px. Nombre de área primero; clave y cantidades secundarias. Porcentaje y barra alineados a la derecha. Las filas crecen con nombres largos y no usan elipsis. Filas de formato con mínimo 76 px; controles se acomodan en móvil.
- Se conserva el acceso al formato propio al expandir un área sin dependientes. Hover suave y foco visible dentro del borde; descripción accesible con cantidades y avance. No se añaden sombras ni movimientos de hover.

Archivos de aplicación: `Inicio.tsx`, `AreaDirectory.tsx` y `app.css`. No se cambiaron cálculos, filtros de elegibilidad, estado de pestaña/expansión, permisos, captura, reservas, autoguardado, SignalR, autenticación ni servicios del backend. No hay dependencias ni migraciones nuevas.

## Verificado

Se sirvió el bundle compilado en un servidor temporal de loopback, con props sintéticas y solicitudes externas bloqueadas. No se arrancó ASP.NET para las capturas ni se conectó a bases o servicios de identidad. Catálogo sintético: diez áreas principales, una dependencia, un nodo auxiliar tipo `N` y un área de nivel 3 sin agrupador. Los datos permiten verificar paginación, nombres largos, consulta y edición autorizada por las props.

Posición superior de la primera área, medida desde el inicio de la ventana; filas principales completas visibles con los grupos contraídos:

| Ventana | Primera área antes → después | Filas completas antes → después |
|---|---|---|
| 1920×1080 | 600 → 459 px | 4 → 7 |
| 1366×768 | 600 → 459 px | 1 → 3 |
| 768×1024 | 573 → 453 px | 3 → 6 |
| 390×844 | 775 → 654 px | 0 → 2 |
| 320×740 | 824 → 675 px | 0 → 0; comienza a verse el primer grupo |

Sin desbordamientos horizontales en las cinco medidas, etiquetas completas y tipografía original. En 320 px las filas crecen para conservar nombre, distintivo, cantidades y progreso; no se fuerza una altura que oculte información.

TypeScript y Vite correctos. `tdv2.slnx` compila en Release sin errores ni advertencias. **55/55** pruebas frontend aprobadas. Recorrido visual/funcional aprobado: cálculos, conteo único, exclusión N e inclusión 0, búsqueda 06000/6000, vacíos, filtros, Mis áreas/Todas las áreas, expansión/contracción, paginación, solicitud de apertura del formato sin dependientes, estado de carga de Actualizar, Colaboradores según props, vista personal, foco de teclado y movimiento reducido. Cero errores JavaScript.

La prueba de apertura verifica el destino solicitado; no pretende validar el contenido de la pantalla de captura. Los primeros intentos del verificador encontraron selectores ambiguos durante el cierre del Select y una espera incompleta antes de medir la recarga; se corrigieron las esperas del verificador. El recorrido final terminó con código 0.

Capturas revisadas:

- [Antes, 1366×768](distribucion-procesos-antes.png) y [después, 1366×768](distribucion-procesos-1366.png).
- [Escritorio, 1920×1080](distribucion-procesos-1920.png).
- [Tableta, 768×1024](distribucion-procesos-768.png).
- [Móvil, 390×844](distribucion-procesos-390.png) y [320×740](distribucion-procesos-320.png).
- [Jerarquía expandida y nombre largo](distribucion-procesos-expandido.png), [vista personal](distribucion-procesos-personal.png).

[Medidas previas](evidencia-distribucion-procesos-antes.json) y [medidas y comprobaciones finales](evidencia-distribucion-procesos.json).

## Reproducir desde la raíz

Con Node/npm y .NET 10 en PATH; el recorrido visual requiere Edge instalado:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
node ClientApp/tests/browser/operational-layout.mjs
```

El verificador escribe sus capturas y mediciones en `.artifacts/operational-layout/after` y cierra el navegador/servidor temporal al terminar. Su opción `before` sólo captura una referencia del bundle que se encuentre compilado en ese momento; no recupera una versión histórica.

En Visual Studio, abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React** y pulsar F5. En Procesos operativos comprobar tamaños, pestañas, búsqueda, filtro y expansión al redimensionar. La captura automática anterior permite hacer la revisión con datos sintéticos sin acceder a una cuenta institucional.

## Pendiente y límites

La aceptación con datos y cuentas institucionales corresponde al operador. Esta revisión visual verifica la interfaz y las props de permisos; no se presenta como una nueva prueba de autorización del servidor o de concurrencia PostgreSQL. Se conservan las evidencias de esas reglas en las entregas anteriores. No se modificaron bases institucionales, credenciales ni Herd; no hubo push, publicación ni despliegue.
