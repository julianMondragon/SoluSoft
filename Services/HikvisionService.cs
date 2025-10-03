using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using TAS360.Models.ViewModel;
using System.Linq;
using System.Diagnostics;
using System.Globalization;

namespace TAS360.Services
{
    public class HikvisionService : IHikvisionService
    {
        private static Uri BuildBaseUri(string apiServer)
        {
            if (string.IsNullOrWhiteSpace(apiServer))
                throw new ArgumentException("El parámetro apiServer no puede estar vacío.", nameof(apiServer));

            var trimmed = apiServer.Trim();
            if (!trimmed.Contains("://"))
            {
                trimmed = $"http://{trimmed}";
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var baseUri))
            {
                throw new ArgumentException("La dirección del servidor Hikvision no es válida.", nameof(apiServer));
            }

            return baseUri;
        }

        public async Task<string> ConsumeISAPI(string url, string user, string pass)
        {
            var endpointUri = BuildBaseUri(url);
            var handler = new HttpClientHandler()
            {
                Credentials = new CredentialCache
                {
                    { endpointUri, "Digest", new NetworkCredential(user, pass) }
                }
            };

            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync(endpointUri);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                else
                    throw new Exception($"Error ISAPI: {response.StatusCode}");
            }
        }

        public async Task<string> GetDeviceInfo(string apiServer, string user, string pass)
        {
            var baseUri = BuildBaseUri(apiServer);
            var requestUri = new Uri(baseUri, "/ISAPI/System/deviceInfo");
            return await ConsumeISAPI(requestUri.ToString(), user, pass);
        }

        public async Task<string> GetNetworkInterfaces(string apiServer, string user, string pass)
        {
            var baseUri = BuildBaseUri(apiServer);
            var requestUri = new Uri(baseUri, "/ISAPI/System/Network/interfaces");
            return await ConsumeISAPI(requestUri.ToString(), user, pass);
        }

        public async Task<JObject> GetEventsAsync(string apiServer, string user, string password, object eventSearch = null)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(user, password)
            };

            using (var client = new HttpClient(handler))
            {
                var baseUri = BuildBaseUri(apiServer);
                var requestUri = new Uri(baseUri, "/ISAPI/AccessControl/AcsEvent?format=json");

                var body = eventSearch ?? new
                {
                    AcsEventSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 30
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(body);

                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    var response = await client.PostAsync(requestUri, content).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("No se pudo obtener la información de eventos del dispositivo.");
                    }

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JObject.Parse(responseBody);
                }
            }
        }




        /// <summary>
        ///  Este endpoint regresa una lista de personas registradas en el dispositivo, numero de personas y total de personas..
        /// </summary>
        /// <param name="apiServer"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<(List<UserInfo>, int, int)> GetPeopleInfoAsync(string apiServer, string user, string password)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(user, password)
            };

            using (var client = new HttpClient(handler))
            {
                var baseUri = BuildBaseUri(apiServer);
                var requestUri = new Uri(baseUri, "/ISAPI/AccessControl/UserInfo/Search?format=json");
                var body = new
                {
                    UserInfoSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 30
                    }
                };
                var jsonBody = JsonConvert.SerializeObject(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, content);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("No se pudo obtener la información de personas.");

                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonData = JObject.Parse(responseBody);

                var numMatches = (int)jsonData["UserInfoSearch"]?["numOfMatches"];
                var totalMatches = (int)jsonData["UserInfoSearch"]?["totalMatches"];

                var people = jsonData["UserInfoSearch"]?["UserInfo"]
                                ?.ToObject<List<UserInfo>>() ?? new List<UserInfo>();

                return (people, numMatches, totalMatches);
            }
        }
        /// <summary>
        /// Este endpoint agrega un usuario al dispositivo hikvision DS-K1T320
        /// </summary>
        /// <param name="model"></param>
        /// <param name="apiServer"></param>
        /// <param name="usuario"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> CrearUsuarioDispositivoAsync(HikvisionUserViewModel model, string apiServer, string usuario, string password)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(usuario, password)
            };

            var payload = new
            {
                UserInfo = new
                {
                    employeeNo = model.EmployeeNo,
                    name = model.Name,
                    userType = model.UserType,
                    Valid = new
                    {
                        enable = true,
                        beginTime = model.BeginTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = model.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        timeType = "local"
                    },
                    doorRight = model.DoorRight,
                    roomNumber = model.RoomNumber
                }
            };

            using (var client = new HttpClient(handler))
            {
                var requestUri = new Uri(BuildBaseUri(apiServer), "/ISAPI/AccessControl/UserInfo/Record?format=json");
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUri, content);
                return response.IsSuccessStatusCode;
            }
        }

        public async Task<UserInfo> ObtenerUsuarioPorIdAsync(string apiServer, string user, string password, string employeeNo)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(user, password)
            };

            using (var client = new HttpClient(handler))
            {
                var requestUri = new Uri(BuildBaseUri(apiServer), "/ISAPI/AccessControl/UserInfo/Search?format=json");

                var body = new
                {
                    UserInfoSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 100
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, content);
                if (!response.IsSuccessStatusCode)
                    throw new Exception("No se pudo obtener la lista de usuarios.");

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);

                var usuarios = json["UserInfoSearch"]?["UserInfo"]?.ToObject<List<UserInfo>>();
                return usuarios?.FirstOrDefault(u => u.employeeNo == employeeNo);
            }
        }

        public async Task<bool> EditarUsuarioDispositivoAsync(HikvisionUserViewModel model, string apiServer, string usuario, string password)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(usuario, password)
            };

            var payload = new
            {
                UserInfo = new
                {
                    employeeNo = model.EmployeeNo,
                    name = model.Name,
                    userType = model.UserType,
                    Valid = new
                    {
                        enable = true,
                        beginTime = model.BeginTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = model.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        timeType = "local"
                    },
                    doorRight = model.DoorRight,
                    roomNumber = model.RoomNumber,
                    gender = model.gender
                }
            };

            using (var client = new HttpClient(handler))
            {
                var requestUri = new Uri(BuildBaseUri(apiServer), "/ISAPI/AccessControl/UserInfo/Modify?format=json");
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(requestUri, content);
                return response.IsSuccessStatusCode;
            }
        }
        public async Task<bool> EliminarUsuarioDispositivoAsync(string apiServer, string usuario, string password, string employeeNo)
        {
            throw new Exception($"404 Method not found");
            if (string.IsNullOrWhiteSpace(employeeNo))
                throw new ArgumentException("El número de empleado es obligatorio");

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(usuario, password)
            };

            var body = new
            {
                UserInfoDelCond = new
                {
                    employeeNo = new[] { employeeNo }
                }
            };

            using (var client = new HttpClient(handler))
            {
                var requestUri = new Uri(BuildBaseUri(apiServer), "/ISAPI/AccessControl/UserInfo/Delete?format=json");
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(requestUri, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error al eliminar usuario: {response.StatusCode} - {responseBody}");

                return true;
            }
        }

        /// <summary>
        /// Consulta el API de eventos de Hikvision y proyecta la respuesta al modelo de presentación.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos para leer eventos.</param>
        /// <param name="pass">Contraseña del usuario con permisos.</param>
        /// <param name="start">Fecha y hora de inicio del intervalo a consultar.</param>
        /// <param name="end">Fecha y hora de fin del intervalo a consultar.</param>
        /// <param name="maxResults">Número máximo de eventos que se desean recuperar.</param>
        /// <returns>Lista inmutable de eventos representados mediante <see cref="HikvisionEventViewModel"/>.</returns>
        public async Task<IReadOnlyCollection<HikvisionEventViewModel>> GetEventsAsync(string apiServer, string user, string pass, DateTime start, DateTime end, int maxResults = 100)
        {
            if (string.IsNullOrWhiteSpace(apiServer))
                throw new ArgumentException("La dirección del servidor no puede ser vacía", nameof(apiServer));

            if (string.IsNullOrWhiteSpace(user))
                throw new ArgumentException("El usuario es obligatorio", nameof(user));

            if (string.IsNullOrWhiteSpace(pass))
                throw new ArgumentException("La contraseña es obligatoria", nameof(pass));

            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxResults), "El número máximo de resultados debe ser mayor que cero");

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(user, pass)
            };

            using (var client = new HttpClient(handler))
            {
                var baseUri = BuildBaseUri(apiServer);
                var requestUri = new Uri(baseUri, "/ISAPI/AccessControl/AcsEvent?format=json");
                var payload = new
                {
                    AcsEventCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = maxResults,
                        major = 5,
                        minor = 0,
                        startTime = start.ToString("yyyy-MM-dd'T'HH:mm:ss",CultureInfo.CurrentCulture),
                        endTime = end.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.CurrentCulture)
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var reason = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new Exception($"No se pudo obtener el historial de eventos: {(int)response.StatusCode} - {reason}");
                }

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var parsed = JObject.Parse(body);
                var eventsToken = parsed["AcsEvent"]?["InfoList"];
                if (eventsToken == null)
                    return Array.Empty<HikvisionEventViewModel>();

                if (eventsToken.Type != JTokenType.Array)
                {
                    eventsToken = new JArray(eventsToken);
                }

                var result = new List<HikvisionEventViewModel>();
                foreach (var item in eventsToken)
                {
                    DateTime? eventTime = null;
                    var eventTimeString = item.Value<string>("eventTime");
                    if (!string.IsNullOrWhiteSpace(eventTimeString) && DateTime.TryParse(eventTimeString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
                    {
                        eventTime = parsedDate;
                    }

                    result.Add(new HikvisionEventViewModel
                    {
                        totalMatches = item.Value<int>("totalMatches"),
                        serchID = item.Value<string>("serchID"),
                        InfoList = new List<InfoList>() 
                        {
                            new InfoList(){
                            serialNo = item.Value<int>("serialNo"),
                            cardType = item.Value<int>("cardType"),
                            currentVerifyMode = item.Value<string>("currentVerifyMode"),
                            time = item.Value<string>("time"),
                            name = item.Value<string>("name"),
                            employeeNoString = item.Value<string>("employeeNoString"),
                            doorNo = item.Value<int>("doorNo"),
                            minor = item.Value<int>("menior"),
                            major = item.Value<int>("major")
                            } 
                        }
                    });
                    
                }

                return result;
            }
        }

        public async Task<DeviceCheckResult> CheckStatusAsync(string baseUrl, string user, string pass, int timeoutMs = 2500)
        {
            var handler = new HttpClientHandler { Credentials = new NetworkCredential(user, pass) };
            using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(timeoutMs) })
            {
                var baseUri = BuildBaseUri(baseUrl);
                var requestUri = new Uri(baseUri, "/ISAPI/Security/userCheck");
                var sw = Stopwatch.StartNew();
                try
                {
                    var resp = await http.GetAsync(requestUri).ConfigureAwait(false);
                    sw.Stop();
                    return new DeviceCheckResult
                    {
                        Online = resp.IsSuccessStatusCode,
                        StatusCode = (int)resp.StatusCode,
                        LatencyMs = sw.ElapsedMilliseconds
                    };
                }
                catch (Exception ex)
                {
                    return new DeviceCheckResult { Online = false, Error = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
                }
            }
        }

    }
}