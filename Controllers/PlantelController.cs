using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models.ViewModel;
using TAS360.Models;
using DocumentFormat.OpenXml.EMMA;

namespace TAS360.Controllers
{
    public class PlantelController : Controller
    {
        // TODO: "Agregar el decorador AutorizedUser para cada metodo"
         [AuthorizeUser(idOperacion: 14)]  //Usa el idOperacion correspondiente a Planteles

        /// <summary>
        /// Metodo principal de Planteles (Obtiene planteles)
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<PlantelViewModel></returns>
        public ActionResult Index(string searchString)
        {
            List<PlantelViewModel> lst;

            using (var db = new HelpDesk_Entities1())
            {
                var query = from p in db.Planteles
                            select new PlantelViewModel
                            {
                                Id = p.id,
                                Nombre = p.nombre,
                                Telefono = p.telefono,
                                Direccion = p.direccion,
                                Administrador = p.administrador,
                                Director = p.director,
                                FechaHoraRegistro = p.FechaHoraRegistro
                            };

                if (!String.IsNullOrEmpty(searchString))
                {
                    query = query.Where(p =>
                        p.Nombre.Contains(searchString) ||
                        p.Administrador.Contains(searchString) ||
                        p.Director.Contains(searchString) ||
                        p.Telefono.Contains(searchString)
                    );
                }

                lst = query.ToList();
            }

            return View(lst);
        }

        // GET: Plantel/Create
        [AuthorizeUser(idOperacion: 15)]
        public ActionResult Create()
        {
            var model = new PlantelViewModel(); 
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(idOperacion: 15)]
        public ActionResult Create(PlantelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Vuelve a la vista con errores de validación
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = new Planteles
                {
                    nombre = model.Nombre,
                    telefono = model.Telefono,
                    direccion = model.Direccion,
                    administrador = model.Administrador,
                    director = model.Director,
                    FechaHoraRegistro = DateTime.Now
                };

                db.Planteles.Add(entity);
                db.SaveChanges();
            }

            string path = Server.MapPath("~/Logs/Plantel/");
            Log oLog = new Log(path);
            oLog.Add("El usuario " + ((User)Session["User"]).nombre + " hizo el registro del plantel " + model.Nombre);
            oLog = null;

            return RedirectToAction("Index"); // <- debería redirigir si todo fue exitoso
        }
        // GET: Plantel/Edit/5
        [AuthorizeUser(idOperacion: 16)]
        public ActionResult Edit(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Planteles.Find(id);
                if (entity == null)
                    return HttpNotFound();

                var model = new PlantelViewModel
                {
                    Id = entity.id,
                    Nombre = entity.nombre,
                    Telefono = entity.telefono,
                    Direccion = entity.direccion,
                    Administrador = entity.administrador,
                    Director = entity.director,
                    FechaHoraRegistro = entity.FechaHoraRegistro
                };

                return View(model);
            }
        }
        // POST: Plantel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(idOperacion: 16)]
        public ActionResult Edit(PlantelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Planteles.Find(model.Id);
                if (entity == null)
                    return HttpNotFound();

                // --- DATOS ANTES ---
                string datosAntes = $"ID: {entity.id}, " +
                                    $"Nombre: {entity.nombre}, " +
                                    $"Teléfono: {entity.telefono}, " +
                                    $"Dirección: {entity.direccion}, " +
                                    $"Administrador: {entity.administrador}, " +
                                    $"Director: {entity.director}";

                // Actualizamos entidad con los nuevos datos
                entity.nombre = model.Nombre;
                entity.telefono = model.Telefono;
                entity.direccion = model.Direccion;
                entity.administrador = model.Administrador;
                entity.director = model.Director;

                // --- DATOS DESPUÉS ---
                string datosDespues = $"ID: {entity.id}, " +
                                      $"Nombre: {entity.nombre}, " +
                                      $"Teléfono: {entity.telefono}, " +
                                      $"Dirección: {entity.direccion}, " +
                                      $"Administrador: {entity.administrador}, " +
                                      $"Director: {entity.director}";

                db.SaveChanges();

                // --- LOG ---
                string path = Server.MapPath("~/Logs/Plantel/");
                Log oLog = new Log(path);
                oLog.Add($"Usuario: {((User)Session["User"]).nombre} modificó el plantel con ID {entity.id}\n" +
                         $"--- DATOS ANTES ---\n{datosAntes}\n" +
                         $"--- DATOS DESPUÉS ---\n{datosDespues}\n" +
                         $"Fecha: {DateTime.Now}");
                oLog = null;
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        [AuthorizeUser(idOperacion: 18)]
        public ActionResult Details(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Planteles.Find(id);
                if (entity == null)
                    return HttpNotFound();

                var puntosAccesoQuery = db.PuntosAcceso.Where(pa => pa.idPlantel == id);
                var estudiantesQuery = db.Estudiantes.Where(es => es.idEscuela == id);
                var model = new PlantelViewModel
                {
                    Id = entity.id,
                    Nombre = entity.nombre,
                    Telefono = entity.telefono,
                    Direccion = entity.direccion,
                    Administrador = entity.administrador,
                    Director = entity.director,
                    FechaHoraRegistro = entity.FechaHoraRegistro,

                    PuntosAcceso = puntosAccesoQuery.Select(pa => new HikvisionPuntosAccesoViewModel
                    {
                        id = pa.id,
                        idPlantel = pa.idPlantel,
                        nombre = pa.nombre,
                        NombrePlantel = entity.nombre,
                        ubicacion = pa.ubicacion,
                        FechaHoraInstalacion = pa.FechaHoraInstalacion,
                        apiServer = pa.apiServer,
                        usuario = pa.usuario,
                        password = pa.password
                    }).ToList(),

                    Estudiantes = estudiantesQuery.Select(es => new HikvisionEstudiantesViewModel
                    {
                        id = es.id,
                        IdEscuela = es.idEscuela,
                        Nombre = es.nombre,
                        CorreoPersonal = es.correoPersonal,
                        IdExterno = es.id_externo
                    }).ToList()
                };

                return View(model);
            }
        }

        /// <summary>
        /// Devuelve a la vista una lista de los puntos de acceso que pertenecen al plantel. del sistema 
        /// </summary>
        private void GetAccessControlByPlantel(int idPlantel)
        {

            List<SelectListItem> AccessControl = new List<SelectListItem>();
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                var aux = (from s in db.PuntosAcceso where s.idPlantel == idPlantel select s);
                if (aux != null && aux.Any())
                {
                    foreach (var a in aux)
                    {
                        AccessControl.Add(new SelectListItem
                        {
                            Text = a.nombre,
                            Value = a.id.ToString()

                        });
                    }
                }
            }
            ViewBag.AccessControl = AccessControl;
        }


        // POST: Plantel/Delete/5
        //[AuthorizeUser(idOperacion: 17)] // Cambia el idOperacion según corresponda
        public ActionResult Delete(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Planteles.Find(id);
                if (entity == null)
                    return HttpNotFound();

                string plantelNombre = entity.nombre;

                db.Planteles.Remove(entity);
                db.SaveChanges();

                string path = Server.MapPath("~/Logs/Plantel/");
                Log oLog = new Log(path);
                oLog.Add("El usuario " + ((User)Session["User"]).nombre +
                         " eliminó el registro del plantel: " + plantelNombre);
                oLog = null;
            }

            return RedirectToAction("Index");
        }




    }
}