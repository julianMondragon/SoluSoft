using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel;
using TAS360.Services;

namespace TAS360.Controllers
{
    public class PuntosAccesoController : Controller
    {
        private readonly IHikvisionService _hikvisionService;
        public PuntosAccesoController()
            : this(new HikvisionService())
        {
        }

        public PuntosAccesoController(IHikvisionService hikvisionService)
        {
            _hikvisionService = hikvisionService ?? throw new ArgumentNullException(nameof(hikvisionService));
        }
        /// <summary>
        /// Metodo principal de Puntos de Acceso 
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<HikvisionPuntosAccesoViewModel></returns>
        [AuthorizeUser(idOperacion: 20)]
        public ActionResult Index()
        {
            List<HikvisionPuntosAccesoViewModel> lista;

            using (var db = new HelpDesk_Entities1())
            {
                lista = (from p in db.PuntosAcceso
                         join pl in db.Planteles on p.idPlantel equals pl.id
                         orderby p.id descending
                         select new HikvisionPuntosAccesoViewModel
                         {
                             id = p.id,
                             //idPlantel = p.idPlantel,
                             NombrePlantel = pl.nombre,
                             nombre = p.nombre,
                             ubicacion = p.ubicacion,
                             FechaHoraInstalacion = p.FechaHoraInstalacion,
                             apiServer = p.apiServer,
                             usuario = p.usuario,
                             password = p.password
                         }).ToList();
            }
            return View(lista);
        }

        /// <summary>
        /// Metodo principal de Puntos de Acceso 
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<HikvisionPuntosAccesoViewModel></returns>
        public ActionResult GetAPByPlantel(PlantelViewModel model)
        {
            List<HikvisionPuntosAccesoViewModel> lista;
            using (var db = new HelpDesk_Entities1())
            {
                lista = (from p in db.PuntosAcceso
                         join pl in db.Planteles on p.idPlantel equals pl.id
                         where p.id == model.Id_AccessControlToView
                         orderby p.id descending
                         select new HikvisionPuntosAccesoViewModel
                         {
                             id = p.id,
                             //idPlantel = p.idPlantel,
                             NombrePlantel = pl.nombre,
                             nombre = p.nombre,
                             ubicacion = p.ubicacion,
                             FechaHoraInstalacion = p.FechaHoraInstalacion,
                             apiServer = p.apiServer,
                             usuario = p.usuario,
                             password = p.password
                         }).ToList();
            }
            return View(lista);
        }
        /// <summary>
        /// Muestra el formulario para crear un nuevo punto de acceso,
        /// cargando la lista de planteles disponibles en un dropdown.
        /// </summary>
        /// <type>GET</type>
        /// <returns>HikvisionPuntosAccesoViewModel</returns>
        [AuthorizeUser(idOperacion: 21)]
        public ActionResult Create()
        {
            var model = new HikvisionPuntosAccesoViewModel();
            using (var db = new HelpDesk_Entities1())
            {
                ViewBag.idPlantel = db.Planteles.Select(p => new SelectListItem
                {
                    Value = p.id.ToString(),
                    Text = p.nombre.ToString()
                }).ToList();
            }

            return View(model);
        }

        /// <summary>
        /// Procesa el formulario para crear un nuevo punto de acceso
        /// y guarda la información en la base de datos.
        /// </summary>
        /// <type>POST</type>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeUser(idOperacion: 21)]
        public ActionResult Create(HikvisionPuntosAccesoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.idPlantel = db.Planteles.Select(p => new SelectListItem
                    {
                        Value = p.id.ToString(),
                        Text = p.nombre.ToString()
                    }).ToList();
                }
                return View(model); // Vuelve a la vista con errores de validación
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = new PuntosAcceso
                {
                    idPlantel = model.idPlantel,
                    nombre = model.nombre,
                    ubicacion = model.ubicacion,
                    FechaHoraInstalacion = model.FechaHoraInstalacion ?? DateTime.Now,
                    estado = model.estado,
                    apiServer = model.apiServer,
                    usuario = model.usuario,
                    password = model.password
                };

                db.PuntosAcceso.Add(entity);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Muestra el formulario para editar un punto de acceso existente
        /// cargando los datos actuales y la lista de planteles disponibles.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuthorizeUser(idOperacion: 22)]
        public ActionResult Edit(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.PuntosAcceso.Find(id);
                if (entity == null)
                    return HttpNotFound();

                var model = new HikvisionPuntosAccesoViewModel
                {
                    id = entity.id,
                    idPlantel = entity.idPlantel,
                    NombrePlantel = db.Planteles
                          .Where(p => p.id == entity.idPlantel)
                          .Select(p => p.nombre)
                          .FirstOrDefault(),
                    nombre = entity.nombre,
                    ubicacion = entity.ubicacion,
                    FechaHoraInstalacion = entity.FechaHoraInstalacion,
                    estado = entity.estado ?? true,
                    apiServer = entity.apiServer,
                    usuario = entity.usuario,
                    password = entity.password
                };
                ViewBag.ListaDePlanteles = db.Planteles.Select(p => new SelectListItem
                {
                    Value = p.id.ToString(),
                    Text = p.nombre.ToString(),
                    Selected = p.id == entity.idPlantel ? true : false

                }).ToList();
                return View(model);
            }
        }
        /// <summary>
        /// Procesa el formulario de edición para un punto de acceso existente
        /// y actualiza los datos en la base de datos.
        /// </summary>
        /// <type>POST</type>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(idOperacion: 22)]
        public ActionResult Edit(HikvisionPuntosAccesoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.idPlantel = db.Planteles.Select(p => new SelectListItem
                    {
                        Value = p.id.ToString(),
                        Text = p.nombre
                    }).ToList();
                }
                return View(model);
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.PuntosAcceso.Find(model.id);
                if (entity == null)
                    return HttpNotFound();
                entity.idPlantel = model.idPlantel;
                entity.nombre = model.nombre;
                entity.ubicacion = model.ubicacion;
                entity.FechaHoraInstalacion = model.FechaHoraInstalacion ?? DateTime.Now;
                entity.estado = model.estado;
                entity.apiServer = model.apiServer;
                entity.usuario = model.usuario;
                entity.password = model.password;

                db.SaveChanges();
            }

            return RedirectToAction("Details/" + model.id);
        }

        /// <summary>
        /// Elimina un tutor directamente desde el listado
        /// </summary>
        /// <param name="id">Id del tutor</param>
        /// <returns>Redirección al Index</returns>
        [AuthorizeUser(idOperacion: 23)]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var db = new HelpDesk_Entities1())
                {
                    var Punto = db.PuntosAcceso.FirstOrDefault(t => t.id == id);
                    if (Punto == null)
                        return HttpNotFound();

                    db.PuntosAcceso.Remove(Punto);
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        //[AuthorizeUser(idOperacion: 18)]
        public ActionResult Details(int id)
        {
            HikvisionPuntosAccesoViewModel model = new HikvisionPuntosAccesoViewModel();
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.PuntosAcceso.Find(id);
                if (entity == null)
                    return HttpNotFound();
                model.apiServer = entity.apiServer;
                model.password = entity.password;
                model.usuario = entity.usuario;
                model.nombre = entity.nombre;
                model.idPlantel = entity.idPlantel;
                model.id = entity.id;
                model.FechaHoraInstalacion = entity.FechaHoraInstalacion;
                model.ubicacion = entity.ubicacion;
                model.estado = entity.estado ?? false;
                model.NombrePlantel = entity.Planteles?.nombre ??
                    db.Planteles.Where(p => p.id == entity.idPlantel).Select(p => p.nombre).FirstOrDefault();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> CheckStatus(int id, string host, bool https = false, int? port = null)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var puntoAcceso = db.PuntosAcceso.FirstOrDefault(p => p.id == id);
                if (puntoAcceso == null)
                {
                    return Json(new { online = false, error = "Punto de acceso no encontrado" }, JsonRequestBehavior.AllowGet);
                }

                var scheme = https ? "https" : "http";
                var apiServer = string.IsNullOrWhiteSpace(host)
                    ? puntoAcceso.apiServer
                    : (port.HasValue ? $"{scheme}://{host}:{port.Value}" : $"{scheme}://{host}");

                if (string.IsNullOrWhiteSpace(apiServer))
                {
                    return Json(new { online = false, error = "Host no disponible" }, JsonRequestBehavior.AllowGet);
                }

                var r = await _hikvisionService.CheckStatusAsync(apiServer, puntoAcceso.usuario, puntoAcceso.password, 2500);
                return Json(new { online = r.Online, latencyMs = r.LatencyMs, statusCode = r.StatusCode, error = r.Error },
                            JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Recupera en formato JSON los eventos del día generados por el punto de acceso seleccionado.
        /// </summary>
        /// <param name="id">Identificador del punto de acceso en la base de datos.</param>
        /// <param name="maxResults">Número máximo de eventos a consultar.</param>
        /// <returns>Listado de eventos del día o un mensaje de error en caso de no poder comunicarse con el dispositivo.</returns>
        [HttpGet]
        public async Task<JsonResult> GetTodayEvents(int id, int maxResults = 50)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var puntoAcceso = db.PuntosAcceso.FirstOrDefault(p => p.id == id);
                if (puntoAcceso == null)
                {
                    return Json(new { ok = false, error = "Punto de acceso no encontrado" }, JsonRequestBehavior.AllowGet);
                }

                var inicio = DateTime.Today;
                var fin = DateTime.Now;

                try
                {
                    var events = await _hikvisionService.GetEventsAsync(puntoAcceso.apiServer, puntoAcceso.usuario, puntoAcceso.password, inicio, fin, maxResults);
                    var projection = events.Select(e => new
                    {
                        employeeNo = e.EmployeeNo,
                        personName = e.PersonName,
                        cardNumber = e.CardNumber,
                        majorEventType = e.MajorEventType,
                        minorEventType = e.MinorEventType,
                        eventTime = e.EventTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                        sourceName = e.SourceName
                    });

                    return Json(new { ok = true, events = projection }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { ok = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }
    }
}
