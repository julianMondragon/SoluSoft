using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Models.ViewModel;
using TAS360.Models;

namespace TAS360.Controllers
{
    public class TutorController : Controller
    {
        /// <summary>
        /// Metodo principal de Estudiantes
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<HikvisionEstudiantesViewModel></returns>
        public ActionResult Index()
        {
            List<HikvisionTutorViewModel> lista;

            using (var db = new HelpDesk_Entities1())
            {
                lista = (from p in db.Tutores
                         select new HikvisionTutorViewModel
                         {
                             id = p.id,
                             Nombre = p.Nombre,
                             Correo = p.correo,
                             Telefono = p.telefono,
                             IdExterno = p.id_externo,
                             //FechaHoraRegistro = p.FechaHoraRegistro
                         }).ToList();
            }
            return View(lista);
        }

        /// <summary>
        /// Muestra el formulario para crear un nuevo EStudiantes,
        /// cargando la lista de planteles disponibles en un dropdown.
        /// </summary>
        /// <type>GET</type>
        /// <returns>HikvisionEstudiantesViewModel</returns>
        public ActionResult Create()
        {
            var model = new HikvisionTutorViewModel();
            return View(model);
        }

        /// <summary>
        /// Procesa el formulario para crear un nuevo estudiante
        /// y guarda la información en la base de datos.
        /// </summary>
        /// <type>POST</type>
        [HttpPost]
        public ActionResult Create(HikvisionTutorViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                using (var db = new HelpDesk_Entities1())
                {
                    var entity = new Tutores
                    {
                        id = model.id,
                        Nombre = model.Nombre,
                        correo = model.Correo,
                        telefono = model.Telefono,
                        id_externo = model.IdExterno
                    };
                    db.Tutores.Add(entity);
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }
        /// <summary>
        /// Muestra el formulario para editar un tutor existente.
        /// </summary>
        /// <param name="id">ID del tutor a editar</param>
        /// <returns>HikvisionTutorViewModel</returns>
        public ActionResult Edit(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Tutores.Find(id);
                if (entity == null)
                    return HttpNotFound();

                var model = new HikvisionTutorViewModel
                {
                    id = entity.id,
                    Nombre = entity.Nombre,
                    Correo = entity.correo,
                    Telefono = entity.telefono,
                    IdExterno = entity.id_externo
                };

                return View(model);
            }
        }

        /// <summary>
        /// Procesa el formulario para actualizar un tutor existente en la base de datos.
        /// </summary>
        [HttpPost]
        public ActionResult Edit(HikvisionTutorViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                using (var db = new HelpDesk_Entities1())
                {
                    var entity = db.Tutores.Find(model.id);
                    if (entity == null)
                        return HttpNotFound();

                    entity.Nombre = model.Nombre;
                    entity.correo = model.Correo;
                    entity.telefono = model.Telefono;
                    entity.id_externo = model.IdExterno;

                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }


        // GET: Tutor/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Tutor/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
