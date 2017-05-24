using Newtonsoft.Json;

namespace TwitterBuild
{
    public class User
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }
    }
}