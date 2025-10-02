using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        // Se encapsular acceso seguro del dispositivo a la sesión del usuario
        private string GetSessionValue(string key) => Session[key] as string ?? string.Empty;
        private void SetSessionValue(string key, string value) => Session[key] = value;
        /// <summary>
        /// Metodo encargado probar la comunicacion con un equipo
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            HikVisionViewModel model = new HikVisionViewModel
            {
                APIServer = GetSessionValue("HikApiServer"),
                Usuario = GetSessionValue("HikUser"),
                Password = GetSessionValue("HikPass"),
                Peticion = "GET /ISAPI/"
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
                // Guardar valores en sesión
                SetSessionValue("HikApiServer", model.APIServer);
                SetSessionValue("HikUser", model.Usuario);
                SetSessionValue("HikPass", model.Password);
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
        /// <summary>
        /// Metodo que se encarga de mostrar el Dasboard principal, que muestra un
        /// - CRUD de Personas   
        /// - La informacion de la RED
        /// - La informacion del dispositivo
        /// </summary>
        /// <param name="model1"></param>
        /// <returns></returns>
        public async Task<ActionResult> MainDashboard(HikVisionViewModel model1)
        {
            string apiServer = GetSessionValue("HikApiServer");
            string user = GetSessionValue("HikUser");
            string pass = GetSessionValue("HikPass");

            MainDashboardViewModel model = new MainDashboardViewModel();
            try
            {
                // 1. Obtiene la informacion de la Dispositivo (Control de Acceso DS-K1T320)
                HikvisionService service = new HikvisionService();
                string url = $"{apiServer}/ISAPI/System/deviceInfo";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler { Credentials = new NetworkCredential(user, pass) };
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
                string xmlNetwork = await service.GetNetworkInterfaces(apiServer, user, pass);
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
                        if(!string.IsNullOrEmpty(model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).Name))
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
                    else
                    {
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).Name = "N/A";
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).ConnectionType = "LAN";
                        model.NetworkInterfaces.FirstOrDefault(m => m.Id == id).LinkStatus = "False";
                    }
                }

                // 🧍‍♂️ 3. Obtener la informacion de Personas
                (var people, int numMatches, int totalMatches) = await service.GetPeopleInfoAsync(apiServer, user, pass);
                model.Personas = people;
                model.NumPersonas = numMatches;
                model.TotalPersonas = totalMatches;
                // Resultado a la vista: si todos los 3 servicios en el controller se ejecutaron correctamente se devuelve IsActive.
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

        /// <summary>
        /// Metodo encargado de mostrar el formulario para crear un usuario en el dispositivo hikvision
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult CreateUser()
        {
            return View(new HikvisionUserViewModel());
        }
        /// <summary>
        /// Metodo encargado de crear un usuario en el dispositivo hikvision
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> CreateUser(HikvisionUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string apiServer = GetSessionValue("HikApiServer");
            string user = GetSessionValue("HikUser");
            string pass = GetSessionValue("HikPass");

            if (string.IsNullOrEmpty(apiServer) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ViewBag.Warning = "⚠️ No hay sesión activa con credenciales del dispositivo. Inicia desde el formulario principal.";
                return RedirectToAction("Index");
            }

            var service = new HikvisionService();
            bool success = await service.CrearUsuarioDispositivoAsync(model, apiServer, user, pass);

            if (success)
            {
                TempData["Success"] = "✅ Usuario creado exitosamente en el dispositivo.";
                return RedirectToAction("MainDashboard");
            }

            ViewBag.Warning = "❌ No se pudo crear el usuario.";
            return View(model);
        }

        /// <summary>
        /// Metodo encargado de mostrar el formulario para editar un usuario por el id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("MainDashboard");

            string apiServer = GetSessionValue("HikApiServer");
            string user = GetSessionValue("HikUser");
            string pass = GetSessionValue("HikPass");

            var service = new HikvisionService();
            try
            {
                var usuario = await service.ObtenerUsuarioPorIdAsync(apiServer, user, pass, id);
                if (usuario == null)
                    return HttpNotFound();

                var model = new HikvisionUserViewModel
                {                    
                    EmployeeNo = usuario.employeeNo,
                    Name = usuario.name,
                    UserType = usuario.userType,
                    DoorRight = usuario.doorRight,
                    RoomNumber = usuario.roomNumber,
                    gender = usuario.gender,
                    BeginTime = DateTime.TryParseExact(usuario.valid.beginTime, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bTime) ? bTime : DateTime.Now,
                    EndTime = DateTime.TryParseExact(usuario.valid.endTime, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var eTime) ? eTime : DateTime.Now.AddYears(1),
                };
                GetTipoUsuarios();
                GetGeneros();
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Warning = $"❌ Error al obtener el usuario: {ex.Message}";
                return RedirectToAction("MainDashboard");
            }
        }

        /// <summary>
        /// Metodo encargado de actualizar un usuario en el dispositivo hikvision
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> EditUser(HikvisionUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string apiServer = GetSessionValue("HikApiServer");
            string user = GetSessionValue("HikUser");
            string pass = GetSessionValue("HikPass");

            var service = new HikvisionService();
            bool actualizado = await service.EditarUsuarioDispositivoAsync(model, apiServer, user, pass);

            if (actualizado)
            {
                TempData["Success"] = "✅ Usuario actualizado correctamente.";
                return RedirectToAction("MainDashboard");
            }

            ViewBag.Warning = "❌ No se pudo actualizar el usuario en el dispositivo.";
            GetTipoUsuarios();
            GetGeneros();
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteUser(string id)
        {
            string apiServer = GetSessionValue("HikApiServer");
            string user = GetSessionValue("HikUser");
            string pass = GetSessionValue("HikPass");

            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    TempData["Warning"] = "⚠️ El número de empleado es inválido.";
                    return RedirectToAction("MainDashboard");
                }

                var service = new HikvisionService();
                var eliminado = await service.EliminarUsuarioDispositivoAsync(apiServer, user, pass, id);

                if (eliminado)
                {
                    TempData["Success"] = $"✅ Usuario con ID {id} eliminado correctamente.";
                }
                else
                {
                    TempData["Warning"] = $"⚠️ No se pudo eliminar el usuario con ID {id}.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"⚠️ Error al eliminar el usuario: {ex.Message}";
            }

            return RedirectToAction("MainDashboard");
        }



        /// <summary>
        /// Metodo encargado de devolver un catalogo de generos a la vista.
        /// </summary>
        private void GetGeneros()
        {

            List<SelectListItem> Generos = new List<SelectListItem>();
            Generos.Add(new SelectListItem
            {
                Text = "Masculino",
                Value = "male",
                Selected = true
            });

            Generos.Add(new SelectListItem
            {
                Text = "Femenino",
                Value = "female"
            });

            ViewBag.Generos = Generos;
        }
        /// <summary>
        /// Metodo encargado de devolver un catalogo de tipo de usuario a la vista.
        /// </summary>
        private void GetTipoUsuarios()
        {

            List<SelectListItem> TipoUsuario = new List<SelectListItem>();
            TipoUsuario.Add(new SelectListItem
            {
                Text = "Usuario Normal",
                Value = "normal",
                Selected = true
            });

            TipoUsuario.Add(new SelectListItem
            {
                Text = "Visitante",
                Value = "visitor"
            });
            TipoUsuario.Add(new SelectListItem
            {
                Text = "Lista negra",
                Value = "blackList"
            });

            ViewBag.TipoUsuario = TipoUsuario;
        }
    }
}
