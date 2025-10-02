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