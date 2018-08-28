using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;

using System.Threading.Tasks;
using System.Net;

namespace PermissionApp.Function
{
    public static class CollectionRead
    {
        static string publicUserId = System.Environment.GetEnvironmentVariable("PublicUserName");
        static string dbName = Environment.GetEnvironmentVariable("CosmosDBName");
        static string collectionName = Environment.GetEnvironmentVariable("CosmosDBCollection");

        [FunctionName("PublicCollectionRead")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            [CosmosDB(databaseName: "moviereview-db", collectionName: "reviews", ConnectionStringSetting = "CosmosConnectionString")] DocumentClient client,
            TraceWriter log)
        {
            try
            {
                var permission = await GetReadPermission(publicUserId, client, dbName, collectionName);

                // serialize the permission to be transmitted over the wire
                var serializedPermission = SerializePermission(permission);

                return new OkObjectResult(serializedPermission);
            }
            catch (Exception ex)
            {
                log.Error($"Something bad happened! {ex.Message}");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        static async Task<Permission> GetReadPermission(string userId, DocumentClient client, string databaseId, string collectionId)
        {
            Permission collectionPermission = new Permission();

            try
            {
                var collectionPermissionId = $"{userId}-publoc";
                collectionPermission = await client.ReadPermissionAsync(UriFactory.CreatePermissionUri(databaseId, userId, collectionPermissionId));
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // The permission was not found - either the user (and permission) doesn't exist or permission doesn't exist
                    await CreateUserIfNotExistAsync(userId, client, databaseId);
                    
                    // TODO: eventually instead of permissions per collection - make it permissions per doc / bit flipped

                    var newPermission = new Permission
                    {
                        PermissionMode = PermissionMode.Read,
                        Id = $"{userId}-publoc",
                        ResourceLink = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId).ToString()
                    };

                    collectionPermission = await client.CreatePermissionAsync(UriFactory.CreateUserUri(databaseId, userId), newPermission);
                }
                else { throw ex; }
            }

            return collectionPermission;
        }

        static async Task CreateUserIfNotExistAsync(string userId, DocumentClient client, string databaseId)
        {
            try
            {
                await client.ReadUserAsync(UriFactory.CreateUserUri(databaseId, userId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateUserAsync(UriFactory.CreateDatabaseUri(databaseId), new User { Id = userId });
                }
            }

        }

        static string SerializePermission(Permission permission)
        {
            string serializedPermission = "";

            using (var memStream = new MemoryStream())
            {
                permission.SaveTo(memStream);
                memStream.Position = 0;

                using (StreamReader sr = new StreamReader(memStream))
                {
                    serializedPermission = sr.ReadToEnd();
                }
            }

            return serializedPermission;
        }
    }
}
