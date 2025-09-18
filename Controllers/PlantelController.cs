using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models.ViewModel;
using TAS360.Models;

namespace TAS360.Controllers
{
    public class PlantelController : Controller
    {
        // TODO: "Agregar el decorador AutorizedUser para cada metodo"
        // [AuthorizeUser(idOperacion: 21)]  Usa el idOperacion correspondiente a Planteles

        /// <summary>
        /// Metodo principal de Planteles (Obtiene planteles)
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<PlantelViewModel></returns>
        public ActionResult Index()
        {
            List<PlantelViewModel> lst;

            using (var db = new HelpDesk_Entities1()) 
            {
                lst = (from p in db.Planteles
                       select new PlantelViewModel
                       {
                           Id = p.id,
                           Nombre = p.nombre,
                           Telefono = p.telefono,
                           Direccion = p.direccion,
                           Administrador = p.administrador,
                           Director = p.director,
                           FechaHoraRegistro = p.FechaHoraRegistro
                       }).ToList();
            }

            return View(lst);
        }
        // GET: Plantel/Create
        public ActionResult Create()
        {
            var model = new PlantelViewModel(); 
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUser(idOperacion: 21)]
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

            return RedirectToAction("Index"); // <- debería redirigir si todo fue exitoso
        }
        // GET: Plantel/Edit/5
        //[AuthorizeUser(idOperacion: 21)]
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
        //[AuthorizeUser(idOperacion: 21)]
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

                entity.nombre = model.Nombre;
                entity.telefono = model.Telefono;
                entity.direccion = model.Direccion;
                entity.administrador = model.Administrador;
                entity.director = model.Director;

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
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
                GetAccessControlByPlantel(id);
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
    }
}