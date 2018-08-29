using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Permissions.Model;
using Microsoft.Azure.Documents.Client;
using Xamarin.Forms;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;
using System.Runtime.InteropServices;

namespace PermissionApp
{
    public class DataService
    {
        DocumentClient docClient;
        List<string> permittedDocUris = new List<string>();

        bool notAuthenticated = true;

        async Task Initialize()
        {
            if (docClient != null)
                return;

            // Check if the user is logged in or not
            var idService = DependencyService.Get<IIdentityService>();

            var authResult = await idService.GetCachedSignInToken();
            string accessToken = authResult?.AccessToken;

            notAuthenticated = string.IsNullOrEmpty(accessToken);

            // Then hit the function to grab the correct permissions
            var functionService = new FunctionService();

            var token = await functionService.GetPermissionToken(accessToken);
            docClient = new DocumentClient(new Uri(APIKeys.CosmosUrl), token);
        }

        public async Task<List<MovieReview>> LoadReviews()
        {
            await Initialize();

            var mvl = new List<MovieReview>();

            var feedOptions = new FeedOptions() { MaxItemCount = -1 };

            if (notAuthenticated)
                feedOptions.PartitionKey = new PartitionKey(false);

            var collectionUri = UriFactory.CreateDocumentCollectionUri(APIKeys.MovieReviewDB, APIKeys.MovieReviewCollection);

            var query = docClient.CreateDocumentQuery<MovieReview>(collectionUri, feedOptions).AsDocumentQuery();

            while (query.HasMoreResults)
            {
                mvl.AddRange(await query.ExecuteNextAsync<MovieReview>());
            }

            return mvl;
        }
    }
}
