using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Permissions.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PermissionApp.Function
{
    public static class MovieReviewPermissionFunction
    {
        static readonly string publicUserId = System.Environment.GetEnvironmentVariable("PublicUserName");
        static readonly string dbName = Environment.GetEnvironmentVariable("CosmosDBName");
        static readonly string collectionName = Environment.GetEnvironmentVariable("CosmosDBCollection");

        [FunctionName("MovieReviewPermission")]
        public async static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req,
            [CosmosDB(databaseName: "moviereview-db", collectionName: "reviews", ConnectionStringSetting = "CosmosConnectionString")] DocumentClient client,
            TraceWriter log)
        {
            try
            {
                // Figure out if we're logged in or not
                var userId = GetUserId(log);

                log.Info($"User ID: {userId}");

                var permissions = await GetReadPermission(userId, client, dbName, collectionName);

                // serialize the permission to be transmitted over the wire
                var serializedPermissionList = new List<string>();

                foreach (var permission in permissions)
                {
                    serializedPermissionList.Add(SerializePermission(permission));
                }

                return new OkObjectResult(serializedPermissionList);
            }
            catch (Exception ex)
            {
                log.Error($"***Something went wrong {ex.Message}");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        static async Task<List<Permission>> GetReadPermission(string userId, DocumentClient client, string databaseId, string collectionId)
        {
            List<Permission> movieReviewPermissions = new List<Permission>();
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            Uri userUri = UriFactory.CreateUserUri(databaseId, userId);

            IDocumentQuery<MovieReview> query = null;

            // Get the public or public + premium docs (depending on the user we're working with)
            if (userId == publicUserId)
                query = client.CreateDocumentQuery<MovieReview>(collectionUri).Where(mv => mv.IsPremium == false).AsDocumentQuery();
            else
                query = client.CreateDocumentQuery<MovieReview>(collectionUri).AsDocumentQuery();

            // Now loop through the results and create individual permissions for them
            while (query.HasMoreResults)
            {
                foreach (var movieReview in await query.ExecuteNextAsync<MovieReview>())
                {
                    Permission individualPermission = new Permission();
                    string permissionId = $"{userId}-{movieReview.Id}";

                    try
                    {
                        // the permission ID's format: {userID}-{documentID}
                        Uri permissionUri = UriFactory.CreatePermissionUri(databaseId, userId, permissionId);

                        individualPermission = await client.ReadPermissionAsync(permissionUri);

                        movieReviewPermissions.Add(individualPermission);
                    }
                    catch (DocumentClientException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            // The permission was not found - either the user (and permission) doesn't exist or permission doesn't exist
                            await CreateUserIfNotExistAsync(userId, client, databaseId);

                            var newPermission = new Permission
                            {
                                PermissionMode = PermissionMode.Read,
                                Id = permissionId,
                                ResourceLink = UriFactory.CreateDocumentUri(databaseId, collectionId, movieReview.Id).ToString()
                            };

                            individualPermission = await client.CreatePermissionAsync(userUri, newPermission);

                            movieReviewPermissions.Add(individualPermission);
                        }
                        else { throw ex; }

                    }
                }
            }

            return movieReviewPermissions;
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

        static string GetUserId(TraceWriter log)
        {
            if (Thread.CurrentPrincipal == null || !Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                log.Info($"Thread.CurrentPrincipal: {Thread.CurrentPrincipal}");

                return publicUserId;
            }

            var claimsPrincipal = (ClaimsPrincipal)Thread.CurrentPrincipal;

            var objectClaimTypeName = @"http://schemas.microsoft.com/identity/claims/objectidentifier";

            var objectClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == objectClaimTypeName);

            if (objectClaim == null)
                return publicUserId;
            else
                return objectClaim.Value;
        }
    }
}
