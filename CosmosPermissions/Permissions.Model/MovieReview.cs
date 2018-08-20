using System;
using Newtonsoft.Json;

namespace Permissions.Model
{
    public class MovieReview
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("movieName")]
        public string MovieName { get; set; }

        [JsonProperty("starRating")]
        public int StarRating { get; set; }

        [JsonProperty("reviewText")]
        public string ReviewText { get; set; }

        [JsonProperty("isPremium")]
        public bool IsPremium { get; set; }
    }
}
