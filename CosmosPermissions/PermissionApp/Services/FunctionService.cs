using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace PermissionApp
{
    public class FunctionService
    {
        public FunctionService()
        {
        }

        public async Task<string> GetPermissionToken(string accessToken)
        {
            var baseUri = new Uri(APIKeys.BrokerUrlBase);
            var client = new HttpClient { BaseAddress = baseUri };

            var brokerUrl = new Uri(baseUri, APIKeys.BrokerUrlPath);

            var request = new HttpRequestMessage(HttpMethod.Get, brokerUrl);

            // Here check if there's a token or not
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var token = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());

            return token;

            //Permission permission = new Permission();
            //using (var memStream = new MemoryStream())
            //{
            //    using (var streamWriter = new StreamWriter(memStream))
            //    {
            //        streamWriter.Write(serialized);
            //        streamWriter.Flush();
            //        memStream.Position = 0;

            //        permission = Permission.LoadFrom<Permission>(memStream);

            //        streamWriter.Close();
            //    }
            //}

            //return permission;
        }

        public async Task<List<Permission>> GetPermissions(string accessToken)
        {
            var allPermissions = new List<Permission>();

            var baseUri = new Uri(APIKeys.BrokerUrlBase);
            var client = new HttpClient { BaseAddress = baseUri };

            var brokerUrl = new Uri(baseUri, APIKeys.BrokerUrlPath);

            var request = new HttpRequestMessage(HttpMethod.Get, brokerUrl);

            // Here check if there's a token or not
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var serializedResponse = await response.Content.ReadAsStringAsync();

            var serializedPermissionList = JsonConvert.DeserializeObject<List<string>>(serializedResponse);

            foreach (var serializedPermissionString in serializedPermissionList)
            {
                using (var memStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memStream))
                    {
                        streamWriter.Write(serializedPermissionString);
                        streamWriter.Flush();
                        memStream.Position = 0;

                        var permission = Permission.LoadFrom<Permission>(memStream);

                        streamWriter.Close();

                        allPermissions.Add(permission);
                    }
                }
            }

            return allPermissions;
        }
    }
}
