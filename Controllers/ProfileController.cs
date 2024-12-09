using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using TAS360.Models;
using TAS360.Models.ViewModel;
using System.Web;
using System.IO;
using DocumentFormat.OpenXml.EMMA;

namespace TAS360.Controllers
{
    public class ProfileController : Controller
    {
        // GET: Profile/Index
        public ActionResult Index()
        {
            PerfilusrViewModel perfil = new PerfilusrViewModel();
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                int userId = ((User)Session["User"]).id;

                // Buscar el perfil del usuario conectado en la base de datos usando el ID
                var usuario = db.usr_profile.FirstOrDefault(u => u.id_User == userId);

                if (usuario != null)
                {
                    perfil.id_User = usuario.id;
                    perfil.nombre = usuario.nombre;
                    perfil.email = usuario.email;
                    perfil.Cel = usuario.Cel;
                    perfil.Género = usuario.Género;
                    perfil.Estado = usuario.Estado;
                    perfil.Foto_usuario = usuario.Foto_usuario;

                }
                else
                {
                    ViewBag.ErrorMessage = "Usuario no encontrado.";
                }
            }

            return View(perfil);
        }
        [HttpGet]
        public ActionResult Editprofile()
        {
            PerfilusrViewModel perfil = new PerfilusrViewModel();
            using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
            {
                
                User user = (User)Session["User"];
                if (user == null)
                {
                    return Redirect("~\\Acceso\\login");
                }
                int userId = ((User)Session["User"]).id;
                // Buscar el perfil del usuario en la base de datos usando el ID del usuario
                var usuario = db.usr_profile.FirstOrDefault(u => u.id_User == userId);

                if (usuario != null)
                {
                    perfil.nombre = usuario.nombre;
                    perfil.email = usuario.email;
                    perfil.Cel = usuario.Cel;
                    perfil.Género = usuario.Género;
                    perfil.Estado = usuario.Estado;
                    perfil.Foto_usuario = usuario.Foto_usuario;

                    GetGeneroOptions(perfil.Género);
                    GetEstadoOptions(perfil.Estado);
                }
                else
                {
                    return HttpNotFound("Usuario no encontrado.");
                }
            } 
            return View(perfil);
        }

        [HttpPost]
        public ActionResult Editprofile(PerfilusrViewModel perfil)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (HelpDesk_Entities1 db = new HelpDesk_Entities1())
                    {
                        int userId = ((User)Session["User"]).id;

                        // Buscar el perfil del usuario en la base de datos usando el ID del usuario
                        var profile = db.usr_profile.FirstOrDefault(u => u.id_User == userId);
                        if (profile == null)
                        {
                            GetGeneroOptions(perfil.Género);
                            GetEstadoOptions(perfil.Estado);
                            ViewBag.ErrorMessage = "Registro de perfil no encontrado.";
                            return View(perfil);
                        }

                        // Asignar los valores del perfil al usuario
                        profile.nombre = perfil.nombre;
                        profile.email = perfil.email;
                        profile.Cel = perfil.Cel;
                        profile.Género = perfil.Género;
                        profile.Estado = perfil.Estado;

                        var user = db.User.Find(userId);
                        if (user == null)
                        {
                            GetGeneroOptions(perfil.Género);
                            GetEstadoOptions(perfil.Estado);
                            ViewBag.ErrorMessage = "Registro de usuario no encontrado.";
                            return View();
                        }
                        user.nombre = perfil.nombre;
                        db.SaveChanges();
                    }
                    TempData["Editprofile"] = "Si has realizado un cambio en el nombre de usuario, cierra la sesión y vuelve a iniciar para ver el cambio.";
                    return RedirectToAction("Index");

                }
                // Si el modelo no es válido, regresa la vista con el modelo para mostrar los errores
                ViewBag.ErrorMessage = "Por favor, complete todos los campos requeridos.";   
                return View(perfil);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar el registro: " + ex.Message);
                ViewBag.ErrorMessage = "Ocurrió un error inesperado. Por favor, inténtelo de nuevo más tarde.";
                GetGeneroOptions(perfil.Género);
                GetEstadoOptions(perfil.Estado);
                return View(perfil);
            }
        }


        private void GetGeneroOptions(string selectedGenero = null)
        {
            List<SelectListItem> generoOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "Masculino", Value = "Masculino" },
                new SelectListItem { Text = "Femenino", Value = "Femenino" },
                new SelectListItem { Text = "Otro", Value = "Otro" }
            };
            // Verifica si hay un género seleccionado y si existe en la lista
            if (!string.IsNullOrEmpty(selectedGenero))
            {
                var selectedOption = generoOptions.FirstOrDefault(g => g.Value == selectedGenero);
                if (selectedOption != null)
                {
                    selectedOption.Selected = true;
                }
            }
            ViewBag.GeneroOptions = generoOptions;
        }




        private void GetEstadoOptions(string selectedEstado = null)
        {
            List<SelectListItem> estadoOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "Aguascalientes", Value = "Aguascalientes" },
                new SelectListItem { Text = "Baja California", Value = "Baja California" },
                new SelectListItem { Text = "Baja California Sur", Value = "Baja California Sur" },
                new SelectListItem { Text = "Campeche", Value = "Campeche" },
                new SelectListItem { Text = "Chiapas", Value = "Chiapas" },
                new SelectListItem { Text = "Chihuahua", Value = "Chihuahua" },
                new SelectListItem { Text = "Coahuila", Value = "Coahuila" },
                new SelectListItem { Text = "Colima", Value = "Colima" },
                new SelectListItem { Text = "Durango", Value = "Durango" },
                new SelectListItem { Text = "Guanajuato", Value = "Guanajuato" },
                new SelectListItem { Text = "Guerrero", Value = "Guerrero" },
                new SelectListItem { Text = "Hidalgo", Value = "Hidalgo" },
                new SelectListItem { Text = "Jalisco", Value = "Jalisco" },
                new SelectListItem { Text = "México", Value = "México" },
                new SelectListItem { Text = "Michoacán", Value = "Michoacán" },
                new SelectListItem { Text = "Morelos", Value = "Morelos" },
                new SelectListItem { Text = "Nayarit", Value = "Nayarit" },
                new SelectListItem { Text = "Nuevo León", Value = "Nuevo León" },
                new SelectListItem { Text = "Oaxaca", Value = "Oaxaca" },
                new SelectListItem { Text = "Puebla", Value = "Puebla" },
                new SelectListItem { Text = "Querétaro", Value = "Querétaro" },
                new SelectListItem { Text = "Quintana Roo", Value = "Quintana Roo" },
                new SelectListItem { Text = "San Luis Potosí", Value = "San Luis Potosí" },
                new SelectListItem { Text = "Sinaloa", Value = "Sinaloa" },
                new SelectListItem { Text = "Sonora", Value = "Sonora" },
                new SelectListItem { Text = "Tabasco", Value = "Tabasco" },
                new SelectListItem { Text = "Tamaulipas", Value = "Tamaulipas" },
                new SelectListItem { Text = "Tlaxcala", Value = "Tlaxcala" },
                new SelectListItem { Text = "Veracruz", Value = "Veracruz" },
                new SelectListItem { Text = "Yucatán", Value = "Yucatán" },
                new SelectListItem { Text = "Zacatecas", Value = "Zacatecas" }
            };

            // Selecciona el estado actual si es necesario
            if (!string.IsNullOrEmpty(selectedEstado))
            {
                var selectedOption = estadoOptions.FirstOrDefault(e => e.Value == selectedEstado);
                if (selectedOption != null) // Verificar que el objeto no sea null
                {
                    selectedOption.Selected = true;
                }
            }

            ViewBag.EstadoOptions = estadoOptions;
        }






    }
}
