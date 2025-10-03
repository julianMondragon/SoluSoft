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

        // =====================================================================
        //                          EVENTOS (AcsEvent)
        // =====================================================================

        /// <summary>
        /// Obtiene eventos en un rango de tiempo usando /ISAPI/AccessControl/AcsEvent?format=json
        /// Maneja paginación por searchResultPosition y devuelve la lista consolidada.
        /// IMPORTANTE: requiere que existan AcsEventEnvelope/HikvisionEventViewModel/InfoItem en ViewModels.
        /// </summary>
        public async Task<(List<InfoItem> Items, int NumMatches, int TotalMatches)> GetEventsAsync(
                string apiServer, string user, string password,
                DateTime start, DateTime end,
                int pageSize = 50, int major = 5, int minor = 0)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new CredentialCache
                {
                    { new Uri(apiServer), "Digest", new NetworkCredential(user, password) }
                }
            };

            var all = new List<InfoItem>();
            int totalMatches = 0, numMatches = 0, pos = 0;

            using (var client = new HttpClient(handler))
            {
                var url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/AcsEvent?format=json";

                do
                {
                    var payload = new
                    {
                        AcsEventCond = new
                        {
                            searchID = "1",
                            searchResultPosition = pos,
                            maxResults = pageSize,
                            major = major,     // 5 = Control de acceso (0 = todos)
                            minor = minor,     // 0 = todos los subtipos
                            startTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = end.ToString("yyyy-MM-ddTHH:mm:ss")
                        }
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var resp = await client.PostAsync(url, content);
                    var body = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                        throw new Exception($"AcsEvent {(int)resp.StatusCode}: {body}");

                    // Deserializar con "envelope"
                    var envelope = JsonConvert.DeserializeObject<AcsEventEnvelope>(body);
                    var result = envelope?.AcsEvent;

                    if (result == null)
                        break;

                    numMatches = result.numOfMatches;
                    totalMatches = result.totalMatches;

                    if (result.InfoList != null && result.InfoList.Count > 0)
                        all.AddRange(result.InfoList);

                    pos += pageSize;

                } while (pos < totalMatches && totalMatches > 0);
            }

            return (all, numMatches, totalMatches);
        }

        /// <summary>
        /// Azúcar para obtener los eventos del día de hoy (00:00:00 a 23:59:59).
        /// </summary>
        public Task<(List<InfoItem> Items, int NumMatches, int TotalMatches)>GetEventsTodayAsync(string apiServer, string user, string password, int pageSize = 50, int major = 5, int minor = 0)
        {
            var start = DateTime.Today;
            var end = start.AddDays(1).AddSeconds(-1);
            return GetEventsAsync(apiServer, user, password, start, end, pageSize, major, minor);
        }

        /// <summary>
        /// (Opcional) Dado un employeeNo, resuelve el nombre con UserInfo/Search (útil cuando en eventos no viene "name")
        /// </summary>
        public async Task<string> ResolveNameByEmployeeNoAsync(string apiServer, string user, string password, string employeeNo)
        {
            if (string.IsNullOrWhiteSpace(employeeNo)) return string.Empty;

            var handler = new HttpClientHandler
            {
                Credentials = new CredentialCache
                {
                    { new Uri(apiServer), "Digest", new NetworkCredential(user, password) }
                }
            };

            using (var client = new HttpClient(handler))
            {
                var url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Search?format=json";
                var body = new
                {
                    UserInfoSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = 1,
                        EmployeeNoList = new[] { new { employeeNo = employeeNo } }
                    }
                };
                var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(url, content);
                var txt = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode) return string.Empty;

                var json = JObject.Parse(txt);
                var userInfo = json["UserInfoSearch"]?["UserInfo"]?.First;
                return userInfo?["name"]?.ToString() ?? string.Empty;
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