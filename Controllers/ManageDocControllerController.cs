using Rotativa;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Models.Enums;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class ManageDocController : Controller
    {
        private const string SessionKeyDocument = "CurrentDocumentVM";

        /// <summary>
        /// Muestra el formulario inicial para crear un documento.
        /// </summary>
        [HttpGet]
        public ActionResult CreateDocument()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                model = new DocumentViewModel
                {
                    DocumentSessionId = Guid.NewGuid(),
                    DocumentType = DocumentTypeEnum.DocumentoGeneral,
                    Folio = string.Empty,
                    Title = string.Empty,
                    Subtitle = string.Empty,
                    GeneralDescription = string.Empty,
                    Observations = string.Empty,
                    Notes = string.Empty,
                    ClientName = string.Empty,
                    ResponsibleName = string.Empty,
                    ResponsiblePosition = string.Empty,
                    IssueLocation = string.Empty
                };

                model.Sections.Add(new DocumentSectionViewModel
                {
                    Id = 1,
                    Order = 1,
                    ShowSectionTitle = true,
                    ShowDescription = true,
                    ShowImage = true
                });

                model.Items.Add(new DocumentItemViewModel
                {
                    Id = 1,
                    Order = 1
                });

                model.Signatures.Add(new DocumentSignatureViewModel
                {
                    Id = 1,
                    Order = 1,
                    ShowSignature = true
                });

                SaveDocumentInSession(model);
            }

            LoadDocumentTypes(model.DocumentType);
            return View(model);
        }

        /// <summary>
        /// Recibe el formulario del documento, recalcula totales,
        /// guarda el modelo en Session y redirige a la vista previa.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDocument(DocumentViewModel model, string submitAction)
        {
            try
            {
                if (model == null)
                {
                    ModelState.AddModelError("", "No se recibió información del documento.");
                    LoadDocumentTypes();
                    return View(new DocumentViewModel());
                }

                NormalizeDocument(model);
                CalculateTotals(model);

                if (model.DocumentSessionId == Guid.Empty)
                {
                    model.DocumentSessionId = Guid.NewGuid();
                }

                switch (submitAction)
                {
                    case "addSection":
                        if (model.Sections == null)
                        {
                            model.Sections = new List<DocumentSectionViewModel>();
                        }

                        int nextSectionId = model.Sections.Any() ? model.Sections.Max(x => x.Id) + 1 : 1;
                        int nextSectionOrder = model.Sections.Any() ? model.Sections.Max(x => x.Order) + 1 : 1;

                        model.Sections.Add(new DocumentSectionViewModel
                        {
                            Id = nextSectionId,
                            Order = nextSectionOrder,
                            ShowSectionTitle = true,
                            ShowDescription = true,
                            ShowImage = true,
                            SectionTitle = string.Empty,
                            Description = string.Empty,
                            ImagePath = string.Empty,
                            ImageName = string.Empty
                        });

                        SaveDocumentInSession(model);
                        LoadDocumentTypes(model.DocumentType);
                        return View(model);

                    case "addItem":
                        if (model.Items == null)
                        {
                            model.Items = new List<DocumentItemViewModel>();
                        }

                        int nextItemId = model.Items.Any() ? model.Items.Max(x => x.Id) + 1 : 1;
                        int nextItemOrder = model.Items.Any() ? model.Items.Max(x => x.Order) + 1 : 1;

                        model.Items.Add(new DocumentItemViewModel
                        {
                            Id = nextItemId,
                            Order = nextItemOrder,
                            Concept = string.Empty,
                            Description = string.Empty,
                            Quantity = 0,
                            UnitPrice = 0
                        });

                        SaveDocumentInSession(model);
                        LoadDocumentTypes(model.DocumentType);
                        return View(model);

                    case "preview":
                        if (!ModelState.IsValid)
                        {
                            LoadDocumentTypes(model.DocumentType);
                            return View(model);
                        }

                        SaveDocumentInSession(model);
                        return RedirectToAction("DocumentReport");

                    case "save":
                        if (!ModelState.IsValid)
                        {
                            LoadDocumentTypes(model.DocumentType);
                            return View(model);
                        }

                        SaveDocumentInSession(model);
                        TempData["InfoMessage"] = "Documento guardado correctamente.";
                        return RedirectToAction("CreateDocument");

                    default:
                        if (!string.IsNullOrWhiteSpace(submitAction) && submitAction.StartsWith("removeSection_"))
                        {
                            int sectionId;
                            if (int.TryParse(submitAction.Replace("removeSection_", ""), out sectionId))
                            {
                                if (model.Sections != null)
                                {
                                    var sectionToRemove = model.Sections.FirstOrDefault(x => x.Id == sectionId);
                                    if (sectionToRemove != null)
                                    {
                                        model.Sections.Remove(sectionToRemove);
                                    }
                                }
                            }

                            SaveDocumentInSession(model);
                            LoadDocumentTypes(model.DocumentType);
                            return View(model);
                        }

                        if (!string.IsNullOrWhiteSpace(submitAction) && submitAction.StartsWith("removeItem_"))
                        {
                            int itemId;
                            if (int.TryParse(submitAction.Replace("removeItem_", ""), out itemId))
                            {
                                if (model.Items != null)
                                {
                                    var itemToRemove = model.Items.FirstOrDefault(x => x.Id == itemId);
                                    if (itemToRemove != null)
                                    {
                                        model.Items.Remove(itemToRemove);
                                    }
                                }
                            }

                            SaveDocumentInSession(model);
                            LoadDocumentTypes(model.DocumentType);
                            return View(model);
                        }

                        if (!ModelState.IsValid)
                        {
                            LoadDocumentTypes(model.DocumentType);
                            return View(model);
                        }

                        SaveDocumentInSession(model);
                        return RedirectToAction("CreateDocument");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = ex.Message;
                LoadDocumentTypes(model != null ? model.DocumentType : DocumentTypeEnum.DocumentoGeneral);
                return View(model);
            }
        }

        /// <summary>
        /// Muestra la vista previa del documento a imprimir.
        /// </summary>
        [HttpGet]
        public ActionResult DocumentReport()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            CalculateTotals(model);
            ViewBag.IsPdf = false;
            return View(model);
        }

        /// <summary>
        /// Muestra la vista previa del documento a imprimir.
        /// </summary>
        [HttpGet]
        public ActionResult DocumentReport2()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            CalculateTotals(model);
            ViewBag.IsPdf = false;
            return View(model);
        }

        /// <summary>
        /// Genera el PDF del documento actual almacenado en Session.
        /// </summary>
        [HttpGet]
        public ActionResult PrintDocumentReport()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            CalculateTotals(model);
            var fileName = BuildPdfFileName(model);
            ViewBag.IsPdf = true;

            return new ViewAsPdf("DocumentReport2", model)
            {
                FileName = fileName
            };
        }

        /// <summary>
        /// Agrega una imagen a una sección específica del documento actual.
        /// </summary>
        /// <param name="sectionIndex">Índice de la sección dentro de la lista.</param>
        /// <param name="postedFile">Archivo de imagen cargado.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDescriptionImage(int sectionIndex, HttpPostedFileBase postedFile)
        {
            try
            {
                var model = GetDocumentFromSession();

                if (model == null)
                {
                    return RedirectToAction("CreateDocument");
                }

                if (postedFile == null || postedFile.ContentLength <= 0)
                {
                    TempData["WarningMessage"] = "Seleccione una imagen válida.";
                    return RedirectToAction("DocumentReport");
                }

                if (model.Sections == null || !model.Sections.Any())
                {
                    TempData["WarningMessage"] = "El documento no contiene secciones.";
                    return RedirectToAction("DocumentReport");
                }

                if (sectionIndex < 0 || sectionIndex >= model.Sections.Count)
                {
                    TempData["WarningMessage"] = "La sección seleccionada no es válida.";
                    return RedirectToAction("DocumentReport");
                }

                string extension = Path.GetExtension(postedFile.FileName);
                string[] validExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

                if (string.IsNullOrWhiteSpace(extension) || !validExtensions.Contains(extension.ToLower()))
                {
                    TempData["WarningMessage"] = "Solo se permiten imágenes .jpg, .jpeg, .png, .bmp o .webp.";
                    return RedirectToAction("DocumentReport");
                }

                string relativeFolder = "~/DocumentFiles/" + model.DocumentSessionId + "/";
                string physicalFolder = Server.MapPath(relativeFolder);

                if (!Directory.Exists(physicalFolder))
                {
                    Directory.CreateDirectory(physicalFolder);
                }

                string safeFileName = string.Format(
                    "Section_{0}_{1}{2}",
                    sectionIndex + 1,
                    DateTime.Now.ToString("yyyyMMddHHmmss"),
                    extension.ToLower());

                string physicalPath = Path.Combine(physicalFolder, safeFileName);
                postedFile.SaveAs(physicalPath);

                string relativeUrl = Url.Content(relativeFolder + safeFileName);
                string absoluteUrl = string.Format(
                    "{0}://{1}{2}",
                    Request.Url.Scheme,
                    Request.Url.Authority,
                    relativeUrl);

                model.Sections[sectionIndex].ImagePath = absoluteUrl;
                model.Sections[sectionIndex].ImageName = safeFileName;
                model.Sections[sectionIndex].ShowImage = true;

                SaveDocumentInSession(model);

                TempData["InfoMessage"] = "Imagen agregada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = ex.Message;
            }

            return RedirectToAction("DocumentReport");
        }

        /// <summary>
        /// Elimina la imagen asociada a una sección.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveDescriptionImage(int sectionIndex)
        {
            try
            {
                var model = GetDocumentFromSession();

                if (model == null)
                {
                    return RedirectToAction("CreateDocument");
                }

                if (model.Sections == null || !model.Sections.Any())
                {
                    return RedirectToAction("DocumentReport");
                }

                if (sectionIndex < 0 || sectionIndex >= model.Sections.Count)
                {
                    return RedirectToAction("DocumentReport");
                }

                var section = model.Sections[sectionIndex];

                if (!string.IsNullOrWhiteSpace(section.ImagePath))
                {
                    string relativePath = section.ImagePath.Replace(Url.Content("~/"), string.Empty).Replace("/", "\\");
                    string physicalPath = Path.Combine(Server.MapPath("~/"), relativePath);

                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                section.ImagePath = null;
                section.ImageName = null;
                section.ShowImage = false;

                SaveDocumentInSession(model);

                TempData["InfoMessage"] = "Imagen eliminada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = ex.Message;
            }

            return RedirectToAction("DocumentReport");
        }

        /// <summary>
        /// Limpia el documento actual de Session.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetDocument()
        {
            Session.Remove(SessionKeyDocument);
            return RedirectToAction("CreateDocument");
        }

        #region Métodos privados

        private void SaveDocumentInSession(DocumentViewModel model)
        {
            Session[SessionKeyDocument] = model;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddSection()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            if (model.Sections == null)
            {
                model.Sections = new List<DocumentSectionViewModel>();
            }

            int nextId = model.Sections.Any() ? model.Sections.Max(x => x.Id) + 1 : 1;
            int nextOrder = model.Sections.Any() ? model.Sections.Max(x => x.Order) + 1 : 1;

            model.Sections.Add(new DocumentSectionViewModel
            {
                Id = nextId,
                Order = nextOrder,
                ShowSectionTitle = true,
                ShowDescription = true,
                ShowImage = true,
                SectionTitle = string.Empty,
                Description = string.Empty,
                ImagePath = string.Empty,
                ImageName = string.Empty
            });

            SaveDocumentInSession(model);

            return RedirectToAction("CreateDocument");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItem()
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            if (model.Items == null)
            {
                model.Items = new List<DocumentItemViewModel>();
            }

            int nextId = model.Items.Any() ? model.Items.Max(x => x.Id) + 1 : 1;
            int nextOrder = model.Items.Any() ? model.Items.Max(x => x.Order) + 1 : 1;

            model.Items.Add(new DocumentItemViewModel
            {
                Id = nextId,
                Order = nextOrder,
                Concept = string.Empty,
                Description = string.Empty,
                Quantity = 0,
                UnitPrice = 0
            });

            SaveDocumentInSession(model);

            return RedirectToAction("CreateDocument");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveSection(int id)
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            if (model.Sections != null)
            {
                var sectionToRemove = model.Sections.FirstOrDefault(x => x.Id == id);

                if (sectionToRemove != null)
                {
                    model.Sections.Remove(sectionToRemove);
                }
            }

            SaveDocumentInSession(model);

            return RedirectToAction("CreateDocument");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int id)
        {
            var model = GetDocumentFromSession();

            if (model == null)
            {
                return RedirectToAction("CreateDocument");
            }

            if (model.Items != null)
            {
                var itemToRemove = model.Items.FirstOrDefault(x => x.Id == id);

                if (itemToRemove != null)
                {
                    model.Items.Remove(itemToRemove);
                }
            }

            SaveDocumentInSession(model);

            return RedirectToAction("CreateDocument");
        }
        private DocumentViewModel GetDocumentFromSession()
        {
            return Session[SessionKeyDocument] as DocumentViewModel;
        }

        private void NormalizeDocument(DocumentViewModel model)
        {
            if (model.Sections == null)
            {
                model.Sections = new List<DocumentSectionViewModel>();
            }

            if (model.Items == null)
            {
                model.Items = new List<DocumentItemViewModel>();
            }

            if (model.Signatures == null)
            {
                model.Signatures = new List<DocumentSignatureViewModel>();
            }

            model.Sections = model.Sections
                .Where(x => x != null)
                .OrderBy(x => x.Order)
                .ToList();

            model.Items = model.Items
                .Where(x => x != null)
                .OrderBy(x => x.Order)
                .ToList();

            model.Signatures = model.Signatures
                .Where(x => x != null)
                .OrderBy(x => x.Order)
                .ToList();

            model.Folio = model.Folio != null ? model.Folio.Trim() : string.Empty;
            model.Title = model.Title != null ? model.Title.Trim() : string.Empty;
            model.Subtitle = model.Subtitle != null ? model.Subtitle.Trim() : string.Empty;
            model.GeneralDescription = model.GeneralDescription != null ? model.GeneralDescription.Trim() : string.Empty;
            model.Observations = model.Observations != null ? model.Observations.Trim() : string.Empty;
            model.Notes = model.Notes != null ? model.Notes.Trim() : string.Empty;
            model.ClientName = model.ClientName != null ? model.ClientName.Trim() : string.Empty;
            model.ResponsibleName = model.ResponsibleName != null ? model.ResponsibleName.Trim() : string.Empty;
            model.ResponsiblePosition = model.ResponsiblePosition != null ? model.ResponsiblePosition.Trim() : string.Empty;
            model.IssueLocation = model.IssueLocation != null ? model.IssueLocation.Trim() : string.Empty;

            foreach (var section in model.Sections)
            {
                section.SectionTitle = section.SectionTitle != null ? section.SectionTitle.Trim() : string.Empty;
                section.Description = section.Description != null ? section.Description.Trim() : string.Empty;
                section.ImagePath = section.ImagePath != null ? section.ImagePath.Trim() : string.Empty;
                section.ImageName = section.ImageName != null ? section.ImageName.Trim() : string.Empty;
            }

            foreach (var item in model.Items)
            {
                item.Concept = item.Concept != null ? item.Concept.Trim() : string.Empty;
                item.Description = item.Description != null ? item.Description.Trim() : string.Empty;
            }

            foreach (var signature in model.Signatures)
            {
                signature.Name = signature.Name != null ? signature.Name.Trim() : string.Empty;
                signature.Position = signature.Position != null ? signature.Position.Trim() : string.Empty;
                signature.SignaturePath = signature.SignaturePath != null ? signature.SignaturePath.Trim() : string.Empty;
            }
        }

        private void CalculateTotals(DocumentViewModel model)
        {
            if (model == null)
            {
                return;
            }

            if (model.Items == null || !model.Items.Any())
            {
                model.Subtotal = 0;
                model.Vat = 0;
                model.Total = 0;
                return;
            }

            decimal subtotal = model.Items.Sum(x => x.Amount);
            decimal vat = subtotal * 0.16m;
            decimal total = subtotal + vat;

            model.Subtotal = subtotal;
            model.Vat = vat;
            model.Total = total;
        }

        private string BuildPdfFileName(DocumentViewModel model)
        {
            string documentType = model.DocumentType.ToString();
            string folio = string.IsNullOrWhiteSpace(model.Folio) ? "SinFolio" : model.Folio.Trim();

            return string.Format("{0}_{1}.pdf", documentType, folio);
        }

        private void LoadDocumentTypes(DocumentTypeEnum? selectedValue = null)
        {
            var documentTypes = Enum.GetValues(typeof(DocumentTypeEnum))
                .Cast<DocumentTypeEnum>()
                .Select(x => new SelectListItem
                {
                    Text = GetDocumentTypeText(x),
                    Value = ((int)x).ToString(),
                    Selected = selectedValue.HasValue && x == selectedValue.Value
                })
                .ToList();

            ViewBag.DocumentTypes = documentTypes;
        }

        private string GetDocumentTypeText(DocumentTypeEnum documentType)
        {
            switch (documentType)
            {
                case DocumentTypeEnum.Cotizacion:
                    return "Cotización";
                case DocumentTypeEnum.ReporteServicio:
                    return "Reporte de Servicio";
                case DocumentTypeEnum.Requerimiento:
                    return "Requerimiento";
                case DocumentTypeEnum.DocumentoGeneral:
                    return "Documento General";
                default:
                    return documentType.ToString();
            }
        }

        #endregion
    }
}