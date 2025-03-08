using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            HikVisionViewModel model = new HikVisionViewModel();
            return View(model);
        }
        /// <summary>
        /// Método POST: Envía la petición al equipo HikVision y recibe la respuesta
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Index(HikVisionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using (var client = new HttpClient())
                {
                    // Codificación Base64 para autenticación básica
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{model.Usuario}:{model.Password}"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                    // Construcción de la URL de la API
                    string url = $"{model.APIServer}{model.Peticion}";

                    // Cuerpo de la solicitud (si se envía un parámetro)
                    StringContent content = null;
                    if (!string.IsNullOrEmpty(model.Parametro))
                    {
                        content = new StringContent(model.Parametro, Encoding.UTF8, "application/json");
                    }

                    // Enviar solicitud HTTP (GET, POST, PUT...)
                    HttpResponseMessage response;
                    if (string.Equals(model.Peticion.Split(' ')[0], "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        response = await client.GetAsync(url);
                    }
                    else if (string.Equals(model.Peticion.Split(' ')[0], "POST", StringComparison.OrdinalIgnoreCase))
                    {
                        response = await client.PostAsync(url, content);
                    }
                    else if (string.Equals(model.Peticion.Split(' ')[0], "PUT", StringComparison.OrdinalIgnoreCase))
                    {
                        response = await client.PutAsync(url, content);
                    }
                    else
                    {
                        model.Respuesta = "⚠️ Método HTTP no soportado.";
                        return View(model);
                    }

                    // Leer la respuesta del servidor
                    model.Respuesta = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                model.Respuesta = $"❌ Error: {ex.Message}";
            }

            return View(model);
        }
    }
}