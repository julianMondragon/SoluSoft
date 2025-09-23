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
        public ActionResult Index(string searchString)
        {
            List<HikvisionTutorViewModel> lista;

            using (var db = new HelpDesk_Entities1())
            {
                var query = from p in db.Tutores
                            orderby p.id descending
                            select new HikvisionTutorViewModel
                            {
                                id = p.id,
                                Nombre = p.Nombre,
                                Correo = p.correo,
                                Telefono = p.telefono,
                                IdExterno = p.id_externo
                            };
                if (!String.IsNullOrEmpty(searchString))
                {
                    query = query.Where(t =>
                        t.Nombre.Contains(searchString) ||
                        t.Correo.Contains(searchString) ||
                        t.Telefono.Contains(searchString) ||
                        t.IdExterno.Contains(searchString)
                    );
                }
                lista = query.ToList();
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


        public ActionResult Asesorias(string searchString)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var query = from rel in db.EstudiantesXTutor
                            join e in db.Estudiantes on rel.idEstudiante equals e.id
                            join t in db.Tutores on rel.idTutor equals t.id
                            select new TutorEstudianteViewModel
                            {
                                Id = rel.id,
                                Tutor = t.id,
                                N_Tutor = t.Nombre,
                                Estudiante = e.id,
                                N_Estudiante = e.nombre,
                                fechaHora = rel.fechaHora
                            };

                // 🔍 Filtro de búsqueda
                if (!String.IsNullOrEmpty(searchString))
                {
                    query = query.Where(x =>
                        x.N_Tutor.Contains(searchString) ||
                        x.N_Estudiante.Contains(searchString)
                    );
                }

                var lista = query
                            .OrderByDescending(x => x.fechaHora)
                            .ToList();

                return View(lista);
            }
        }


        /// <summary>
        /// Muestra el formulario para crear un nuevo EStudiantes,
        /// cargando la lista de planteles disponibles en un dropdown.
        /// </summary>
        /// <type>GET</type>
        /// <returns>HikvisionEstudiantesViewModel</returns>
        public ActionResult CreateAsesorias(int? idTutor, int? idEstudiante)
        {
            using (var db = new HelpDesk_Entities1())
            {
                ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", idTutor);
                ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", idEstudiante);
            }
            var model = new TutorEstudianteViewModel
            {
                Tutor = idTutor ?? 0,
                Estudiante = idEstudiante ?? 0,
                fechaHora = DateTime.Now
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult CreateAsesorias(TutorEstudianteViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    using (var db = new HelpDesk_Entities1())
                    {
                        ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                        ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);
                    }
                    return View(model);
                }

                using (var db = new HelpDesk_Entities1())
                {
                    // 🔎 Validar si ya existe la relación
                    var existeRelacion = db.EstudiantesXTutor.Any(x =>
                        x.idTutor == model.Tutor &&
                        x.idEstudiante == model.Estudiante
                    );

                    if (existeRelacion)
                    {
                        ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                        ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);

                        ModelState.AddModelError("", "⚠️ La relación entre este tutor y estudiante ya existe.");
                        return View(model);
                    }

                    // Si no existe, crear la nueva relación
                    var entity = new EstudiantesXTutor
                    {
                        idTutor = model.Tutor,
                        idEstudiante = model.Estudiante,
                        fechaHora = model.fechaHora ?? DateTime.Now
                    };
                    db.EstudiantesXTutor.Add(entity);
                    db.SaveChanges();
                }

                //return RedirectToAction("Edit");
                return RedirectToAction("Details", "Estudiantes", new { id = model.Estudiante });
            }
            catch (Exception ex)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                    ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);
                }
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }


        /// <summary>
        /// Muestra el formulario para editar la relación Tutor - Estudiante
        /// </summary>
        /// <param name="id">ID de la relación en EstudiantesXTutor</param>
        /// <returns>TutorEstudianteViewModel</returns>
        public ActionResult EditAsesorias(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.EstudiantesXTutor.FirstOrDefault(x => x.id == id);
                if (entity == null)
                {
                    return HttpNotFound();
                }

                var model = new TutorEstudianteViewModel
                {
                    Id = entity.id,
                    Tutor = entity.idTutor,
                    Estudiante = entity.idEstudiante,
                    fechaHora = entity.fechaHora
                };

                ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult EditAsesorias(TutorEstudianteViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    using (var db = new HelpDesk_Entities1())
                    {
                        ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                        ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);
                    }
                    return View(model);
                }

                using (var db = new HelpDesk_Entities1())
                {
                    var entity = db.EstudiantesXTutor.FirstOrDefault(x => x.id == model.Id);
                    if (entity == null)
                    {
                        return HttpNotFound();
                    }

                    entity.idTutor = model.Tutor;
                    entity.idEstudiante = model.Estudiante;
                    entity.fechaHora = model.fechaHora ?? DateTime.Now;

                    db.SaveChanges();
                }

                return RedirectToAction("Asesorias");
            }
            catch (Exception ex)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.Tutores = new SelectList(db.Tutores.ToList(), "id", "Nombre", model.Tutor);
                    ViewBag.Estudiantes = new SelectList(db.Estudiantes.ToList(), "id", "Nombre", model.Estudiante);
                }
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }


        /// <summary>
        /// Elimina un tutor directamente desde el listado
        /// </summary>
        /// <param name="id">Id del tutor</param>
        /// <returns>Redirección al Index</returns>
        public ActionResult Delete(int id)
        {
            try
            {
                using (var db = new HelpDesk_Entities1())
                {
                    var tutor = db.Tutores.FirstOrDefault(t => t.id == id);
                    if (tutor == null)
                        return HttpNotFound();

                    db.Tutores.Remove(tutor);
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


        /// <summary>
        /// Elimina un tutor directamente desde el listado
        /// </summary>
        /// <param name="id">Id del tutor</param>
        /// <returns>Redirección al Index</returns>
        public ActionResult DeleteAsesoria(int id)
        {
            try
            {
                using (var db = new HelpDesk_Entities1())
                {
                    var tutor = db.EstudiantesXTutor.FirstOrDefault(t => t.id == id);
                    if (tutor == null)
                        return HttpNotFound();

                    db.EstudiantesXTutor.Remove(tutor);
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



    }
}
