using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class CursoController : Controller
    {
        private readonly HelpDesk_Entities1 _context;

        public CursoController()
        {
            _context = new HelpDesk_Entities1();
        }

        /// <summary>
        /// Listado de cursos del certificador
        /// </summary>
        [AuthorizeUser(idOperacion: 45)]
        public ActionResult Index()
        {
            int userId = GetUserId();

            var cursos = _context.Curso
                .Include("AreaTematica")
                .Where(c => c.Activo == true && c.CertificadorId == userId)
                .ToList();

            return View(cursos);
        }

        /// <summary>
        /// GET: Crear curso
        /// </summary>
        [AuthorizeUser(idOperacion: 46)]
        public ActionResult Create()
        {
            var model = new CursoViewModel
            {
                AreasTematicas = GetAreasTematicas()
            };

            return View(model);
        }

        /// <summary>
        /// POST: Crear curso
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 46)]
        public ActionResult Create(CursoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AreasTematicas = GetAreasTematicas();
                return View(model);
            }

            int userId = GetUserId();

            var curso = new Curso
            {
                Nombre = model.Nombre,
                DuracionHoras = model.DuracionHoras,
                AreaTematicaId = model.AreaTematicaId,
                CertificadorId = userId,
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.Curso.Add(curso);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// GET: Editar curso
        /// </summary>
        [AuthorizeUser(idOperacion: 47)]
        public ActionResult Edit(int id)
        {
            int userId = GetUserId();

            var curso = _context.Curso.Find(id);

            if (curso == null || curso.CertificadorId != userId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new CursoViewModel
            {
                Id = curso.Id,
                Nombre = curso.Nombre,
                DuracionHoras = curso.DuracionHoras,
                AreaTematicaId = curso.AreaTematicaId,
                AreasTematicas = GetAreasTematicas()
            };

            return View(model);
        }

        /// <summary>
        /// POST: Editar curso
        /// </summary>
        [HttpPost]
        [AuthorizeUser(idOperacion: 47)]
        public ActionResult Edit(CursoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AreasTematicas = GetAreasTematicas();
                return View(model);
            }

            int userId = GetUserId();

            var curso = _context.Curso.Find(model.Id);

            if (curso == null || curso.CertificadorId != userId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            curso.Nombre = model.Nombre;
            curso.DuracionHoras = model.DuracionHoras;
            curso.AreaTematicaId = model.AreaTematicaId;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Eliminar lógico
        /// </summary>
        [AuthorizeUser(idOperacion: 48)]
        public ActionResult Delete(int id)
        {
            int userId = GetUserId();

            var curso = _context.Curso.Find(id);

            if (curso == null || curso.CertificadorId != userId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            curso.Activo = false;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // =============================
        // HELPERS
        // =============================
        private int GetUserId()
        {
            return ((User)Session["User"]).id;
        }

        private List<SelectListItem> GetAreasTematicas()
        {
            return _context.AreaTematica
                .Where(a => a.Activo == true)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Clave + " - " + a.Nombre
                }).ToList();
        }
    }
}