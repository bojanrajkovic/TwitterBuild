using Newtonsoft.Json;

namespace TwitterBuild
{
    public class Tweet
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }
    }
}