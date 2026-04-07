using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Filters;
using TAS360.Models;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers.ManagerDC3
{
    public class EmpresaController : Controller
    {
        private readonly HelpDesk_Entities1 _context;

        public EmpresaController()
        {
            _context = new HelpDesk_Entities1();
        }

        /// <summary>
        /// Muestra el listado de empresas activas del certificador logueado
        /// </summary>
        /// <returns>Lista de empresas</returns>
        [HttpGet]
        [AuthorizeUser(idOperacion: 42)]
        public ActionResult Index()
        {
            int userId = GetUserId();
            var empresas = _context.Empresa
                .Include("User")
                .Where(e => e.Activo == true && e.CertificadorId == userId)
                .ToList();

            return View(empresas);
        }

        /// <summary>
        ///  Metodo Get para crear una empresa
        /// </summary>
        [HttpGet]
        [AuthorizeUser(idOperacion: 41)]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AuthorizeUser(idOperacion: 41)]
        public ActionResult Create(EmpresaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // 🔴 VALIDACIÓN RFC DUPLICADO
            int userId = GetUserId();
            bool existeRFC = _context.Empresa
                .Any(e => e.RFC == model.RFC && e.CertificadorId == userId);

            if (existeRFC)
            {
                ModelState.AddModelError("RFC", "Ya existe una empresa con este RFC.");
                return View(model);
            }
            var empresa = new Empresa
            {
                Nombre = model.Nombre,
                RFC = model.RFC,
                Direccion = model.Direccion,
                CertificadorId = GetUserId(),
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.Empresa.Add(empresa);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Metodo get para editar una empresa por el id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizeUser(idOperacion: 43)]
        public ActionResult Edit(int id)
        {
            var empresa = _context.Empresa.Find(id);

            if (empresa == null)
                return HttpNotFound();

            var model = new EmpresaViewModel
            {
                Id = empresa.Id,
                Nombre = empresa.Nombre,
                RFC = empresa.RFC,
                Direccion = empresa.Direccion
            };

            return View(model);
        }
        /// <summary>
        /// Metodo Post para editar una empresa por el id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeUser(idOperacion: 43)]
        public ActionResult Edit(EmpresaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // 🔴 VALIDACIÓN RFC DUPLICADO
            int userId = GetUserId();
            bool existeRFC = _context.Empresa
                .Any(e => e.RFC == model.RFC && e.CertificadorId == userId && e.Id != model.Id);

            if (existeRFC)
            {
                ModelState.AddModelError("RFC", "Ya existe una empresa con este RFC.");
                return View(model);
            }
            var empresa = _context.Empresa.Find(model.Id);

            empresa.Nombre = model.Nombre;
            empresa.RFC = model.RFC;
            empresa.Direccion = model.Direccion;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Metodo Get que elimina una empresa estableciendo su estado en falso.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuthorizeUser(idOperacion: 44)]
        public ActionResult Delete(int id)
        {
            var empresa = _context.Empresa.Find(id);

            empresa.Activo = false;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // =============================
        // HELPER
        // =============================
        private int GetUserId()
        {
            return ((User)Session["User"]).id;
        }
    }
}