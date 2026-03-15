using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TAS360.Models;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        ///  Acción KeepAlive para responder a la solicitud de mantenimiento de sesión
        ///  Agrega al log los datos de usuario, URL de donde se envió la solicitud.
        /// </summary>
        /// <param name="currentUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult KeepAlive(string currentUrl)
        {
            string path = Server.MapPath("~/Logs/KeepAlive/");
            Log oLog = new Log(path);
            string userIp = GetClientIp();

            oLog.Add("------------------------------------------------------------");
            oLog.Add("URL actual: " + currentUrl);
            oLog.Add("IP del Usuario: " + userIp); // Agregar la IP al log
            oLog.Add("Usuario Logeado: " + ((User)Session["User"])?.nombre + ", ID: " + ((User)Session["User"])?.id);

            return new EmptyResult();
        }

        // Método para obtener la IP del cliente
        private string GetClientIp()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.ServerVariables["REMOTE_ADDR"];
            }

            return ip;
        }

        public ActionResult Home()
        {
            return View();
        }
        public ActionResult Index()
        {
            entradasViewModel entradasVM = new entradasViewModel();
            using (bdSimcot_Entities db = new bdSimcot_Entities())
            {
                var aux = (from s in db.Entradas select s);
                if (aux != null && aux.Any())
                {
                    foreach (var a in aux)
                    {
                        entradasVM.entradas.Add(a);
                    }
                }
            }
            return View(entradasVM);
        }
    }
} 