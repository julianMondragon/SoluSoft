using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel.Documentos;

namespace TAS360.Controllers
{
    public class DocumentoController : Controller
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> ImageExtensions =
            new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }, StringComparer.OrdinalIgnoreCase);

        private readonly HelpDesk_Entities1 db = new HelpDesk_Entities1();

        [HttpGet]
        [AuthorizeUser("Mostrar_Documentos")]
        public ActionResult Index()
        {
            try
            {
                var documentos = db.Documento.AsNoTracking()
                    .Where(x => x.Activo)
                    .OrderByDescending(x => x.UpdatedAt)
                    .Select(x => new DocumentoListItemViewModel
                    {
                        Id = x.Id,
                        Folio = x.Folio,
                        TipoDocumento = x.TipoDocumento,
                        Estado = x.Estado,
                        Titulo = x.Titulo,
                        FechaEmision = x.FechaDocumento ?? DateTime.MinValue,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToList();

                return View(documentos);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR INDEX: " + ex);
                ViewBag.ExceptionMessage = "No fue posible consultar los documentos.";
                return View(new List<DocumentoListItemViewModel>());
            }
        }

        [HttpGet]
        [AuthorizeUser("Crear_Documento")]
        public ActionResult Create()
        {
            return View(new DocumentoEditViewModel
            {
                Estado = "Borrador",
                FechaEmision = DateTime.Today,
                TipoDocumento = "DocumentoGeneral"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Crear_Documento")]
        public ActionResult Create(DocumentoEditViewModel model)
        {
            try
            {
                ValidateHeader(model, null);
                if (!ModelState.IsValid)
                    return View(model);

                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                var documento = new Documento
                {
                    SyncGuid = Guid.NewGuid(),
                    Folio = model.Folio.Trim(),
                    TipoDocumento = model.TipoDocumento,
                    Estado = "Borrador",
                    Titulo = model.Titulo.Trim(),
                    Descripcion = NullIfWhiteSpace(model.Descripcion),
                    FechaDocumento = model.FechaEmision.Value.Date,
                    Activo = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId
                };

                db.Documento.Add(documento);
                db.SaveChanges();
                SafeLog("Documento creado ID: " + documento.Id + ", folio: " + documento.Folio + ", usuario: " + userId);
                TempData["InfoMessage"] = "Documento creado. Ya puede agregar secciones, conceptos, firmas e imágenes.";
                return RedirectToAction("Edit", new { id = documento.Id });
            }
            catch (Exception ex)
            {
                SafeLog("ERROR CREATE: " + ex);
                ViewBag.ExceptionMessage = "No fue posible crear el documento. " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        [AuthorizeUser("Editar_Documento")]
        public ActionResult Edit(int id)
        {
            try
            {
                var model = BuildDocumentViewModel(id);
                return model == null ? (ActionResult)HttpNotFound() : View(model);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR EDIT GET ID " + id + ": " + ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
        public ActionResult Edit(DocumentoEditViewModel model)
        {
            try
            {
                var documento = GetActiveDocument(model.Id);
                if (documento == null)
                    return HttpNotFound();

                ValidateHeader(model, model.Id);
                if (!ModelState.IsValid)
                {
                    FillChildren(model);
                    return View(model);
                }

                int userId = GetCurrentUserId();
                documento.Folio = model.Folio.Trim();
                documento.TipoDocumento = model.TipoDocumento;
                documento.Estado = model.Estado;
                documento.Titulo = model.Titulo.Trim();
                documento.Descripcion = NullIfWhiteSpace(model.Descripcion);
                documento.FechaDocumento = model.FechaEmision.Value.Date;
                documento.UpdatedAt = DateTime.UtcNow;
                documento.UpdatedByUserId = userId;
                db.SaveChanges();

                SafeLog("Documento editado ID: " + documento.Id + ", usuario: " + userId);
                TempData["InfoMessage"] = "Documento actualizado correctamente.";
                return RedirectToAction("Edit", new { id = documento.Id });
            }
            catch (Exception ex)
            {
                SafeLog("ERROR EDIT POST ID " + model.Id + ": " + ex);
                FillChildren(model);
                ViewBag.ExceptionMessage = "No fue posible actualizar el documento. " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        [AuthorizeUser("Mostrar_Documentos")]
        public ActionResult Details(int id)
        {
            try
            {
                var model = BuildDocumentViewModel(id);
                return model == null ? (ActionResult)HttpNotFound() : View(model);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DETAILS ID " + id + ": " + ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [AuthorizeUser("Eliminar_Documento")]
        public ActionResult Delete(int id)
        {
            try
            {
                var model = BuildDocumentViewModel(id);
                return model == null ? (ActionResult)HttpNotFound() : View(model);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE GET ID " + id + ": " + ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Eliminar_Documento")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                var documento = GetActiveDocument(id);
                if (documento == null)
                    return HttpNotFound();

                using (var transaction = db.Database.BeginTransaction())
                {
                    documento.Activo = false;
                    documento.Estado = "Eliminado";
                    documento.UpdatedAt = now;
                    documento.UpdatedByUserId = userId;

                    var secciones = db.DocumentoSeccion.Where(x => x.DocumentoId == id && x.Activo).ToList();
                    var seccionIds = secciones.Select(x => x.Id).ToList();
                    SetInactive(secciones, userId, now);
                    SetInactive(db.DocumentoConcepto.Where(x => x.DocumentoId == id && x.Activo).ToList(), userId, now);
                    SetInactive(db.DocumentoFirma.Where(x => x.DocumentoId == id && x.Activo).ToList(), userId, now);
                    SetInactive(db.DocumentoSeccionImagen.Where(x => seccionIds.Contains(x.DocumentoSeccionId) && x.Activo).ToList(), userId, now);

                    db.SaveChanges();
                    transaction.Commit();
                }

                SafeLog("Documento eliminado logicamente ID: " + id + ", usuario: " + userId);
                TempData["InfoMessage"] = "Documento eliminado lógicamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE ID " + id + ": " + ex);
                TempData["ErrorMessage"] = "No fue posible eliminar el documento.";
                return RedirectToAction("Delete", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult SaveSection(DocumentoSeccionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid || GetActiveDocument(model.DocumentoId) == null)
                    return RedirectWithError(model.DocumentoId, "Revise los datos de la sección.");

                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                DocumentoSeccion entity;

                if (model.Id == 0)
                {
                    entity = new DocumentoSeccion
                    {
                        DocumentoId = model.DocumentoId,
                        SyncGuid = Guid.NewGuid(),
                        Activo = true,
                        CreatedAt = now,
                        CreatedByUserId = userId
                    };
                    db.DocumentoSeccion.Add(entity);
                }
                else
                {
                    entity = db.DocumentoSeccion.FirstOrDefault(x => x.Id == model.Id && x.DocumentoId == model.DocumentoId && x.Activo);
                    if (entity == null) return HttpNotFound();
                }

                entity.Orden = model.Orden;
                entity.Titulo = NullIfWhiteSpace(model.Titulo);
                entity.Contenido = NullIfWhiteSpace(model.Contenido);
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                db.SaveChanges();
                SafeLog("Seccion guardada ID: " + entity.Id + ", documento: " + model.DocumentoId);
                return RedirectToEdit(model.DocumentoId, "Sección guardada correctamente.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR SAVE SECTION: " + ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar la sección. Verifique que el orden no esté repetido.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult DeleteSection(int id, int documentoId)
        {
            try
            {
                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                var section = db.DocumentoSeccion.FirstOrDefault(x => x.Id == id && x.DocumentoId == documentoId && x.Activo);
                if (section == null || GetActiveDocument(documentoId) == null) return HttpNotFound();
                SetInactive(new[] { section }, userId, now);
                SetInactive(db.DocumentoSeccionImagen.Where(x => x.DocumentoSeccionId == id && x.Activo).ToList(), userId, now);
                db.SaveChanges();
                SafeLog("Seccion eliminada logicamente ID: " + id);
                return RedirectToEdit(documentoId, "Sección eliminada lógicamente.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE SECTION: " + ex);
                return RedirectWithError(documentoId, "No fue posible eliminar la sección.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult SaveConcept(DocumentoConceptoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid || GetActiveDocument(model.DocumentoId) == null)
                    return RedirectWithError(model.DocumentoId, "Revise los datos del concepto.");

                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                DocumentoConcepto entity;
                if (model.Id == 0)
                {
                    entity = new DocumentoConcepto
                    {
                        DocumentoId = model.DocumentoId,
                        SyncGuid = Guid.NewGuid(),
                        Activo = true,
                        CreatedAt = now,
                        CreatedByUserId = userId
                    };
                    db.DocumentoConcepto.Add(entity);
                }
                else
                {
                    entity = db.DocumentoConcepto.FirstOrDefault(x => x.Id == model.Id && x.DocumentoId == model.DocumentoId && x.Activo);
                    if (entity == null) return HttpNotFound();
                }

                entity.Orden = model.Orden;
                entity.Clave = NullIfWhiteSpace(model.Clave);
                entity.Descripcion = model.Descripcion.Trim();
                entity.Unidad = NullIfWhiteSpace(model.Unidad);
                entity.Cantidad = model.Cantidad;
                entity.PrecioUnitario = model.PrecioUnitario;
                entity.Importe = decimal.Round(model.Cantidad * model.PrecioUnitario, 4);
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                db.SaveChanges();
                SafeLog("Concepto guardado ID: " + entity.Id + ", documento: " + model.DocumentoId);
                return RedirectToEdit(model.DocumentoId, "Concepto guardado correctamente.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR SAVE CONCEPT: " + ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar el concepto.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult DeleteConcept(int id, int documentoId)
        {
            try
            {
                var entity = db.DocumentoConcepto.FirstOrDefault(x => x.Id == id && x.DocumentoId == documentoId && x.Activo);
                return LogicalDeleteChild(entity, documentoId, "Concepto");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR FIND CONCEPT: " + ex);
                return RedirectWithError(documentoId, "No fue posible eliminar el concepto.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult SaveSignature(DocumentoFirmaViewModel model)
        {
            try
            {
                if (!ModelState.IsValid || GetActiveDocument(model.DocumentoId) == null)
                    return RedirectWithError(model.DocumentoId, "Revise los datos de la firma.");

                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                DocumentoFirma entity;
                if (model.Id == 0)
                {
                    entity = new DocumentoFirma
                    {
                        DocumentoId = model.DocumentoId,
                        SyncGuid = Guid.NewGuid(),
                        Activo = true,
                        CreatedAt = now,
                        CreatedByUserId = userId
                    };
                    db.DocumentoFirma.Add(entity);
                }
                else
                {
                    entity = db.DocumentoFirma.FirstOrDefault(x => x.Id == model.Id && x.DocumentoId == model.DocumentoId && x.Activo);
                    if (entity == null) return HttpNotFound();
                }

                entity.TipoFirma = model.TipoFirma.Trim();
                entity.NombreFirmante = model.NombreFirmante.Trim();
                entity.CargoFirmante = NullIfWhiteSpace(model.CargoFirmante);
                entity.FechaFirma = model.FechaFirma;
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                db.SaveChanges();
                SafeLog("Firma guardada ID: " + entity.Id + ", documento: " + model.DocumentoId);
                return RedirectToEdit(model.DocumentoId, "Firma guardada correctamente.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR SAVE SIGNATURE: " + ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar la firma.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult DeleteSignature(int id, int documentoId)
        {
            try
            {
                var entity = db.DocumentoFirma.FirstOrDefault(x => x.Id == id && x.DocumentoId == documentoId && x.Activo);
                return LogicalDeleteChild(entity, documentoId, "Firma");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR FIND SIGNATURE: " + ex);
                return RedirectWithError(documentoId, "No fue posible eliminar la firma.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult UploadImages(int sectionId, IEnumerable<HttpPostedFileBase> images)
        {
            var savedFiles = new List<string>();
            int documentoId = 0;
            try
            {
                var section = db.DocumentoSeccion.FirstOrDefault(x => x.Id == sectionId && x.Activo);
                if (section == null || GetActiveDocument(section.DocumentoId) == null) return HttpNotFound();
                documentoId = section.DocumentoId;

                var files = (images ?? Enumerable.Empty<HttpPostedFileBase>())
                    .Where(x => x != null && x.ContentLength > 0).ToList();
                if (!files.Any()) return RedirectWithError(documentoId, "Seleccione al menos una imagen.");

                foreach (var file in files)
                {
                    string validationError = ValidateImage(file);
                    if (validationError != null) return RedirectWithError(documentoId, validationError);
                }

                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                int nextOrder = db.DocumentoSeccionImagen
                    .Where(x => x.DocumentoSeccionId == sectionId && x.Activo)
                    .Select(x => (int?)x.Orden).Max() ?? -1;

                string relativeFolder = "/DocumentFiles/" + documentoId + "/Images/";
                string physicalFolder = Server.MapPath("~" + relativeFolder);
                Directory.CreateDirectory(physicalFolder);

                using (var transaction = db.Database.BeginTransaction())
                {
                    foreach (var file in files)
                    {
                        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string fileName = Guid.NewGuid().ToString("N") + extension;
                        string physicalPath = Path.Combine(physicalFolder, fileName);
                        file.SaveAs(physicalPath);
                        savedFiles.Add(physicalPath);

                        db.DocumentoSeccionImagen.Add(new DocumentoSeccionImagen
                        {
                            DocumentoSeccionId = sectionId,
                            SyncGuid = Guid.NewGuid(),
                            Orden = ++nextOrder,
                            NombreArchivo = Path.GetFileName(file.FileName),
                            MimeType = file.ContentType,
                            RutaArchivo = relativeFolder + fileName,
                            Imagen = null,
                            Activo = true,
                            CreatedAt = now,
                            UpdatedAt = now,
                            CreatedByUserId = userId,
                            UpdatedByUserId = userId
                        });
                    }

                    db.SaveChanges();
                    transaction.Commit();
                }

                SafeLog(files.Count + " imagen(es) cargadas en seccion: " + sectionId);
                return RedirectToEdit(documentoId, "Imágenes cargadas correctamente.");
            }
            catch (Exception ex)
            {
                foreach (string file in savedFiles)
                    try { if (System.IO.File.Exists(file)) System.IO.File.Delete(file); } catch { }
                SafeLog("ERROR UPLOAD IMAGES: " + ex);
                return RedirectWithError(documentoId, "No fue posible guardar las imágenes.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Administrar_Documentos")]
        public ActionResult DeleteImage(int id, int documentoId)
        {
            try
            {
                var image = db.DocumentoSeccionImagen.FirstOrDefault(x => x.Id == id && x.Activo);
                var section = image == null ? null : db.DocumentoSeccion.FirstOrDefault(x => x.Id == image.DocumentoSeccionId && x.DocumentoId == documentoId && x.Activo);
                if (image == null || section == null || GetActiveDocument(documentoId) == null) return HttpNotFound();
                SetInactive(new[] { image }, GetCurrentUserId(), DateTime.UtcNow);
                db.SaveChanges();
                SafeLog("Imagen eliminada logicamente ID: " + id + ". El archivo fisico se conserva.");
                return RedirectToEdit(documentoId, "Imagen retirada del documento; el archivo físico se conservó.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE IMAGE: " + ex);
                return RedirectWithError(documentoId, "No fue posible retirar la imagen.");
            }
        }

        private DocumentoEditViewModel BuildDocumentViewModel(int id)
        {
            var documento = db.Documento.AsNoTracking().FirstOrDefault(x => x.Id == id && x.Activo);
            if (documento == null) return null;

            var model = new DocumentoEditViewModel
            {
                Id = documento.Id,
                Folio = documento.Folio,
                TipoDocumento = documento.TipoDocumento,
                Estado = documento.Estado,
                Titulo = documento.Titulo,
                Descripcion = documento.Descripcion,
                FechaEmision = documento.FechaDocumento
            };
            FillChildren(model);
            return model;
        }

        private void FillChildren(DocumentoEditViewModel model)
        {
            model.Secciones = db.DocumentoSeccion.AsNoTracking()
                .Where(x => x.DocumentoId == model.Id && x.Activo)
                .OrderBy(x => x.Orden)
                .Select(x => new DocumentoSeccionViewModel
                {
                    Id = x.Id, DocumentoId = x.DocumentoId, Orden = x.Orden, Titulo = x.Titulo, Contenido = x.Contenido
                }).ToList();

            var sectionIds = model.Secciones.Select(x => x.Id).ToList();
            var images = db.DocumentoSeccionImagen.AsNoTracking()
                .Where(x => sectionIds.Contains(x.DocumentoSeccionId) && x.Activo)
                .OrderBy(x => x.Orden)
                .Select(x => new DocumentoImagenViewModel
                {
                    Id = x.Id, DocumentoSeccionId = x.DocumentoSeccionId, Orden = x.Orden,
                    NombreArchivo = x.NombreArchivo, MimeType = x.MimeType, RutaArchivo = x.RutaArchivo,
                    TextoAlternativo = x.TextoAlternativo
                }).ToList();
            foreach (var section in model.Secciones)
                section.Imagenes = images.Where(x => x.DocumentoSeccionId == section.Id).ToList();

            model.Conceptos = db.DocumentoConcepto.AsNoTracking()
                .Where(x => x.DocumentoId == model.Id && x.Activo).OrderBy(x => x.Orden)
                .Select(x => new DocumentoConceptoViewModel
                {
                    Id = x.Id, DocumentoId = x.DocumentoId, Orden = x.Orden, Clave = x.Clave,
                    Descripcion = x.Descripcion, Unidad = x.Unidad, Cantidad = x.Cantidad, PrecioUnitario = x.PrecioUnitario
                }).ToList();

            model.Firmas = db.DocumentoFirma.AsNoTracking()
                .Where(x => x.DocumentoId == model.Id && x.Activo).OrderBy(x => x.Id)
                .Select(x => new DocumentoFirmaViewModel
                {
                    Id = x.Id, DocumentoId = x.DocumentoId, TipoFirma = x.TipoFirma,
                    NombreFirmante = x.NombreFirmante, CargoFirmante = x.CargoFirmante, FechaFirma = x.FechaFirma
                }).ToList();
        }

        private void ValidateHeader(DocumentoEditViewModel model, int? currentId)
        {
            string[] validTypes = { "ReporteServicio", "Cotizacion", "Requerimiento", "DocumentoGeneral" };
            string[] validStates = { "Borrador", "Generado", "Cancelado" };
            if (!string.IsNullOrWhiteSpace(model.TipoDocumento) && !validTypes.Contains(model.TipoDocumento))
                ModelState.AddModelError("TipoDocumento", "El tipo de documento no es válido.");
            if (!string.IsNullOrWhiteSpace(model.Estado) && !validStates.Contains(model.Estado))
                ModelState.AddModelError("Estado", "El estado no es válido.");
            if (!string.IsNullOrWhiteSpace(model.Folio))
            {
                string folio = model.Folio.Trim();
                if (db.Documento.Any(x => x.Activo && x.Folio == folio && (!currentId.HasValue || x.Id != currentId.Value)))
                    ModelState.AddModelError("Folio", "Ya existe un documento activo con ese folio.");
            }
        }

        private string ValidateImage(HttpPostedFileBase file)
        {
            string extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !ImageExtensions.Contains(extension))
                return "Formato no permitido. Use JPG, JPEG, PNG, GIF o WEBP.";
            if (file.ContentLength > MaxImageBytes)
                return "Cada imagen debe pesar como máximo 5 MB.";
            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return "El archivo seleccionado no tiene un tipo MIME de imagen válido.";
            return null;
        }

        private ActionResult LogicalDeleteChild(object entity, int documentoId, string entityName)
        {
            try
            {
                if (entity == null || GetActiveDocument(documentoId) == null) return HttpNotFound();
                int userId = GetCurrentUserId();
                DateTime now = DateTime.UtcNow;
                if (entity is DocumentoConcepto) SetInactive(new[] { (DocumentoConcepto)entity }, userId, now);
                else if (entity is DocumentoFirma) SetInactive(new[] { (DocumentoFirma)entity }, userId, now);
                else return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                db.SaveChanges();
                SafeLog(entityName + " eliminado logicamente del documento: " + documentoId);
                return RedirectToEdit(documentoId, entityName + " eliminado lógicamente.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE " + entityName.ToUpperInvariant() + ": " + ex);
                return RedirectWithError(documentoId, "No fue posible eliminar el registro.");
            }
        }

        private Documento GetActiveDocument(int id)
        {
            return db.Documento.FirstOrDefault(x => x.Id == id && x.Activo);
        }

        private int GetCurrentUserId()
        {
            var user = Session["User"] as User;
            if (user == null) throw new InvalidOperationException("La sesión de usuario expiró.");
            return user.id;
        }

        private RedirectToRouteResult RedirectToEdit(int documentoId, string message)
        {
            TempData["InfoMessage"] = message;
            return RedirectToAction("Edit", new { id = documentoId });
        }

        private RedirectToRouteResult RedirectWithError(int documentoId, string message)
        {
            TempData["ErrorMessage"] = message;
            return documentoId > 0
                ? RedirectToAction("Edit", new { id = documentoId })
                : RedirectToAction("Index");
        }

        private void SafeLog(string message)
        {
            try { new Log(Server.MapPath("~/Logs/Documentos/")).Add(message); }
            catch { System.Diagnostics.Trace.TraceError(message); }
        }

        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void SetInactive(IEnumerable<DocumentoSeccion> records, int userId, DateTime now)
        {
            foreach (var item in records) { item.Activo = false; item.UpdatedAt = now; item.UpdatedByUserId = userId; }
        }
        private static void SetInactive(IEnumerable<DocumentoSeccionImagen> records, int userId, DateTime now)
        {
            foreach (var item in records) { item.Activo = false; item.UpdatedAt = now; item.UpdatedByUserId = userId; }
        }
        private static void SetInactive(IEnumerable<DocumentoConcepto> records, int userId, DateTime now)
        {
            foreach (var item in records) { item.Activo = false; item.UpdatedAt = now; item.UpdatedByUserId = userId; }
        }
        private static void SetInactive(IEnumerable<DocumentoFirma> records, int userId, DateTime now)
        {
            foreach (var item in records) { item.Activo = false; item.UpdatedAt = now; item.UpdatedByUserId = userId; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
