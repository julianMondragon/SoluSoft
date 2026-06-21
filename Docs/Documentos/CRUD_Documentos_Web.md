# CRUD y PDF de Documentos Web

## Alcance

Módulo persistente en ASP.NET MVC 5, .NET Framework 4.7.2 y Entity Framework 6 Database First. Permite administrar documentos y generar/descargar el último PDF. No incluye API móvil ni sincronización MAUI.

## Migración requerida

Antes de publicar el código, ejecutar en la BD destino:

`Backup_Database/Migraciones/Alter_Documentos_Pdf_Fields.sql`

El script es incremental e idempotente; no elimina registros. Agrega metadatos de cotización, `SubTotal`, `IVA`, `Total`, ruta/fecha del último PDF y `Orden` en firmas. Después, respaldar y regenerar el EDMX con **Update Model from Database** si se vuelve a generar el modelo desde Visual Studio; los cambios del EDMX ya están incluidos en esta rama.

## Datos y orden visual

- `DocumentoSeccion`, `DocumentoConcepto`, `DocumentoFirma` y `DocumentoSeccionImagen` se consultan por `Orden` ascendente.
- Se persisten descripción general, observaciones, notas, cliente, responsable, puesto, fecha y lugar de emisión.
- El importe de cada concepto es `Cantidad × PrecioUnitario`.
- Al guardar o retirar un concepto se recalcula `SubTotal`; `Total = SubTotal + IVA`.
- Todos los registros visibles requieren `Activo = true`; las bajas siguen siendo lógicas y los archivos físicos se conservan.

## Imágenes

- Ruta física: `/DocumentFiles/{DocumentoId}/Images/`; la BD guarda sólo la ruta relativa.
- La vista permite selección o arrastre múltiple y muestra miniaturas antes de enviar.
- Extensiones permitidas: JPG, JPEG y PNG; además se valida MIME `image/*`.
- El límite se configura con `DocumentImageMaxBytes` en `Web.config` (5 MB por defecto) y se valida en cliente y servidor.
- Orden y texto alternativo pueden editarse por imagen.

## PDF

- `GeneratePdf` requiere `Generar_Documento`, usa Rotativa y formato A4 vertical.
- El PDF respeta el orden de secciones, imágenes, conceptos y firmas.
- Se guarda en `/DocumentFiles/{DocumentoId}/Pdf/Documento_{DocumentoId}.pdf`.
- La regeneración reemplaza el mismo archivo; no se conserva historial de PDFs.
- La BD guarda la ruta relativa en `RutaUltimoPdf` y la fecha UTC en `PdfGeneratedAt`.
- `DownloadPdf` requiere `Mostrar_Documentos`, valida que la ruta permanezca dentro de la carpeta permitida y entrega un nombre basado en el folio.

## Seguridad y logs

| Acción | Operación |
|---|---|
| Index, Details y descarga | `Mostrar_Documentos` |
| Create | `Crear_Documento` |
| Edit e hijos | `Editar_Documento` |
| Delete lógico | `Eliminar_Documento` |
| Generación y vista interna del PDF | `Generar_Documento` |

Todos los POST incluyen `ValidateAntiForgeryToken`. El log existente en `/Logs/Documentos/` registra fecha UTC, ID y nombre de usuario, IP, acción, DocumentoId, folio, detalle y excepción controlada cuando aplica.

## Validaciones técnicas realizadas

- Compilación MSBuild correcta; quedan seis warnings preexistentes fuera del módulo.
- EDMX válido como XML y procesado correctamente durante la compilación.
- `git diff --check` sin errores.
- El script SQL usa `IF COL_LENGTH`/catálogos de SQL Server para poder repetirse y no contiene `DELETE` ni `DROP`.
- La precompilación global ASP.NET del equipo queda bloqueada por la dependencia de diseño preexistente `System.Data.Entity.Design.AspNet.EntityDesignerBuildProvider`; no es un error de estas vistas.
- No se ejecutó la migración ni se alteró la BD local.

## Checklist manual después de ejecutar la migración

- [ ] Crear una cotización y completar cliente, responsable, puesto, fecha, lugar, descripción, observaciones y notas.
- [ ] Agregar secciones, imágenes, conceptos y firmas con órdenes fuera de secuencia; confirmar que Details y PDF los ordenan.
- [ ] Arrastrar dos JPG/PNG juntos y revisar miniaturas antes de guardar.
- [ ] Rechazar extensión no permitida y archivo mayor a `DocumentImageMaxBytes`.
- [ ] Confirmar `Importe`, `SubTotal`, `IVA` y `Total` en pantalla, BD y PDF.
- [ ] Generar PDF, descargarlo y comprobar contenido/imágenes/saltos de página.
- [ ] Regenerar PDF y confirmar que sólo existe `Documento_{Id}.pdf`.
- [ ] Retirar una imagen y comprobar `Activo = 0` y que el archivo físico permanece.
- [ ] Probar accesos con y sin `Mostrar_Documentos`, `Editar_Documento` y `Generar_Documento`.
- [ ] Revisar los campos estructurados del log para crear, editar, cargar imágenes, generar, descargar, eliminar y errores.

## Commit sugerido

`feat(documentos): generar y descargar último PDF con totales y orden`
