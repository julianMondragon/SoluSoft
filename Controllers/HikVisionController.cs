using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
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
using System.Xml.Linq;
using TAS360.Models.ViewModel;
using TAS360.Services;

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
                APIServer = "http://192.168.0.2",
                Peticion = "GET /ISAPI/", //AccessControl/CardInfo/Capabilities?format=json",
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
                    if (model.Respuesta.TrimStart().StartsWith("{"))
                    {
                        ViewBag.ContentType = "application/json";
                    }                        
                    else if (model.Respuesta.TrimStart().StartsWith("<"))
                    {
                        ViewBag.ContentType = "application/xml";
                        try
                        {
                            var xmlDoc = XDocument.Parse(model.Respuesta);

                            // 🔎 Buscar un nodo por ejemplo <deviceName>
                            var deviceName = xmlDoc.Descendants("deviceName").FirstOrDefault()?.Value;
                            ViewBag.DeviceName = deviceName;

                            // 🔧 quí se hace la indentación automática
                            model.Respuesta = xmlDoc.ToString();

                            // 🔐 También puedes eliminar o modificar nodos si quieres
                            // xmlDoc.Descendants("serialNumber").Remove();
                        }
                        catch (Exception ex)
                        {
                            ViewBag.Warning = $"Error procesando XML: {ex.Message}";
                        }
                    }
                    else
                        ViewBag.ContentType = "text/plain";
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

        public async Task<ActionResult> MainDashboard(HikVisionViewModel model1)
        {

            MainDashboardViewModel model = new MainDashboardViewModel();
            try
            {
                HikvisionService service = new HikvisionService();
                string url = $"{model1.APIServer}/ISAPI/System/deviceInfo";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler { Credentials = new NetworkCredential(model1.Usuario, model1.Password) };


                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var xmlString = await response.Content.ReadAsStringAsync();
                        var doc = XDocument.Parse(xmlString);
                        
                        XNamespace ns = doc.Root.GetDefaultNamespace();
                        var deviceInfo = doc.Root;
                        if (deviceInfo != null)
                        {
                            model.DeviceInfo.DeviceName = deviceInfo.Element(ns + "deviceName")?.Value;
                            model.DeviceInfo.SerialNumber = deviceInfo.Element(ns + "serialNumber")?.Value;
                            model.DeviceInfo.FirmwareVersion = deviceInfo.Element(ns + "firmwareVersion")?.Value;
                            model.DeviceInfo.Model = deviceInfo.Element(ns + "model")?.Value;
                        }

                    }
                }

                // 🔎 2. Obtener la informacion de la Red
                string xmlNetwork = await service.GetNetworkInterfaces(model1.APIServer, model1.Usuario, model1.Password);
                var docNet = XDocument.Parse(xmlNetwork);
                XNamespace nsNet = docNet.Root.GetDefaultNamespace();
                var interfaces = docNet.Descendants(nsNet + "NetworkInterface");

                foreach (var iface in interfaces)
                {
                    string id = iface.Element(nsNet + "id")?.Value;
                    model.NetworkInterfaces.Add(new NetworkInterface
                    {
                        Id = iface.Element(nsNet + "id")?.Value,  // El único identificador directo
                        IPAddress = iface.Element(nsNet + "IPAddress")?.Element(nsNet + "ipAddress")?.Value,
                        MacAddress = iface.Element(nsNet + "MACAddress")?.Value,
                        ConnectionType = "N/A",  // No hay campo connectionType
                        LinkStatus = iface.Element(nsNet + "linkStatus")?.Value ?? "N/A",
                        WirelessStatus = iface.Element(nsNet + "Wireless")?.Element(nsNet + "enabled")?.Value ?? "N/A"
                    });
                    if (model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).WirelessStatus == "true")
                    {
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).Name = iface.Element(nsNet + "Wireless").Element(nsNet + "ssid").Value;
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).ConnectionType = "Wireless";
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).LinkStatus = "True";
                    }
                    else
                    {
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).Name = "N/A";
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).ConnectionType = "LAN";
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).LinkStatus = "False";
                    }
                }

                // 🧍‍♂️ 3. Obtener la informacion de Personas
                (var people, int numMatches, int totalMatches) = await service.GetPeopleInfoAsync(model1.APIServer, model1.Usuario, model1.Password);
                model.Personas = people;
                model.NumPersonas = numMatches;
                model.TotalPersonas = totalMatches;
                //si todos los servicios en el controller se ejecutaron correctamente se devuelve IsActive.
                ViewBag.IsActive = true;
                return View(model);
            }
            catch(Exception ex)
            {
                ViewBag.Warning = $"❌: {ex.Message}";
                ViewBag.IsActive = false;
                return View(model);
            }
        }

    }
}
