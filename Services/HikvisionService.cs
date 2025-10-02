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
    public class HikvisionService
    {
        public async Task<string> ConsumeISAPI(string url, string user, string pass)
        {
            var handler = new HttpClientHandler()
            {
                Credentials = new CredentialCache
                {
                    { new Uri(url), "Digest", new NetworkCredential(user, pass) }
                }
            };

            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();
                else
                    throw new Exception($"Error ISAPI: {response.StatusCode}");
            }
        }

        public async Task<string> GetDeviceInfo(string apiServer, string user, string pass)
        {
            string url = $"{apiServer}/ISAPI/System/deviceInfo";
            return await ConsumeISAPI(url, user, pass);
        }

        public async Task<string> GetNetworkInterfaces(string apiServer, string user, string pass)
        {
            string url = $"{apiServer}/ISAPI/System/Network/interfaces";
            return await ConsumeISAPI(url, user, pass);
        }



        /// <summary>
        /// Obtiene los eventos generados durante el día actual en el dispositivo Hikvision.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos de consulta.</param>
        /// <param name="password">Contraseña del usuario.</param>
        /// <param name="maxResults">Cantidad máxima de registros a recuperar.</param>
        /// <returns>Resultado con la colección de eventos y metadatos de coincidencias.</returns>
        public async Task<HikvisionEventResult> GetEventsForTodayAsync(string apiServer, string user, string password, int maxResults = 50)
        {
            var start = DateTime.Now.Date;
            var end = start.AddDays(1).AddTicks(-1);
            return await GetEventsAsync(apiServer, user, password, start, end, maxResults).ConfigureAwait(false);
        }

        /// <summary>
        /// Realiza una búsqueda de eventos dentro de un rango de fechas determinado.
        /// </summary>
        /// <param name="apiServer">Dirección base del servidor ISAPI.</param>
        /// <param name="user">Usuario con permisos de consulta.</param>
        /// <param name="password">Contraseña del usuario.</param>
        /// <param name="startTime">Fecha y hora inicial del rango.</param>
        /// <param name="endTime">Fecha y hora final del rango.</param>
        /// <param name="maxResults">Cantidad máxima de registros a recuperar.</param>
        /// <returns>Resultado con la colección de eventos y metadatos asociados.</returns>
        /// <exception cref="Exception">Se lanza cuando la petición al dispositivo falla.</exception>
        public async Task<HikvisionEventResult> GetEventsAsync(string apiServer, string user, string password, DateTime startTime, DateTime endTime, int maxResults = 50)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(user, password)
            };

            using (var client = new HttpClient(handler))
            {
                var url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/AcsEvent?format=json";
                var payload = new
                {
                    AcsEventSearchCond = new
                    {
                        searchID = "1",
                        searchResultPosition = 0,
                        maxResults = maxResults,
                        major = 0,
                        minor = 0,
                        startTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss")
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new Exception($"No se pudieron obtener los eventos. Código: {response.StatusCode}. Detalle: {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsonData = JObject.Parse(responseBody);
                var resultNode = jsonData["AcsEventSearchResult"];

                var events = ParseEvents(resultNode?["AcsEvent"]);
                var numMatches = resultNode?["numOfMatches"]?.Value<int?>() ?? events.Count;
                var totalMatches = resultNode?["totalMatches"]?.Value<int?>() ?? events.Count;

                return new HikvisionEventResult
                {
                    Events = events,
                    NumOfMatches = numMatches,
                    TotalMatches = totalMatches
                };
            }
        }

        /// <summary>
        /// Convierte la respuesta JSON de eventos en una colección tipada.
        /// </summary>
        /// <param name="token">Token JSON que contiene el arreglo de eventos.</param>
        /// <returns>Lista de eventos normalizados.</returns>
        private List<HikvisionEventViewModel> ParseEvents(JToken token)
        {
            var events = new List<HikvisionEventViewModel>();
            if (token == null)
            {
                return events;
            }

            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    var parsed = ParseEvent(item);
                    if (parsed != null)
                    {
                        events.Add(parsed);
                    }
                }
            }
            else if (token is JObject obj)
            {
                var parsed = ParseEvent(obj);
                if (parsed != null)
                {
                    events.Add(parsed);
                }
            }

            return events
                .OrderByDescending(e => ParseSortableDate(e.EventTime) ?? DateTime.MinValue)
                .ToList();
        }

        /// <summary>
        /// Convierte un token JSON individual de evento a un modelo fuertemente tipado.
        /// </summary>
        /// <param name="token">Token JSON del evento.</param>
        /// <returns>Instancia de <see cref="HikvisionEventViewModel"/> o null si el token es inválido.</returns>
        private HikvisionEventViewModel ParseEvent(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            var major = token.Value<string>("major") ?? token.Value<string>("majorEventType");
            var minor = token.Value<string>("minor") ?? token.Value<string>("minorEventType");
            var eventType = token.Value<string>("eventType") ?? token.Value<string>("type");
            var description = token.Value<string>("eventDescription") ?? token.Value<string>("description");

            if (string.IsNullOrWhiteSpace(description))
            {
                description = BuildEventDescription(eventType, major, minor);
            }

            var rawTime = token.Value<string>("time") ?? token.Value<string>("eventTime");
            var normalizedTime = NormalizeEventTime(rawTime);

            return new HikvisionEventViewModel
            {
                EmployeeNo = token.Value<string>("employeeNoString") ?? token.Value<string>("employeeNo"),
                PersonName = token.Value<string>("name") ?? token.Value<string>("personName"),
                CardNumber = token.Value<string>("cardNo") ?? token.Value<string>("credentialNo"),
                DoorName = token.Value<string>("doorName") ?? token.Value<string>("doorNo") ?? token.Value<string>("doorID"),
                EventType = eventType,
                EventDescription = description,
                EventCode = BuildEventCode(major, minor),
                EventTime = normalizedTime
            };
        }

        /// <summary>
        /// Normaliza el texto del evento a una representación consistente.
        /// </summary>
        /// <param name="eventType">Tipo general del evento.</param>
        /// <param name="major">Código mayor del evento.</param>
        /// <param name="minor">Código menor del evento.</param>
        /// <returns>Descripción amigable del evento.</returns>
        private string BuildEventDescription(string eventType, string major, string minor)
        {
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                return eventType;
            }

            if (!string.IsNullOrWhiteSpace(major) || !string.IsNullOrWhiteSpace(minor))
            {
                return $"Evento {BuildEventCode(major, minor)}".Trim();
            }

            return "Evento";
        }

        /// <summary>
        /// Genera un código combinando los valores mayor-menor del evento.
        /// </summary>
        /// <param name="major">Código mayor del evento.</param>
        /// <param name="minor">Código menor del evento.</param>
        /// <returns>Código combinado, o cadena vacía si no hay datos.</returns>
        private string BuildEventCode(string major, string minor)
        {
            var parts = new[] { major, minor }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return parts.Length > 0 ? string.Join("-", parts) : string.Empty;
        }

        /// <summary>
        /// Normaliza la fecha del evento a formato ISO 8601 compatible con JavaScript.
        /// </summary>
        /// <param name="rawTime">Cadena original devuelta por la API.</param>
        /// <returns>Cadena en formato ISO 8601 o la original si no se puede convertir.</returns>
        private string NormalizeEventTime(string rawTime)
        {
            if (string.IsNullOrWhiteSpace(rawTime))
            {
                return rawTime;
            }

            if (DateTime.TryParse(rawTime, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedUtc))
            {
                return parsedUtc.ToLocalTime().ToString("o");
            }

            if (DateTime.TryParse(rawTime, out var parsedLocal))
            {
                return parsedLocal.ToString("o");
            }

            return rawTime;
        }

        /// <summary>
        /// Intenta convertir una cadena en una fecha para ordenar los eventos.
        /// </summary>
        /// <param name="value">Cadena de fecha a interpretar.</param>
        /// <returns>Instancia de <see cref="DateTime"/> cuando es posible, null en caso contrario.</returns>
        private DateTime? ParseSortableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, out parsed))
            {
                return parsed;
            }

            return null;
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
                var url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Search?format=json";
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

                var response = await client.PostAsync(url, content);
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
                string url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Record?format=json";
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
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
                string url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Search?format=json";

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

                var response = await client.PostAsync(url, content);
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
                string url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Modify?format=json";
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(url, content);
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
                string url = $"{apiServer.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/Delete?format=json";
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Error al eliminar usuario: {response.StatusCode} - {responseBody}");

                return true;
            }
        }

        public async Task<DeviceCheckResult> CheckStatusAsync(string baseUrl, string user, string pass, int timeoutMs = 2500)
        {
            var handler = new HttpClientHandler { Credentials = new NetworkCredential(user, pass) };
            using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(timeoutMs) })
            {
                var url = $"{baseUrl.TrimEnd('/')}/ISAPI/Security/userCheck";
                var sw = Stopwatch.StartNew();
                try
                {
                    var resp = await http.GetAsync(url).ConfigureAwait(false);
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
        public sealed class DeviceCheckResult
        {
            public bool Online { get; set; }
            public int? StatusCode { get; set; }
            public long LatencyMs { get; set; }
            public string Error { get; set; }
        }

    }
}