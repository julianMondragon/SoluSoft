using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TAS360.Models.ViewModel;

namespace TAS360.Controllers
{
    public class HikVisionController : Controller
    {
        /// <summary>
        /// Metodo encargado probar la comunicacion con un equipo
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            HikVisionViewModel model = new HikVisionViewModel()
            {
                APIServer = "http://192.168.0.5",
                Peticion = "GET /ISAPI/AccessControl/CardInfo/Capabilities?format=json",
                Usuario = "admin",
                Password = "DS-K1T320",
            };
            return View(model);
        }
        /// <summary>
        /// Método POST: Envía la petición al equipo HikVision y recibe la respuesta
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Index(HikVisionViewModel model) 
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Warning = "Modelo no valido";
                return View(model);
            }
            try
            {
                string baseUrl = model.APIServer?.Trim();

                if (string.IsNullOrEmpty(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                {
                    ViewBag.Warning = "❌ Error: La URL del servidor no es válida.";
                    return View(model);
                }
                if (model.Peticion.IsNullOrWhiteSpace()) 
                {
                    ViewBag.Warning = "❌ Error: La URL de la peticion no es válida.";
                    return View(model);
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler
                {
                    Credentials = new NetworkCredential(model.Usuario, model.Password)
                };

                using (var client = new HttpClient(handler))
                {
                    string[] peticionSplit = model.Peticion.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    string metodoHttp = peticionSplit[0].ToUpper().Trim();
                    string ruta = peticionSplit[1].Trim();
                    string url = $"{model.APIServer.TrimEnd('/')}{ruta}";

                    HttpResponseMessage response;
                    if (metodoHttp == "GET")
                    {
                        response = await client.GetAsync(url);
                    }
                    else if (metodoHttp == "POST")
                    {
                        var content = new StringContent(model.Parametro ?? "", Encoding.UTF8, "application/json");
                        response = await client.PostAsync(url, content);
                    }
                    else if (metodoHttp == "PUT")
                    {
                        var content = new StringContent(model.Parametro ?? "", Encoding.UTF8, "application/json");
                        response = await client.PutAsync(url, content);
                    }
                    else
                    {
                        ViewBag.Info = "⚠️ Método HTTP no soportado.";
                        return View(model);
                    }

                    model.Respuesta = await response.Content.ReadAsStringAsync();
                    ViewBag.Respuesta = model.Respuesta;
                }
            }
            catch (Exception ex)
            {
                model.Respuesta = $"❌ Error: {ex.Message}";
                ViewBag.Warning = $"❌ Error: {ex.Message}";
            }

            return View(model);
        }

    }
}
