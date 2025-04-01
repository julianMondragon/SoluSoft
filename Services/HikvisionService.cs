using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using TAS360.Models.ViewModel;

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

    }
}