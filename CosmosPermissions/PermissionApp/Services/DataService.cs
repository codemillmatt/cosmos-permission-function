using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Permissions.Model;
using Microsoft.Azure.Documents.Client;
using Xamarin.Forms;

namespace PermissionApp
{
    public class DataService
    {
        DocumentClient docClient;

        public DataService()
        {
        }

        async Task Initialize()
        {
            // Check if the user is logged in or not
            var idService = DependencyService.Get<IIdentityService>();

            var authResult = await idService.GetCachedSignInToken();
            var accessToken = authResult.AccessToken;

            // Then hit the function to grab the correct permissions

            // Deserialize those permissions

            // Finally new up the client (
            docClient = new DocumentClient(new Uri(""), "");
        }

        public async Task<List<MovieReview>> LoadReviews()
        {
            await Initialize();


        }
    }
}
