using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;

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

    }
}