using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class PuntosAccesoController : Controller
    {
        /// <summary>
        /// Metodo principal de Puntos de Acceso 
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<HikvisionPuntosAccesoViewModel></returns>
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
        /// Muestra el formulario para crear un nuevo punto de acceso,
        /// cargando la lista de planteles disponibles en un dropdown.
        /// </summary>
        /// <type>GET</type>
        /// <returns>HikvisionPuntosAccesoViewModel</returns>
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
                ViewBag.idPlantel = db.Planteles.Select(p => new SelectListItem
                {
                    Value = p.id.ToString(),
                    Text = p.nombre.ToString()
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

            return RedirectToAction("Index");
        }
    }
}
