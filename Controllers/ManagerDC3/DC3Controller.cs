using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using QRCoder;
using Rotativa;
using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel.ManageDC3;

namespace TAS360.Controllers.ManagerDC3
{
    public class DC3Controller : Controller
    {
        private readonly HelpDesk_Entities1 _context;

        public DC3Controller()
        {
            _context = new HelpDesk_Entities1();
        }

        [HttpGet]
        [AuthorizeUser(idOperacion: 57)]
        public ActionResult Index()
        {
            int userId = GetUserId();

            var data = _context.DC3
                .Include(x => x.Empresa)
                .Include(x => x.Trabajador)
                .Include(x => x.Curso)
                .Include(x => x.Capacitador)
                .Where(x => x.CertificadorId == userId && x.Estatus != "Eliminado")
                .OrderByDescending(x => x.FechaCreacion)
                .ToList();

            return View(data.Select(MapToViewModel).ToList());
        }

        [HttpGet]
        [AuthorizeUser(idOperacion: 58)]
        public ActionResult Create()
        {
            var model = new DC3ViewModel
            {
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today,
                FechaEmision = DateTime.Today,
                Activo = true,
                Empresas = GetEmpresas(),
                Trabajadores = GetTrabajadores(),
                Cursos = GetCursos(),
                Capacitadores = GetCapacitadores(),
                Ocupaciones = GetOcupaciones()
            };

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 58)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DC3ViewModel model)
        {
            string path = Server.MapPath("~/Logs/DC3/");
            Log oLog = new Log(path);

            try
            {
                if (!ModelState.IsValid)
                {
                    LoadCatalogs(model);
                    return View(model);
                }

                int userId = GetUserId();
                if (!IsValidOwnership(model, userId))
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                var entity = new DC3
                {
                    Folio = GenerateFolio(userId),
                    EmpresaId = model.EmpresaId,
                    TrabajadorId = model.TrabajadorId,
                    CursoId = model.CursoId,
                    CapacitadorId = model.CapacitadorId,
                    FechaInicio = model.FechaInicio,
                    FechaFin = model.FechaFin,
                    CertificadorId = userId,
                    FechaCreacion = DateTime.Now,
                    Estatus = "Borrador"
                };

                _context.DC3.Add(entity);
                _context.SaveChanges();

                SaveOrUpdateSignature(entity.Id, "Capacitador", GetCapacitadorSignature(model.CapacitadorId), ((User)Session["User"]).nombre);
                _context.SaveChanges();

                oLog.Add("DC3 creado ID: " + entity.Id + " Por usuario: " + ((User)Session["User"]).nombre + " id: " + ((User)Session["User"]).id);
                return RedirectToAction("Index");
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityErrors.ValidationErrors)
                    {
                        oLog.Add($"ERROR VALIDATION: {validationError.PropertyName} - {validationError.ErrorMessage}");
                    }
                }

                ViewBag.ExceptionMessage = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ExceptionMessage = ex.Message;
                oLog.Add("ERROR CREATE: " + ex.Message);
                LoadCatalogs(model);
                return View(model);
            }
        }

        [HttpGet]
        [AuthorizeUser(idOperacion: 59)]
        public ActionResult Edit(int id)
        {
            
            int userId = GetUserId();
            var entity = GetDc3ById(id, userId);
            if (entity == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (entity.Estatus == "Firmado")
            {
                ViewBag.InfoMessage = "No puedes editar un DC3 Firmado";
                return RedirectToAction("Index"); ;
            }
            var model = MapToViewModel(entity);
            LoadCatalogs(model);
            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 59)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(DC3ViewModel model)
        {
            string path = Server.MapPath("~/Logs/DC3/");
            Log oLog = new Log(path);

            try
            {
                if (!ModelState.IsValid)
                {
                    LoadCatalogs(model);
                    return View(model);
                }

                int userId = GetUserId();
                var entity = GetDc3ById(model.Id, userId);
                if (entity == null || !IsValidOwnership(model, userId))
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                entity.EmpresaId = model.EmpresaId;
                entity.TrabajadorId = model.TrabajadorId;
                entity.CursoId = model.CursoId;
                entity.CapacitadorId = model.CapacitadorId;
                entity.FechaInicio = model.FechaInicio;
                entity.FechaFin = model.FechaFin;

                SaveOrUpdateSignature(entity.Id, "Capacitador", GetCapacitadorSignature(model.CapacitadorId), ((User)Session["User"]).nombre);

                _context.SaveChanges();

                oLog.Add("DC3 editado ID: " + entity.Id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR EDIT: " + ex.Message);
                LoadCatalogs(model);
                ViewBag.ExceptionMessage = ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        [AuthorizeUser(idOperacion: 61)]
        public ActionResult Details(int id)
        {
            int userId = GetUserId();
            var entity = GetDc3ById(id, userId);
            if (entity == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(MapToViewModel(entity));
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 60)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string path = Server.MapPath("~/Logs/DC3/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();
                var entity = GetDc3ById(id, userId);
                if (entity == null)
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                entity.Estatus = "Eliminado";
                _context.SaveChanges();

                oLog.Add("DC3 eliminado ID: " + id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR DELETE: " + ex.Message);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [AuthorizeUser(idOperacion: 62)]
        public ActionResult Firmar(int id)
        {
            int userId = GetUserId();
            var entity = GetDc3ById(id, userId);
            if (entity == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            return View(MapToViewModel(entity));
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 62)]
        [ValidateAntiForgeryToken]
        public ActionResult Firmar(int id, HttpPostedFileBase FirmaRepresentanteFile, HttpPostedFileBase FirmaTrabajadorFile)
        {
            string path = Server.MapPath("~/Logs/DC3/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();
                var entity = GetDc3ById(id, userId);
                if (entity == null)
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                string rutaCap = GetCapacitadorSignature(entity.CapacitadorId);
                SaveOrUpdateSignature(entity.Id, "Capacitador", rutaCap, ((User)Session["User"]).nombre);

                string folder = Server.MapPath("~/Content/Firmas/DC3/");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (FirmaRepresentanteFile != null && FirmaRepresentanteFile.ContentLength > 0)
                {
                    string rutaRep = SaveSignatureFile(FirmaRepresentanteFile, folder, "/Content/Firmas/DC3/");
                    SaveOrUpdateSignature(entity.Id, "Representante", rutaRep, ((User)Session["User"]).nombre);
                }

                if (FirmaTrabajadorFile != null && FirmaTrabajadorFile.ContentLength > 0)
                {
                    string rutaTrab = SaveSignatureFile(FirmaTrabajadorFile, folder, "/Content/Firmas/DC3/");
                    SaveOrUpdateSignature(entity.Id, "Trabajador", rutaTrab, ((User)Session["User"]).nombre);
                }
                entity.Estatus = "Firmado";
                _context.SaveChanges();

                oLog.Add("DC3 firmado ID: " + id);
                return RedirectToAction("Details", new { id });
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityErrors.ValidationErrors)
                    {
                        oLog.Add($"ERROR VALIDATION: {validationError.PropertyName} - {validationError.ErrorMessage}");
                    }
                }

                ViewBag.ExceptionMessage = ex.Message;
                return RedirectToAction("Firmar", new { id });
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR FIRMAR: " + ex.Message);
                return RedirectToAction("Firmar", new { id });
            }
        }


        /// <summary>
        /// Metodo que se encarga de descargar el archivo en pdf que fue generado
        /// </summary>
        /// <param name="path"></param>
        /// <param name="NameFile"></param>
        /// <returns></returns>
        public FileResult DownloadQRDC3(string path, string FileName)
        {
            string rute = Server.MapPath("~" + path);
            return File(rute, "application/png", FileName + ".png");
        }

        /// <summary>
        /// Metodo que se encarga de descargar el archivo en pdf que fue generado
        /// </summary>
        /// <param name="path"></param>
        /// <param name="NameFile"></param>
        /// <returns></returns>
        public FileResult DownloadFileDC3(string path , string FileName)
        {
            string rute = Server.MapPath("~" + path);
            return File(rute, "application/pdf", FileName + ".pdf");
        }

        [AuthorizeUser(idOperacion: 63)]
        public ActionResult GenerarQR(int id)
        {
            string path = Server.MapPath("~/Logs/DC3/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();
                var entity = GetDc3ById(id, userId);

                if (entity == null)
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

                // ============================
                // 📄 1. GENERAR PDF
                // ============================

                string pdfFolder = Server.MapPath("~/Content/DC3/PDF/");
                if (!Directory.Exists(pdfFolder))
                    Directory.CreateDirectory(pdfFolder);

                string pdfFileName = $"dc3_{id}_{DateTime.Now.Ticks}.pdf";
                string pdfFullPath = Path.Combine(pdfFolder, pdfFileName);

                var pdfBytes = new ActionAsPdf("DC3Report", new { id, userId })
                    .BuildFile(ControllerContext);

                System.IO.File.WriteAllBytes(pdfFullPath, pdfBytes);

                string pdfVirtualPath = "/Content/DC3/PDF/" + pdfFileName;

                // ============================
                // 🔗 2. GENERAR URL PUBLICA
                // ============================

                string pdfUrl = Request.Url.GetLeftPart(UriPartial.Authority) + pdfVirtualPath;

                // ============================
                // 🧾 3. GENERAR QR
                // ============================

                string qrFolder = Server.MapPath("~/Content/DC3/QR/");
                if (!Directory.Exists(qrFolder))
                    Directory.CreateDirectory(qrFolder);

                string qrFileName = $"qr_{id}_{DateTime.Now.Ticks}.png";
                string qrFullPath = Path.Combine(qrFolder, qrFileName);

                GenerateQr(pdfUrl, qrFullPath);

                string qrVirtualPath = "/Content/DC3/QR/" + qrFileName;

                // ============================
                // 💾 4. GUARDAR EN BD
                // ============================

                var doc = GetOrCreateDocumento(entity.Id);

                doc.RutaPDF = pdfVirtualPath;
                doc.RutaQR = qrVirtualPath;
                doc.UrlPublica = pdfUrl;
                doc.FechaGeneracion = DateTime.Now;

                // ============================
                // 🔄 5. ACTUALIZAR ESTATUS
                // ============================

                entity.Estatus = "Generado";

                _context.SaveChanges();

                oLog.Add($"Documento completo generado (PDF + QR) para DC3 ID: {id}");

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR GENERAR DOCUMENTO: " + ex.Message);
                return RedirectToAction("Index");
            }
        }
        private void GenerateQr(string content, string outputPath)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
            {
                qrCodeImage.Save(outputPath, ImageFormat.Png);
            }
        }

        //[AuthorizeUser(idOperacion: 61)]
        public ActionResult DC3Report(int id , int userId)
        {
            
            var entity = GetDc3ById(id, userId);

            if (entity == null)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = MapToViewModel(entity);

            return View(model); // Vista limpia SOLO para PDF
        }

        public ActionResult PrintDC3(int id, int userId)
        {
            return new ActionAsPdf("DC3Report", new { id, userId })
            {
                FileName = $"Reporte_DC3_{id}.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Portrait
            };
        }
        //public ActionResult PrintDC3(int id , int userId)
        //{
            
        //    var entity = GetDc3ById(id, userId);
        //    if (entity == null)
        //        return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

        //    var pdfBytes = new ActionAsPdf("DC3Report", new { id })
        //        .BuildFile(ControllerContext);

        //    return File(pdfBytes, "application/pdf", $"DC3_{id}.pdf");
        //}

        [AuthorizeUser(idOperacion: 64)]
        public ActionResult GenerarPdf(int id)
        {
            int userId = GetUserId();
            var entity = GetDc3ById(id, userId);

            string folder = Server.MapPath("~/Content/DC3/PDF/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = $"dc3_{id}_{DateTime.Now.Ticks}.pdf";
            string filePath = Path.Combine(folder, fileName);

            var pdfBytes = new ActionAsPdf("DC3Report", new { id })
                .BuildFile(ControllerContext);

            System.IO.File.WriteAllBytes(filePath, pdfBytes);

            var doc = GetOrCreateDocumento(entity.Id);
            doc.RutaPDF = "/Content/DC3/PDF/" + fileName;
            doc.FechaGeneracion = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Validar(int id)
        {
            var entity = _context.DC3
                .Include(x => x.Empresa)
                .Include(x => x.Trabajador)
                .Include(x => x.Curso)
                .Include(x => x.Capacitador)
                .FirstOrDefault(x => x.Id == id && x.Estatus != "Eliminado");

            if (entity == null)
                return HttpNotFound("El certificado no existe o no está activo.");

            return View(MapToViewModel(entity));
        }

        private DC3 GetDc3ById(int id, int userId)
        {
            return _context.DC3
                .Include(x => x.Empresa)
                .Include(x => x.Trabajador)
                .Include(x => x.Curso)
                .Include(x => x.Capacitador)
                .Include(x => x.DC3Firma)
                .Include(x => x.DC3Documento)
                .FirstOrDefault(x => x.Id == id && x.CertificadorId == userId && x.Estatus != "Eliminado");
        }

        private DC3Documento GetOrCreateDocumento(int dc3Id)
        {
            var doc = _context.DC3Documento.FirstOrDefault(x => x.DC3Id == dc3Id);
            if (doc != null)
                return doc;

            doc = new DC3Documento
            {
                DC3Id = dc3Id,
                FechaGeneracion = DateTime.Now
            };
            _context.DC3Documento.Add(doc);
            return doc;
        }

        private void SaveOrUpdateSignature(int dc3Id, string tipoFirma, string ruta, string nombre)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            var firma = _context.DC3Firma.FirstOrDefault(x => x.DC3Id == dc3Id && x.TipoFirma == tipoFirma);
            if (firma == null)
            {
                firma = new DC3Firma
                {
                    DC3Id = dc3Id,
                    TipoFirma = tipoFirma,
                    RutaArchivo = ruta,
                    FechaFirma = DateTime.Now,
                    NombreFirmante = nombre
                };
                _context.DC3Firma.Add(firma);
            }
            else
            {
                firma.RutaArchivo = ruta;
                firma.FechaFirma = DateTime.Now;
            }
        }

        private string SaveSignatureFile(HttpPostedFileBase file, string physicalFolder, string virtualFolder)
        {
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(physicalFolder, fileName);
            file.SaveAs(fullPath);
            return virtualFolder + fileName;
        }

        private string GetCapacitadorSignature(int capacitadorId)
        {
            return _context.Capacitador
                .Where(c => c.Id == capacitadorId)
                .Select(c => c.RutaFirmaBase)
                .FirstOrDefault();
        }

        private string GenerateFolio(int userId)
        {
            int count = _context.DC3.Count(x => x.CertificadorId == userId) + 1;
            return "DC3-" + userId + "-" + count.ToString("D5");
        }

        private bool IsValidOwnership(DC3ViewModel model, int userId)
        {
            bool empresaOk = _context.Empresa.Any(x => x.Id == model.EmpresaId && x.CertificadorId == userId && x.Activo == true);
            bool trabajadorOk = _context.Trabajador.Any(x => x.Id == model.TrabajadorId && x.Activo == true && x.Empresa.CertificadorId == userId);
            bool cursoOk = _context.Curso.Any(x => x.Id == model.CursoId && x.CertificadorId == userId && x.Activo == true);
            bool capacitadorOk = _context.Capacitador.Any(x => x.Id == model.CapacitadorId && x.CertificadorId == userId && x.Activo == true);

            return empresaOk && trabajadorOk && cursoOk && capacitadorOk;
        }

        private DC3ViewModel MapToViewModel(DC3 x)
        {
            var firmaCap = x.DC3Firma.FirstOrDefault(f => f.TipoFirma == "Capacitador");
            var firmaRep = x.DC3Firma.FirstOrDefault(f => f.TipoFirma == "Representante");
            var firmaTrab = x.DC3Firma.FirstOrDefault(f => f.TipoFirma == "Trabajador");
            var doc = x.DC3Documento.FirstOrDefault();

            return new DC3ViewModel
            {
                Id = x.Id,
                CertificadorId = x.CertificadorId,
                Folio = x.Folio,
                EmpresaId = x.EmpresaId,
                TrabajadorId = x.TrabajadorId,
                CursoId = x.CursoId,
                CursoTematica = x.Curso.AreaTematica != null ? x.Curso.AreaTematica.Nombre : "",
                CapacitadorId = x.CapacitadorId,
                FechaInicio = x.FechaInicio,
                FechaFin = x.FechaFin,
                Status = x.Estatus,
                DuracionHoras = x.Curso != null ? x.Curso.DuracionHoras : 0,
                Puesto = x.Trabajador != null ? x.Trabajador.Puesto : string.Empty,
                OcupacionId = x.Trabajador != null && x.Trabajador.Ocupacion != null ? x.Trabajador.Ocupacion.Id : 0,
                OcupacionNombre = x.Trabajador != null && x.Trabajador.Ocupacion != null ? x.Trabajador.Ocupacion.Nombre : "",
                RepresentanteTrabajadores = firmaRep != null ? firmaRep.NombreFirmante : null,
                FechaEmision = x.FechaCreacion ?? DateTime.Now,
                RutaFirmaCapacitador = firmaCap != null ? firmaCap.RutaArchivo : null,
                RutaFirmaRepresentante = firmaRep != null ? firmaRep.RutaArchivo : null,
                RutaFirmaTrabajador = firmaTrab != null ? firmaTrab.RutaArchivo : null,
                QRUrl = doc != null ? doc.RutaQR : null,
                PdfUrl = doc != null ? doc.RutaPDF : null,
                Activo = x.Estatus != "Eliminado",
                EmpresaNombre = x.Empresa != null ? x.Empresa.Nombre : string.Empty,
                TrabajadorNombre = x.Trabajador != null ? x.Trabajador.Nombre : string.Empty,
                CursoNombre = x.Curso != null ? x.Curso.Nombre : string.Empty,
                CapacitadorNombre = x.Capacitador != null ? x.Capacitador.Nombre : string.Empty,
                CapacitadorSTPS = x.Capacitador != null ? x.Capacitador.NumeroRegistroSTPS : string.Empty,
                CURP = x.Trabajador != null ? x.Trabajador.CURP : "",
                RFC = x.Empresa != null ? x.Empresa.RFC : "",
                CURPArray = !string.IsNullOrEmpty(x.Trabajador.CURP)
                ? x.Trabajador.CURP.ToCharArray()
                : new char[18],
                            RFCArray = !string.IsNullOrEmpty(x.Empresa.RFC)
                ? x.Empresa.RFC.ToCharArray()
                : new char[13],
                FechaInicioArray = new[]
                {
                    x.FechaInicio.Year.ToString(),
                    x.FechaInicio.Month.ToString("D2"),
                    x.FechaInicio.Day.ToString("D2")
                },
                FechaFinArray = new[]
                {
                    x.FechaFin.Year.ToString(),
                    x.FechaFin.Month.ToString("D2"),
                    x.FechaFin.Day.ToString("D2")
                }
            };
        }

        private void LoadCatalogs(DC3ViewModel model)
        {
            model.Empresas = GetEmpresas();
            model.Trabajadores = GetTrabajadores();
            model.Cursos = GetCursos();
            model.Capacitadores = GetCapacitadores();
            model.Ocupaciones = GetOcupaciones();
        }

        private SelectList GetEmpresas()
        {
            int userId = GetUserId();
            return new SelectList(_context.Empresa.Where(x => x.CertificadorId == userId && x.Activo == true).OrderBy(x => x.Nombre).ToList(), "Id", "Nombre");
        }

        private SelectList GetTrabajadores()
        {
            int userId = GetUserId();
            return new SelectList(_context.Trabajador.Where(x => x.Activo == true && x.Empresa.CertificadorId == userId).OrderBy(x => x.Nombre).ToList(), "Id", "Nombre");
        }

        private SelectList GetCursos()
        {
            int userId = GetUserId();
            return new SelectList(_context.Curso.Where(x => x.CertificadorId == userId && x.Activo == true).OrderBy(x => x.Nombre).ToList(), "Id", "Nombre");
        }

        private SelectList GetOcupaciones()
        {
             return new SelectList(_context.Ocupacion.Where(x => x.Activo == true).OrderBy(x => x.Nombre).ToList(), "Id", "Nombre");
        }

        private SelectList GetCapacitadores()
        {
            int userId = GetUserId();
            return new SelectList(_context.Capacitador.Where(x => x.CertificadorId == userId && x.Activo == true).OrderBy(x => x.Nombre).ToList(), "Id", "Nombre");
        }

        private int GetUserId()
        {
            return ((User)Session["User"]).id;
        }

        private void GeneratePseudoQr(string content, string outputPath)
        {
            using (var bitmap = new Bitmap(350, 350))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.White);
                using (var pen = new Pen(System.Drawing.Color.Black, 2))
                {
                    g.DrawRectangle(pen, 5, 5, 340, 340);
                    g.DrawRectangle(pen, 25, 25, 65, 65);
                    g.DrawRectangle(pen, 260, 25, 65, 65);
                    g.DrawRectangle(pen, 25, 260, 65, 65);
                }

                using (var font = new System.Drawing.Font("Arial", 8))
                using (var brush = new SolidBrush(System.Drawing.Color.Black))
                {
                    g.DrawString("QR", new System.Drawing.Font("Arial", 24, FontStyle.Bold), brush, new PointF(145, 145));
                    g.DrawString(content, font, brush, new RectangleF(20, 305, 310, 40));
                }

                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }
    }
}
