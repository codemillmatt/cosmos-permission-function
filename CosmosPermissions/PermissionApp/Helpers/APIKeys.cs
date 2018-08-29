using System;
namespace PermissionApp
{
    public static class APIKeys
    {
        public static string CosmosUrl = "https://trashtalk.documents.azure.com:443/";

        public static string MovieReviewDB = "moviereview-db";
        public static string MovieReviewCollection = "review-part";

        public static string BrokerUrlBase = "https://cosmospermission-v1.azurewebsites.net";
        public static string BrokerUrlPath = "api/MovieReviewPermissionPartitioned";
    }
}
