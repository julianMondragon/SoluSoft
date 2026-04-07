using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel.ManageDC3;

namespace TAS360.Controllers.ManagerDC3
{
    public class TrabajadorController : Controller
    {
        private readonly HelpDesk_Entities1 _context;

        public TrabajadorController()
        {
            _context = new HelpDesk_Entities1();
        }

        /// <summary>
        /// Muestra el listado de trabajadores activos del certificador logueado.
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 53)]
        public ActionResult Index()
        {
            int userId = GetUserId();

            var trabajadores = _context.Trabajador
                .Include("Empresa")
                .Include("Ocupacion")
                .Where(t => t.Activo == true && t.Empresa.CertificadorId == userId)
                .OrderBy(t => t.Nombre)
                .ToList();

            return View(trabajadores);
        }

        /// <summary>
        /// GET: Muestra formulario para crear trabajador.
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 54)]
        public ActionResult Create()
        {
            var model = new TrabajadorViewModel
            {
                Empresas = GetEmpresas(),
                Ocupaciones = GetOcupaciones(),
                Activo = true
            };

            return View(model);
        }

        /// <summary>
        /// POST: Registra un trabajador con validaciones, seguridad multiusuario y logs.
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 54)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TrabajadorViewModel model)
        {
            string path = Server.MapPath("~/Logs/Trabajador/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();

                if (!ModelState.IsValid)
                {
                    model.Empresas = GetEmpresas();
                    model.Ocupaciones = GetOcupaciones();
                    oLog.Add("Modelo inválido al crear trabajador");
                    return View(model);
                }
                model.CURP = model.CURP?.ToUpper().Trim();
                model.CURP = (model.CURP ?? string.Empty).Trim().ToUpper();

                // Seguridad: valida que la empresa seleccionada pertenezca al certificador logueado.
                var empresa = _context.Empresa.FirstOrDefault(e => e.Id == model.EmpresaId && e.Activo == true && e.CertificadorId == userId);
                if (empresa == null)
                {
                    oLog.Add("Intento de crear trabajador con empresa no permitida. EmpresaId: " + model.EmpresaId);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Validación de CURP única por certificador y activa.
                bool curpDuplicada = _context.Trabajador
                    .Any(t => t.CURP == model.CURP && t.Activo == true && t.Empresa.CertificadorId == userId);

                if (curpDuplicada)
                {
                    ModelState.AddModelError("CURP", "Ya existe un trabajador activo con esta CURP.");
                    model.Empresas = GetEmpresas();
                    model.Ocupaciones = GetOcupaciones();
                    oLog.Add("CURP duplicada al crear trabajador: " + model.CURP);
                    return View(model);
                }

                oLog.Add("==== CREAR TRABAJADOR ====");
                oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                oLog.Add("Nombre: " + model.Nombre);
                oLog.Add("CURP: " + model.CURP);
                oLog.Add("EmpresaId: " + model.EmpresaId);
                oLog.Add("OcupacionId: " + model.OcupacionId);

                var entity = new Trabajador
                {
                    Nombre = model.Nombre,
                    CURP = model.CURP,
                    Puesto = model.Puesto,
                    EmpresaId = model.EmpresaId,
                    OcupacionId = model.OcupacionId,
                    Activo = true
                };

                _context.Trabajador.Add(entity);
                _context.SaveChanges();

                oLog.Add("Trabajador creado con ID: " + entity.Id);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR CREATE: " + ex.Message);
                model.Empresas = GetEmpresas();
                model.Ocupaciones = GetOcupaciones();
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        /// <summary>
        /// GET: Muestra formulario para editar trabajador.
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 55)]
        public ActionResult Edit(int id)
        {
            int userId = GetUserId();

            var trabajador = _context.Trabajador
                .Include("Empresa")
                .FirstOrDefault(t => t.Id == id);

            if (trabajador == null || trabajador.Activo != true || trabajador.Empresa.CertificadorId != userId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new TrabajadorViewModel
            {
                Id = trabajador.Id,
                Nombre = trabajador.Nombre,
                CURP = trabajador.CURP,
                Puesto = trabajador.Puesto,
                EmpresaId = trabajador.EmpresaId,
                OcupacionId = trabajador.OcupacionId ?? 0,
                Activo = trabajador.Activo ?? true,
                Empresas = GetEmpresas(),
                Ocupaciones = GetOcupaciones()
            };

            return View(model);
        }

        /// <summary>
        /// POST: Actualiza trabajador con validaciones, seguridad multiusuario y logs.
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 55)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TrabajadorViewModel model)
        {
            string path = Server.MapPath("~/Logs/Trabajador/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();
                model.CURP = model.CURP?.ToUpper().Trim();
                if (!ModelState.IsValid)
                {
                    model.Empresas = GetEmpresas();
                    model.Ocupaciones = GetOcupaciones();
                    oLog.Add("Modelo inválido al editar trabajador. ID: " + model.Id);
                    return View(model);
                }                
                model.CURP = (model.CURP ?? string.Empty).Trim().ToUpper();

                var entity = _context.Trabajador
                    .Include("Empresa")
                    .FirstOrDefault(t => t.Id == model.Id);

                if (entity == null || entity.Activo != true || entity.Empresa.CertificadorId != userId)
                {
                    oLog.Add("Intento no autorizado de edición. ID: " + model.Id);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                var empresa = _context.Empresa.FirstOrDefault(e => e.Id == model.EmpresaId && e.Activo == true && e.CertificadorId == userId);
                if (empresa == null)
                {
                    oLog.Add("Intento de edición con empresa no permitida. EmpresaId: " + model.EmpresaId);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                bool curpDuplicada = _context.Trabajador
                    .Any(t => t.Id != model.Id && t.CURP == model.CURP && t.Activo == true && t.Empresa.CertificadorId == userId);

                if (curpDuplicada)
                {
                    ModelState.AddModelError("CURP", "Ya existe otro trabajador activo con esta CURP.");
                    model.Empresas = GetEmpresas();
                    model.Ocupaciones = GetOcupaciones();
                    oLog.Add("CURP duplicada al editar trabajador. ID: " + model.Id + ", CURP: " + model.CURP);
                    return View(model);
                }

                oLog.Add("==== EDITAR TRABAJADOR ====");
                oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                oLog.Add("ID: " + entity.Id);
                oLog.Add("Nombre anterior: " + entity.Nombre + " | Nuevo nombre: " + model.Nombre);
                oLog.Add("CURP anterior: " + entity.CURP + " | Nueva CURP: " + model.CURP);
                oLog.Add("Puesto anterior: " + entity.Puesto + " | Nuevo puesto: " + model.Puesto);
                oLog.Add("Empresa anterior: " + entity.EmpresaId + " | Nueva empresa: " + model.EmpresaId);
                oLog.Add("Ocupación anterior: " + (entity.OcupacionId.HasValue ? entity.OcupacionId.Value.ToString() : "N/A") + " | Nueva ocupación: " + model.OcupacionId);

                entity.Nombre = model.Nombre;
                entity.CURP = model.CURP;
                entity.Puesto = model.Puesto;
                entity.EmpresaId = model.EmpresaId;
                entity.OcupacionId = model.OcupacionId;

                _context.SaveChanges();

                oLog.Add("Trabajador actualizado correctamente");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                oLog.Add("ERROR EDIT: " + ex.Message);
                model.Empresas = GetEmpresas();
                model.Ocupaciones = GetOcupaciones();
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        /// <summary>
        /// POST: Elimina lógicamente un trabajador (Activo = false).
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 56)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string path = Server.MapPath("~/Logs/Trabajador/");
            Log oLog = new Log(path);

            try
            {
                int userId = GetUserId();

                var entity = _context.Trabajador
                    .Include("Empresa")
                    .FirstOrDefault(t => t.Id == id);

                if (entity == null || entity.Activo != true || entity.Empresa.CertificadorId != userId)
                {
                    oLog.Add("Intento no autorizado de eliminación. ID: " + id);
                    oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                oLog.Add("==== ELIMINAR TRABAJADOR ====");
                oLog.Add("Usuario: " + ((User)Session["User"]).nombre);
                oLog.Add("ID: " + entity.Id);
                oLog.Add("Nombre: " + entity.Nombre);
                oLog.Add("CURP: " + entity.CURP);

                entity.Activo = false;

                _context.SaveChanges();

                oLog.Add("Trabajador eliminado lógicamente");

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

        /// <summary>
        /// Obtiene las empresas activas del certificador logueado para dropdown.
        /// </summary>
        private List<SelectListItem> GetEmpresas()
        {
            int userId = GetUserId();

            return _context.Empresa
                .Where(e => e.Activo == true && e.CertificadorId == userId)
                .OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.Nombre + " (" + e.RFC + ")"
                })
                .ToList();
        }

        /// <summary>
        /// Obtiene ocupaciones activas para dropdown.
        /// </summary>
        private List<SelectListItem> GetOcupaciones()
        {
            return _context.Ocupacion
                .Where(o => o.Activo == true)
                .OrderBy(o => o.Clave)
                .ThenBy(o => o.Nombre)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Clave + " - " + o.Nombre
                })
                .ToList();
        }
    }
}