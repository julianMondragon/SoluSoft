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
    public class EstudiantesController : Controller
    {
        /// <summary>
        /// Metodo principal de Estudiantes
        /// </summary>
        /// <tipe>GET</tipe>
        /// <returns>List<HikvisionEstudiantesViewModel></returns>
        [AuthorizeUser(idOperacion: 28)]
        public ActionResult Index(string searchString)
        {
            List<HikvisionEstudiantesViewModel> lista;

            using (var db = new HelpDesk_Entities1())
            {
                var query = from p in db.Estudiantes
                            join pl in db.Planteles on p.idEscuela equals pl.id
                            orderby p.id descending
                            select new HikvisionEstudiantesViewModel
                            {
                                id = p.id,
                                IdEscuela = p.idEscuela,
                                NombrePlantel = pl.nombre,
                                Nombre = p.nombre,
                                Grado = p.Grado,
                                CorreoPersonal = p.correoPersonal,
                                //CorreoTutor = p.correoTutor,
                                TelefonoPersonal = p.telefonoPersonal,
                                //TelefonoTutor = p.telefonoTutor,
                                IdExterno = p.id_externo,
                                FechaHoraRegistro = p.fechaHoraRegistro ?? DateTime.Now
                            };

                if (!String.IsNullOrEmpty(searchString))
                {
                    query = query.Where(e =>
                        e.Nombre.Contains(searchString) ||
                        e.NombrePlantel.Contains(searchString) ||
                        e.CorreoPersonal.Contains(searchString) ||
                        //e.CorreoTutor.Contains(searchString) ||
                        e.TelefonoPersonal.Contains(searchString)
                        //e.TelefonoTutor.Contains(searchString)
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
        [AuthorizeUser(idOperacion: 29)]
        public ActionResult Create()
        {
            var model = new HikvisionEstudiantesViewModel();
            using (var db = new HelpDesk_Entities1())
            {
                ViewBag.idEscuela = db.Planteles.Select(p => new SelectListItem
                {
                    Value = p.id.ToString(),
                    Text = p.nombre
                }).ToList();
            }
            return View(model);
        }

        /// <summary>
        /// Procesa el formulario para crear un nuevo estudiante
        /// y guarda la información en la base de datos.
        /// </summary>
        /// <type>POST</type>
        [AuthorizeUser(idOperacion: 29)]
        [HttpPost]
        public ActionResult Create(HikvisionEstudiantesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.idEscuela = db.Planteles.Select(p => new SelectListItem
                    {
                        Value = p.id.ToString(),
                        Text = p.nombre
                    }).ToList();
                }
                return View(model); // Vuelve a la vista con errores de validación
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = new Estudiantes
                {
                    idEscuela = model.IdEscuela,
                    nombre = model.Nombre,
                    Grado = model.Grado,
                    correoPersonal = model.CorreoPersonal,
                    //correoTutor = model.CorreoTutor,
                    telefonoPersonal = model.TelefonoPersonal,
                    //telefonoTutor = model.TelefonoTutor,
                    id_externo = model.IdExterno,
                    fechaHoraRegistro = model.FechaHoraRegistro 
                };

                db.Estudiantes.Add(entity);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Muestra el formulario para editar un estudiante existente
        /// cargando los datos actuales y la lista de escuelas disponibles.
        /// </summary>
        [AuthorizeUser(idOperacion: 30)]
        public ActionResult Edit(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Estudiantes.Find(id);
                if (entity == null)
                    return HttpNotFound();

                var model = new HikvisionEstudiantesViewModel
                {
                    id = entity.id,
                    IdEscuela = entity.idEscuela,
                    Nombre = entity.nombre,
                    Grado = entity.Grado,
                    CorreoPersonal = entity.correoPersonal,
                    //CorreoTutor = entity.correoTutor,
                    TelefonoPersonal = entity.telefonoPersonal,
                    //TelefonoTutor = entity.telefonoTutor,
                    IdExterno = entity.id_externo,
                    FechaHoraRegistro = entity.fechaHoraRegistro ?? DateTime.Now
                };

                // Aquí llenamos el ViewBag con la lista de planteles
                ViewBag.IdEscuela = db.Planteles
                    .Select(e => new SelectListItem
                    {
                        Value = e.id.ToString(),
                        Text = e.nombre
                    }).ToList();

                return View(model);
            }
        }


        /// <summary>
        /// Procesa el formulario de edición para un estudiante existente
        /// y actualiza los datos en la base de datos.
        /// </summary>
        [AuthorizeUser(idOperacion: 30)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HikvisionEstudiantesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                using (var db = new HelpDesk_Entities1())
                {
                    ViewBag.IdEscuela = db.Planteles
                        .Select(e => new SelectListItem
                        {
                            Value = e.id.ToString(),
                            Text = e.nombre
                        }).ToList();
                }
                return View(model);
            }

            using (var db = new HelpDesk_Entities1())
            {
                var entity = db.Estudiantes.Find(model.id);
                if (entity == null)
                    return HttpNotFound();

                entity.idEscuela = model.IdEscuela;
                entity.nombre = model.Nombre;
                entity.Grado = model.Grado;
                entity.correoPersonal = model.CorreoPersonal;
                //entity.correoTutor = model.CorreoTutor;
                entity.telefonoPersonal = model.TelefonoPersonal;
                //entity.telefonoTutor = model.TelefonoTutor;
                entity.id_externo = model.IdExterno;
                entity.fechaHoraRegistro = model.FechaHoraRegistro;

                try
                {
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                {
                    var inner = ex.InnerException?.InnerException?.Message;
                    throw new Exception("Error al guardar: " + inner, ex);
                }
            }

            return RedirectToAction("Index");
        }



        /// <summary>
        /// Elimina un tutor directamente desde el listado
        /// </summary>
        /// <param name="id">Id del tutor</param>
        /// <returns>Redirección al Index</returns>
        [AuthorizeUser(idOperacion: 31)]
        public ActionResult Delete(int id)
        {
            try
            {
                using (var db = new HelpDesk_Entities1())
                {
                    var Estudi = db.Estudiantes.FirstOrDefault(t => t.id == id);
                    if (Estudi == null)
                        return HttpNotFound();

                    db.Estudiantes.Remove(Estudi);
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


        //[HttpGet]
        //[AuthorizeUser(idOperacion: 18)]
        //public ActionResult Details(int id)
        //{
        //    using (var db = new HelpDesk_Entities1())
        //    {
        //        var entity = db.Estudiantes.Find(id);
        //        if (entity == null)
        //            return HttpNotFound();

        //        var TutorQuery = db.Tutores.Where(pa => pa.idPlantel == id);
        //        var estudiantesQuery = db.Estudiantes.Where(es => es.idEscuela == id);
        //        var model = new PlantelViewModel
        //        {
        //            Id = entity.id,
        //            Nombre = entity.nombre,
        //            Telefono = entity.telefonoPersonal,
        //            Direccion = entity.correoPersonal,

        //            Tutor = puntosAccesoQuery.Select(pa => new HikvisionTutorViewModel
        //            {
        //                id = pa.id,
        //                idPlantel = pa.idPlantel,
        //                nombre = pa.nombre,
        //                NombrePlantel = entity.nombre,
        //                ubicacion = pa.ubicacion,
        //                FechaHoraInstalacion = pa.FechaHoraInstalacion,
        //                apiServer = pa.apiServer,
        //                usuario = pa.usuario,
        //                password = pa.password
        //            }).ToList(),

        //            Estudiantes = estudiantesQuery.Select(es => new HikvisionEstudiantesViewModel
        //            {
        //                id = es.id,
        //                IdEscuela = es.idEscuela,
        //                Nombre = es.nombre,
        //                CorreoPersonal = es.correoPersonal,
        //                IdExterno = es.id_externo
        //            }).ToList()
        //        };

        //        return View(model);
        //    }
        //}

        [HttpGet]
        //[AuthorizeUser(idOperacion: 18)]
        public ActionResult Details(int id)
        {
            using (var db = new HelpDesk_Entities1())
            {
                // Buscar al estudiante
                var entity = db.Estudiantes.Find(id);
                if (entity == null)
                    return HttpNotFound();

                // Buscar tutores asociados al estudiante por la tabla intermedia
                var tutoresQuery = from rel in db.EstudiantesXTutor
                                   join t in db.Tutores on rel.idTutor equals t.id
                                   where rel.idEstudiante == id
                                   select new HikvisionTutorViewModel
                                   {
                                       id = t.id,
                                       Nombre = t.Nombre,
                                       Correo = t.correo,
                                       Telefono = t.telefono
                                   };

                // Armar el ViewModel
                var model = new HikvisionEstudiantesViewModel
                {
                    id = entity.id,
                    Nombre = entity.nombre,
                    CorreoPersonal = entity.correoPersonal,
                    TelefonoPersonal = entity.telefonoPersonal,
                    Tutor = tutoresQuery.ToList()
                };

                return View(model);
            }
        }



    }
}
