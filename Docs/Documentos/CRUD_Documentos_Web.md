# Validación funcional — CRUD Documentos Web

## Alcance

Validación del módulo persistente Documentos en ASP.NET MVC 5, .NET Framework 4.7.2 y Entity Framework 6 Database First.

No se incluyen generación PDF, API móvil ni sincronización MAUI.

## Seguridad

El módulo utiliza `AuthorizeUser` resolviendo operaciones por nombre, para evitar depender de IDs `IDENTITY` distintos entre ambientes.

| Acción | Operación |
|---|---|
| Index y Details | `Mostrar_Documentos` |
| Create | `Crear_Documento` |
| Edit y administración de secciones, imágenes, conceptos y firmas | `Editar_Documento` |
| Delete lógico | `Eliminar_Documento` |

Las acciones POST incluyen `ValidateAntiForgeryToken`. El Index calcula los permisos del rol autenticado y sólo muestra los botones Crear, Editar y Eliminar autorizados. Details se muestra cuando el usuario tiene `Mostrar_Documentos`.

## Persistencia y archivos

- Todas las consultas del CRUD filtran `Activo = true`.
- Create guarda `SyncGuid`, `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId` y `Activo = true`.
- Edit actualiza `UpdatedAt` y `UpdatedByUserId`.
- Delete cambia `Activo = false` y `Estado = Eliminado`; también desactiva los hijos en una transacción.
- Las imágenes se guardan físicamente en `/DocumentFiles/{DocumentoId}/Images/`.
- `DocumentoSeccionImagen` conserva sólo `RutaArchivo`; `Imagen` permanece `NULL`.
- Retirar imágenes o eliminar documentos/secciones no borra archivos físicos.
- Sólo se elimina un archivo recién cargado si falla su propia transacción, para no dejar archivos huérfanos de una operación fallida.
- Extensiones permitidas: JPG, JPEG, PNG, GIF y WEBP; máximo 5 MB por archivo y MIME `image/*` obligatorio.

## Cliente y filtros

La tabla existente no tiene una columna `Cliente`. Para evitar otra migración, el valor se conserva dentro de `Documento.ContenidoJson` con la propiedad `Cliente`. El JSON existente se preserva al editar.

Index permite combinar filtros server-side por:

- folio;
- título;
- cliente;
- tipo de documento;
- estado.

## Logs

Se reutiliza `Log` y se escribe en `/Logs/Documentos/`. Cada entrada incluye:

- ID del usuario de sesión;
- nombre del usuario;
- ID del rol;
- operación o excepción controlada.

Se registran creación, edición, eliminación lógica, altas/cambios/bajas de hijos, carga de imágenes y errores.

## Prueba de integración ejecutada

Se ejecutó el controlador compilado contra la BD local configurada por el proyecto con una sesión autenticada simulada. La prueba creó o actualizó:

- Tipo: `Requerimiento`;
- Folio: `REQ-001`;
- Título: `Prueba módulo documentos`;
- Cliente: `Cliente prueba NEGMON`;
- una sección con descripción;
- dos imágenes;
- dos conceptos;
- una firma.

Resultado: **27 validaciones correctas, 0 errores**.

Validaciones cubiertas:

- campos obligatorios por DataAnnotations;
- presencia de `AuthorizeUser` en las acciones;
- antiforgery en todos los POST;
- auditoría y `SyncGuid` en Create/Edit;
- dos archivos físicos y sólo rutas relativas en BD;
- Details con secciones ordenadas e hijos activos;
- búsqueda combinada por los cinco criterios;
- acciones visibles según permisos;
- rechazo de extensión/MIME inválidos;
- eliminación lógica y exclusión posterior del Index;
- logs con datos del usuario autenticado.

También se creó y eliminó lógicamente `REQ-DELETE-TEST` para validar Delete sin afectar `REQ-001`.

## Validación técnica

- Compilación MSBuild: correcta.
- Las cinco vistas del módulo pasan el parser Razor.
- EDMX válido y procesado durante compilación.
- Permanecen seis warnings C# preexistentes en archivos ajenos al módulo.
- La precompilación global de vistas sigue bloqueada por una referencia preexistente en `Views/Aditivo/VolTotalizado.cshtml` a `RepTotalizadoViewModel`.
- El sitio IIS activo redirige correctamente a Login al intentar entrar a `/Documento/Index`; no se sustituyó ese despliegue porque apunta a `C:\Projects\Negmon` y no es un checkout Git.

## Checklist manual posterior al despliegue

- [ ] Publicar/ejecutar la rama sobre el sitio local IIS.
- [ ] Iniciar sesión con un rol que tenga las cuatro operaciones del CRUD.
- [ ] Abrir Documentos desde el menú.
- [ ] Confirmar que `REQ-001` aparece y que los cinco filtros lo encuentran.
- [ ] Confirmar visualmente que las acciones cambian al usar roles con permisos distintos.
- [ ] Abrir Details y comprobar sección, dos imágenes, dos conceptos y una firma.
- [ ] Editar datos generales y confirmar auditoría en BD.
- [ ] Cargar varias imágenes válidas y probar tamaño/extensión inválidos.
- [ ] Retirar una imagen y confirmar que el archivo físico permanece.
- [ ] Eliminar lógicamente un documento desechable y confirmar que desaparece del Index.
- [ ] Revisar `/Logs/Documentos/log_YYYY_M_D.txt`.

## Commit sugerido

`fix(documentos): completar filtros permisos logs y validación funcional`
