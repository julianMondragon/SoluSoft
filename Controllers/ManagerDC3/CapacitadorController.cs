using System;
using System.Collections.Generic;
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
    public class CapacitadorController : Controller
    {
        private readonly HelpDesk_Entities1 _context;

        public CapacitadorController()
        {
            _context = new HelpDesk_Entities1();
        }

        /// <summary>
        /// Muestra el listado de capacitadores del certificador logueado
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 50)]
        public ActionResult Index()
        {
            int userId = GetUserId();

            var capacitadores = _context.Capacitador
                .Where(c => c.Activo == true && c.CertificadorId == userId)
                .ToList();

            return View(capacitadores);
        }

        /// <summary>
        /// Metodo Get que crea un capacitador 
        /// </summary>
        /// <returns></returns>
        [AuthorizeUser(idOperacion: 49)]
        public ActionResult Create()
        {
            return View();
        }
        /// <summary>
        /// Metodo Post que guarda un capacitador
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeUser(idOperacion: 49)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CapacitadorViewModel model, HttpPostedFileBase FirmaFile)
        {
            string path = Server.MapPath("~/Logs/Capacitador/");
            Log oLog = new Log(path);

            try
            {
                if (ModelState.IsValid)
                {
                    int userId = GetUserId();

                    oLog.Add("==== CREAR CAPACITADOR ====");
                    oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                    oLog.Add("Nombre: " + model.Nombre);
                    oLog.Add("Registro STPS: " + model.NumeroRegistroSTPS);

                    string rutaFirma = null;

                    if (FirmaFile != null && FirmaFile.ContentLength > 0)
                    {
                        string folder = Server.MapPath("~/Uploads/Firmas/");
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string fileName = Guid.NewGuid() + Path.GetExtension(FirmaFile.FileName);
                        string fullPath = Path.Combine(folder, fileName);

                        FirmaFile.SaveAs(fullPath);

                        rutaFirma = "/Uploads/Firmas/" + fileName;

                        oLog.Add("Firma guardada en: " + rutaFirma);
                    }

                    var entity = new Capacitador
                    {
                        Nombre = model.Nombre,
                        NumeroRegistroSTPS = model.NumeroRegistroSTPS,
                        CertificadorId = userId,
                        RutaFirmaBase = rutaFirma,
                        FechaRegistroFirma = DateTime.Now,
                        Activo = true
                    };

                    _context.Capacitador.Add(entity);
                    _context.SaveChanges();

                    oLog.Add("Capacitador creado con ID: " + entity.Id);

                    return RedirectToAction("Index");
                }

                oLog.Add("Modelo inválido al crear capacitador");

                return View(model);
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR CREATE: " + ex.Message);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        /// <summary>
        /// Muestra el formulario para editar un capacitador
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 51)]
        public ActionResult Edit(int id)
        {
            int userId = GetUserId();

            var capacitador = _context.Capacitador.Find(id);

            // 🔒 Validación de seguridad
            if (capacitador == null || capacitador.CertificadorId != userId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new CapacitadorViewModel
            {
                Id = capacitador.Id,
                Nombre = capacitador.Nombre,
                NumeroRegistroSTPS = capacitador.NumeroRegistroSTPS,
                RutaFirmaBase = capacitador.RutaFirmaBase
            };

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 51)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CapacitadorViewModel model, HttpPostedFileBase FirmaFile)
        {
            string path = Server.MapPath("~/Logs/Capacitador/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();

                var entity = _context.Capacitador.Find(model.Id);

                if (entity == null || entity.CertificadorId != userId)
                {
                    oLog.Add("Intento no autorizado de edición. ID: " + model.Id);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                oLog.Add("==== EDITAR CAPACITADOR ====");
                oLog.Add("ID: " + entity.Id);
                oLog.Add("Usuario: " + ((User)Session["User"]).nombre);

                oLog.Add("Nombre anterior: " + entity.Nombre);
                oLog.Add("Nuevo nombre: " + model.Nombre);

                oLog.Add("Registro anterior: " + entity.NumeroRegistroSTPS);
                oLog.Add("Nuevo registro: " + model.NumeroRegistroSTPS);

                entity.Nombre = model.Nombre;
                entity.NumeroRegistroSTPS = model.NumeroRegistroSTPS;

                if (FirmaFile != null && FirmaFile.ContentLength > 0)
                {
                    string folder = Server.MapPath("~/Uploads/Firmas/");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(FirmaFile.FileName);
                    string fullPath = Path.Combine(folder, fileName);

                    FirmaFile.SaveAs(fullPath);

                    entity.RutaFirmaBase = "/Uploads/Firmas/" + fileName;
                    entity.FechaRegistroFirma = DateTime.Now;

                    oLog.Add("Firma actualizada: " + entity.RutaFirmaBase);
                }

                _context.SaveChanges();

                oLog.Add("Capacitador actualizado correctamente");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR EDIT: " + ex.Message);
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        /// <summary>
        /// (POST) Metodo que elimina lógicamente un capacitador 
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 52)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string path = Server.MapPath("~/Logs/Capacitador/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();

                var entity = _context.Capacitador.Find(id);

                if (entity == null || entity.CertificadorId != userId)
                {
                    oLog.Add("Intento no autorizado de eliminación. ID: " + id);
                    oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                oLog.Add("==== ELIMINAR CAPACITADOR ====");
                oLog.Add("ID: " + entity.Id);
                oLog.Add("Nombre: " + entity.Nombre);
                oLog.Add("Usuario: " + ((User)Session["User"]).nombre);

                entity.Activo = false;

                _context.SaveChanges();

                oLog.Add("Capacitador eliminado lógicamente");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR DELETE: " + ex.Message);
                return RedirectToAction("Index");
            }
        }

        // =============================
        // HELPERS
        // =============================
        private int GetUserId()
        {
            return ((User)Session["User"]).id;
        }
    }
}