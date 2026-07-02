using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Rotativa;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel.Documentos;

namespace TAS360.Controllers
{
    public class DocumentoController : Controller
    {
        private static readonly HashSet<string> ImageExtensions =
            new HashSet<string>(new[] { ".jpg", ".jpeg", ".png" }, StringComparer.OrdinalIgnoreCase);

        private static int MaxImageBytes
        {
            get
            {
                int configured;
                return int.TryParse(ConfigurationManager.AppSettings["DocumentImageMaxBytes"], out configured) && configured > 0
                    ? configured
                    : 5 * 1024 * 1024;
            }
        }

        private readonly HelpDesk_Entities1 db = new HelpDesk_Entities1();

        [HttpGet]
        [AuthorizeUser("Mostrar_Documentos")]
        public ActionResult Index(string folio, string titulo, string cliente, string tipoDocumento, string estado)
        {
            try
            {
                var query = db.Documento.AsNoTracking().Where(x => x.Activo);

                folio = NullIfWhiteSpace(folio);
                titulo = NullIfWhiteSpace(titulo);
                cliente = NullIfWhiteSpace(cliente);
                tipoDocumento = NullIfWhiteSpace(tipoDocumento);
                estado = NullIfWhiteSpace(estado);

                if (folio != null) query = query.Where(x => x.Folio.Contains(folio));
                if (titulo != null) query = query.Where(x => x.Titulo.Contains(titulo));
                if (cliente != null) query = query.Where(x =>
                    (x.ClienteNombre != null && x.ClienteNombre.Contains(cliente)) ||
                    (x.ContenidoJson != null && x.ContenidoJson.Contains(cliente)));
                if (tipoDocumento != null) query = query.Where(x => x.TipoDocumento == tipoDocumento);
                if (estado != null) query = query.Where(x => x.Estado == estado);

                var rows = query.OrderByDescending(x => x.UpdatedAt).ToList();
                var model = new DocumentoIndexViewModel
                {
                    Folio = folio,
                    Titulo = titulo,
                    Cliente = cliente,
                    TipoDocumento = tipoDocumento,
                    Estado = estado,
                    CanCreate = HasPermission("Crear_Documento"),
                    CanEdit = HasPermission("Editar_Documento"),
                    CanDelete = HasPermission("Eliminar_Documento"),
                    CanGenerate = HasPermission("Generar_Documento"),
                    Documentos = rows.Select(x => new DocumentoListItemViewModel
                    {
                        Id = x.Id,
                        Folio = x.Folio,
                        TipoDocumento = x.TipoDocumento,
                        Estado = x.Estado,
                        Titulo = x.Titulo,
                        Cliente = x.ClienteNombre ?? ReadClient(x.ContenidoJson),
                        FechaEmision = x.FechaDocumento ?? DateTime.MinValue,
                        UpdatedAt = x.UpdatedAt,
                        HasPdf = !string.IsNullOrEmpty(x.RutaUltimoPdf)
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR INDEX: " + ex);
                ViewBag.ExceptionMessage = "No fue posible consultar los documentos.";
                return View(new DocumentoIndexViewModel());
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
                    Observaciones = NullIfWhiteSpace(model.Observaciones),
                    Notas = NullIfWhiteSpace(model.Notas),
                    ClienteNombre = NullIfWhiteSpace(model.Cliente),
                    ResponsableNombre = NullIfWhiteSpace(model.ResponsableNombre),
                    ResponsablePuesto = NullIfWhiteSpace(model.ResponsablePuesto),
                    LugarEmision = NullIfWhiteSpace(model.LugarEmision),
                    SubTotal = 0,
                    IVA = model.IVA,
                    Total = model.IVA,
                    ContenidoJson = WriteClient(null, model.Cliente),
                    FechaDocumento = model.FechaEmision.Value.Date,
                    Activo = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId
                };

                db.Documento.Add(documento);
                db.SaveChanges();
                AuditLog("Crear documento", documento.Id, documento.Folio, "Documento creado.");
                TempData["InfoMessage"] = "Documento creado. Ya puede agregar secciones, conceptos, firmas e imágenes.";
                return RedirectToAction("Edit", new { id = documento.Id });
            }
            catch (Exception ex)
            {
                AuditLog("Error al crear documento", null, model == null ? null : model.Folio, null, ex);
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
                ViewBag.ImageMaxBytes = MaxImageBytes;
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
                    ViewBag.ImageMaxBytes = MaxImageBytes;
                    return View(model);
                }

                int userId = GetCurrentUserId();
                documento.Folio = model.Folio.Trim();
                documento.TipoDocumento = model.TipoDocumento;
                documento.Estado = model.Estado;
                documento.Titulo = model.Titulo.Trim();
                documento.Descripcion = NullIfWhiteSpace(model.Descripcion);
                documento.Observaciones = NullIfWhiteSpace(model.Observaciones);
                documento.Notas = NullIfWhiteSpace(model.Notas);
                documento.ClienteNombre = NullIfWhiteSpace(model.Cliente);
                documento.ResponsableNombre = NullIfWhiteSpace(model.ResponsableNombre);
                documento.ResponsablePuesto = NullIfWhiteSpace(model.ResponsablePuesto);
                documento.LugarEmision = NullIfWhiteSpace(model.LugarEmision);
                documento.IVA = model.IVA;
                RecalculateTotals(documento);
                documento.ContenidoJson = WriteClient(documento.ContenidoJson, model.Cliente);
                documento.FechaDocumento = model.FechaEmision.Value.Date;
                documento.UpdatedAt = DateTime.UtcNow;
                documento.UpdatedByUserId = userId;
                db.SaveChanges();

                AuditLog("Editar documento", documento.Id, documento.Folio, "Datos generales actualizados.");
                TempData["InfoMessage"] = "Documento actualizado correctamente.";
                return RedirectToAction("Edit", new { id = documento.Id });
            }
            catch (Exception ex)
            {
                AuditLog("Error al editar documento", model.Id, model.Folio, null, ex);
                FillChildren(model);
                ViewBag.ImageMaxBytes = MaxImageBytes;
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
                ViewBag.CanGenerate = HasPermission("Generar_Documento");
                return model == null ? (ActionResult)HttpNotFound() : View(model);
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DETAILS ID " + id + ": " + ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        //[AuthorizeUser("Generar_Documento")]
        public ActionResult DocumentPdf(int id)
        {
            var model = BuildDocumentViewModel(id);
            return model == null ? (ActionResult)HttpNotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUser("Generar_Documento")]
        public ActionResult GeneratePdf(int id)
        {
            try
            {
                var documento = GetActiveDocument(id);
                if (documento == null) return HttpNotFound();

                string relativeFolder = "/DocumentFiles/" + id + "/Pdf/";
                string physicalFolder = Server.MapPath("~" + relativeFolder);
                Directory.CreateDirectory(physicalFolder);
                string fileName = "Documento_" + id + ".pdf";
                string physicalPath = Path.Combine(physicalFolder, fileName);
                string temporaryPath = physicalPath + ".tmp";

                var bytes = new ActionAsPdf("DocumentPdf", new { id })
                {
                    PageSize = Rotativa.Options.Size.A4,
                    PageOrientation = Rotativa.Options.Orientation.Portrait,
                    CustomSwitches = "--margin-top 12 --margin-right 12 --margin-bottom 12 --margin-left 12 --print-media-type"
                }.BuildFile(ControllerContext);

                System.IO.File.WriteAllBytes(temporaryPath, bytes);
                if (System.IO.File.Exists(physicalPath)) System.IO.File.Replace(temporaryPath, physicalPath, null);
                else System.IO.File.Move(temporaryPath, physicalPath);

                documento.RutaUltimoPdf = relativeFolder + fileName;
                documento.PdfGeneratedAt = DateTime.UtcNow;
                documento.Estado = "Generado";
                documento.UpdatedAt = DateTime.UtcNow;
                documento.UpdatedByUserId = GetCurrentUserId();
                db.SaveChanges();

                AuditLog("Generar PDF", documento.Id, documento.Folio, "Último PDF reemplazado: " + documento.RutaUltimoPdf);
                TempData["InfoMessage"] = "PDF generado correctamente.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                AuditLog("Error al generar PDF", id, GetDocumentFolio(id), null, ex);
                TempData["ErrorMessage"] = "No fue posible generar el PDF.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpGet]
        [AuthorizeUser("Mostrar_Documentos")]
        public ActionResult DownloadPdf(int id)
        {
            try
            {
                var documento = db.Documento.AsNoTracking().FirstOrDefault(x => x.Id == id && x.Activo);
                if (documento == null) return HttpNotFound();
                if (string.IsNullOrWhiteSpace(documento.RutaUltimoPdf))
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound, "El documento todavía no tiene PDF.");

                string allowedFolder = Path.GetFullPath(Server.MapPath("~/DocumentFiles/" + id + "/Pdf/"));
                string physicalPath = Path.GetFullPath(Server.MapPath("~" + documento.RutaUltimoPdf));
                if (!physicalPath.StartsWith(allowedFolder, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(physicalPath))
                    return HttpNotFound();

                AuditLog("Descargar PDF", id, documento.Folio, documento.RutaUltimoPdf);
                return File(physicalPath, "application/pdf", SafePdfFileName(documento.Folio));
            }
            catch (Exception ex)
            {
                AuditLog("Error al descargar PDF", id, GetDocumentFolio(id), null, ex);
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

                AuditLog("Eliminar documento", id, documento.Folio, "Eliminación lógica; archivos físicos conservados.");
                TempData["InfoMessage"] = "Documento eliminado lógicamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                AuditLog("Error al eliminar documento", id, GetDocumentFolio(id), null, ex);
                TempData["ErrorMessage"] = "No fue posible eliminar el documento.";
                return RedirectToAction("Delete", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
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
                AuditLog("Guardar sección", model.DocumentoId, GetDocumentFolio(model.DocumentoId), "SeccionId: " + entity.Id + "; Orden: " + entity.Orden);
                return RedirectToEdit(model.DocumentoId, "Sección guardada correctamente.");
            }
            catch (Exception ex)
            {
                AuditLog("Error al guardar sección", model.DocumentoId, GetDocumentFolio(model.DocumentoId), null, ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar la sección. Verifique que el orden no esté repetido.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
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
                AuditLog("Eliminar sección", documentoId, GetDocumentFolio(documentoId), "SeccionId: " + id + "; eliminación lógica.");
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
        [AuthorizeUser("Editar_Documento")]
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
                RecalculateTotals(GetActiveDocument(model.DocumentoId));
                db.SaveChanges();
                AuditLog("Guardar concepto", model.DocumentoId, GetDocumentFolio(model.DocumentoId), "ConceptoId: " + entity.Id);
                return RedirectToEdit(model.DocumentoId, "Concepto guardado correctamente.");
            }
            catch (Exception ex)
            {
                AuditLog("Error al guardar concepto", model.DocumentoId, GetDocumentFolio(model.DocumentoId), null, ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar el concepto.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
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
        [AuthorizeUser("Editar_Documento")]
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

                entity.Orden = model.Orden;
                entity.TipoFirma = model.TipoFirma.Trim();
                entity.NombreFirmante = model.NombreFirmante.Trim();
                entity.CargoFirmante = NullIfWhiteSpace(model.CargoFirmante);
                entity.FechaFirma = model.FechaFirma;
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                db.SaveChanges();
                AuditLog("Guardar firma", model.DocumentoId, GetDocumentFolio(model.DocumentoId), "FirmaId: " + entity.Id);
                return RedirectToEdit(model.DocumentoId, "Firma guardada correctamente.");
            }
            catch (Exception ex)
            {
                AuditLog("Error al guardar firma", model.DocumentoId, GetDocumentFolio(model.DocumentoId), null, ex);
                return RedirectWithError(model.DocumentoId, "No fue posible guardar la firma.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
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
        [AuthorizeUser("Editar_Documento")]
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

                AuditLog("Cargar imágenes", documentoId, GetDocumentFolio(documentoId), files.Count + " imagen(es); SeccionId: " + sectionId);
                return RedirectToEdit(documentoId, "Imágenes cargadas correctamente.");
            }
            catch (Exception ex)
            {
                foreach (string file in savedFiles)
                    try { if (System.IO.File.Exists(file)) System.IO.File.Delete(file); } catch { }
                AuditLog("Error al cargar imágenes", documentoId > 0 ? (int?)documentoId : null, GetDocumentFolio(documentoId), null, ex);
                return RedirectWithError(documentoId, "No fue posible guardar las imágenes.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
        public ActionResult DeleteImage(int id, int documentoId)
        {
            try
            {
                var image = db.DocumentoSeccionImagen.FirstOrDefault(x => x.Id == id && x.Activo);
                var section = image == null ? null : db.DocumentoSeccion.FirstOrDefault(x => x.Id == image.DocumentoSeccionId && x.DocumentoId == documentoId && x.Activo);
                if (image == null || section == null || GetActiveDocument(documentoId) == null) return HttpNotFound();
                SetInactive(new[] { image }, GetCurrentUserId(), DateTime.UtcNow);
                db.SaveChanges();
                AuditLog("Retirar imagen", documentoId, GetDocumentFolio(documentoId), "ImagenId: " + id + "; archivo físico conservado.");
                return RedirectToEdit(documentoId, "Imagen retirada del documento; el archivo físico se conservó.");
            }
            catch (Exception ex)
            {
                SafeLog("ERROR DELETE IMAGE: " + ex);
                return RedirectWithError(documentoId, "No fue posible retirar la imagen.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser("Editar_Documento")]
        public ActionResult SaveImageMetadata(int id, int documentoId, int orden, string textoAlternativo)
        {
            try
            {
                if (orden < 0) return RedirectWithError(documentoId, "El orden de la imagen no puede ser negativo.");
                if (!string.IsNullOrWhiteSpace(textoAlternativo) && textoAlternativo.Trim().Length > 250)
                    return RedirectWithError(documentoId, "El texto alternativo admite máximo 250 caracteres.");
                var image = db.DocumentoSeccionImagen.FirstOrDefault(x => x.Id == id && x.Activo);
                var section = image == null ? null : db.DocumentoSeccion.FirstOrDefault(x => x.Id == image.DocumentoSeccionId && x.DocumentoId == documentoId && x.Activo);
                if (image == null || section == null || GetActiveDocument(documentoId) == null) return HttpNotFound();

                image.Orden = orden;
                image.TextoAlternativo = NullIfWhiteSpace(textoAlternativo);
                image.UpdatedAt = DateTime.UtcNow;
                image.UpdatedByUserId = GetCurrentUserId();
                db.SaveChanges();
                AuditLog("Ordenar imagen", documentoId, GetDocumentFolio(documentoId), "ImagenId: " + id + "; Orden: " + orden);
                return RedirectToEdit(documentoId, "Imagen actualizada correctamente.");
            }
            catch (Exception ex)
            {
                AuditLog("Error al actualizar imagen", documentoId, GetDocumentFolio(documentoId), "ImagenId: " + id, ex);
                return RedirectWithError(documentoId, "No fue posible actualizar la imagen.");
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
                Observaciones = documento.Observaciones,
                Notas = documento.Notas,
                Cliente = documento.ClienteNombre ?? ReadClient(documento.ContenidoJson),
                ResponsableNombre = documento.ResponsableNombre,
                ResponsablePuesto = documento.ResponsablePuesto,
                LugarEmision = documento.LugarEmision,
                FechaEmision = documento.FechaDocumento,
                SubTotal = documento.SubTotal,
                IVA = documento.IVA,
                Total = documento.Total,
                RutaUltimoPdf = documento.RutaUltimoPdf,
                PdfGeneratedAt = documento.PdfGeneratedAt
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
                .Where(x => x.DocumentoId == model.Id && x.Activo).OrderBy(x => x.Orden).ThenBy(x => x.Id)
                .Select(x => new DocumentoFirmaViewModel
                {
                    Id = x.Id, DocumentoId = x.DocumentoId, Orden = x.Orden, TipoFirma = x.TipoFirma,
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
                return "Formato no permitido. Use JPG, JPEG o PNG.";
            if (file.ContentLength > MaxImageBytes)
                return "Cada imagen debe pesar como máximo " + Math.Ceiling(MaxImageBytes / 1048576m) + " MB.";
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
                if (entity is DocumentoConcepto)
                {
                    RecalculateTotals(GetActiveDocument(documentoId));
                    db.SaveChanges();
                }
                AuditLog("Eliminar " + entityName, documentoId, GetDocumentFolio(documentoId), "Eliminación lógica.");
                return RedirectToEdit(documentoId, entityName + " eliminado lógicamente.");
            }
            catch (Exception ex)
            {
                AuditLog("Error al eliminar " + entityName, documentoId, GetDocumentFolio(documentoId), null, ex);
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
            AuditLog(message, null, null);
        }

        private void AuditLog(string action, int? documentoId, string folio, string detail = null, Exception exception = null)
        {
            User user = null;
            string ip = null;
            try { user = Session == null ? null : Session["User"] as User; } catch { }
            try { ip = Request == null ? null : Request.UserHostAddress; } catch { }
            string entry = "FechaUtc: " + DateTime.UtcNow.ToString("o") +
                " | UsuarioId: " + (user == null ? "N/D" : user.id.ToString()) +
                " | NombreUsuario: " + (user == null ? "N/D" : user.nombre) +
                " | IP: " + (string.IsNullOrWhiteSpace(ip) ? "N/D" : ip) +
                " | Acción: " + action +
                " | DocumentoId: " + (documentoId.HasValue ? documentoId.Value.ToString() : "N/D") +
                " | Folio: " + (string.IsNullOrWhiteSpace(folio) ? "N/D" : folio) +
                (string.IsNullOrWhiteSpace(detail) ? "" : " | Detalle: " + detail) +
                (exception == null ? "" : " | Excepción: " + exception);
            try { new Log(Server.MapPath("~/Logs/Documentos/")).Add(entry); }
            catch { System.Diagnostics.Trace.TraceError(entry); }
        }

        private bool HasPermission(string operationName)
        {
            var user = Session["User"] as User;
            if (user == null || !user.id_Roll.HasValue) return false;

            return db.Roll_Operacion.Any(x => x.id_Roll == user.id_Roll &&
                x.Operacion.nombre == operationName);
        }

        private void RecalculateTotals(Documento documento)
        {
            if (documento == null) return;
            documento.SubTotal = db.DocumentoConcepto
                .Where(x => x.DocumentoId == documento.Id && x.Activo)
                .Select(x => (decimal?)x.Importe).Sum() ?? 0;
            documento.Total = documento.SubTotal + documento.IVA;
        }

        private string GetDocumentFolio(int id)
        {
            return db.Documento.AsNoTracking().Where(x => x.Id == id).Select(x => x.Folio).FirstOrDefault();
        }

        private static string SafePdfFileName(string folio)
        {
            string value = string.IsNullOrWhiteSpace(folio) ? "Documento" : folio;
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value + ".pdf";
        }

        private static string ReadClient(string contentJson)
        {
            if (string.IsNullOrWhiteSpace(contentJson)) return null;
            try { return (string)JObject.Parse(contentJson)["Cliente"]; }
            catch { return null; }
        }

        private static string WriteClient(string contentJson, string client)
        {
            JObject metadata;
            try { metadata = string.IsNullOrWhiteSpace(contentJson) ? new JObject() : JObject.Parse(contentJson); }
            catch { metadata = new JObject(); }

            string normalizedClient = NullIfWhiteSpace(client);
            if (normalizedClient == null) metadata.Remove("Cliente");
            else metadata["Cliente"] = normalizedClient;
            return metadata.ToString(Newtonsoft.Json.Formatting.None);
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
