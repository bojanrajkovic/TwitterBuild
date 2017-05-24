using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Oxide;

using static Oxide.Results;

namespace TwitterBuild
{
    class Tweeter
    {
        const UriComponents Components = UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path;

        static readonly HttpClient client = new HttpClient();
        static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        string consumerKey, tokenKey, consumerSecret, tokenSecret;

        public Tweeter(string consumerKey, string tokenKey, string consumerSecret, string tokenSecret)
        {
            this.consumerKey = consumerKey;
            this.tokenKey = tokenKey;
            this.consumerSecret = consumerSecret;
            this.tokenSecret = tokenSecret;
        }

        public async Task<Result<User, Exception>> GetUserAsync(string screenName)
        {
            Uri requestUri = new Uri("https://api.twitter.com/1.1/users/show.json?screen_name=" + Encode(screenName));
            var parameters = GenerateOAuthParameters(consumerKey, tokenKey);
            var signature = CreateOAuthSignature(
                consumerSecret,
                tokenSecret,
                "GET",
                requestUri,
                parameters
            );

            parameters.Add("oauth_signature", signature);

            var authHeader = CreateOAuthHeader(parameters);

            var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
            req.Headers.Add("User-Agent", "Swedish-Bot");
            req.Headers.Add("Authorization", authHeader);

            var res = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            var content = await res.Content.ReadAsStringAsync();

            try {
                res.EnsureSuccessStatusCode();
                return Ok<User, Exception>(JsonConvert.DeserializeObject<User>(content));
            } catch (Exception e) {
                return Err<User, Exception>(e);
            }
        }
        
        public async Task<Result<Tweet, Exception>> RetweetAsync(ulong id)
        {
            Uri requestUri = new Uri($"https://api.twitter.com/1.1/statuses/retweet/{id}.json");
            var parameters = GenerateOAuthParameters(consumerKey, tokenKey);
            var signature = CreateOAuthSignature(
                consumerSecret,
                tokenSecret,
                "POST",
                requestUri,
                parameters
            );

            parameters.Add("oauth_signature", signature);

            var authHeader = CreateOAuthHeader(parameters);

            var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
            req.Headers.Add("User-Agent", "Swedish-Bot");
            req.Headers.Add("Authorization", authHeader);

            var res = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            var content = await res.Content.ReadAsStringAsync();

            try {
                res.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<Tweet>(content);
            } catch (Exception e) {
                return e;
            }
        }

        public async Task<Result<IEnumerable<Tweet>, Exception>> GetTweetsForUserAsync(ulong id, ulong since)
        {
            Uri requestUri = new Uri($"https://api.twitter.com/1.1/statuses/user_timeline.json?user_id={id}&since_id={since}");
            var parameters = GenerateOAuthParameters(consumerKey, tokenKey);
            var signature = CreateOAuthSignature(
                consumerSecret,
                tokenSecret,
                "GET",
                requestUri,
                parameters
            );

            parameters.Add("oauth_signature", signature);

            var authHeader = CreateOAuthHeader(parameters);

            var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
            req.Headers.Add("User-Agent", "Swedish-Bot");
            req.Headers.Add("Authorization", authHeader);

            var res = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            var content = await res.Content.ReadAsStringAsync();

            try {
                res.EnsureSuccessStatusCode();
                return Ok<IEnumerable<Tweet>, Exception>(JsonConvert.DeserializeObject<IEnumerable<Tweet>>(content));
            } catch (Exception e) {
                return Err<IEnumerable<Tweet>, Exception>(e);
            }
        }

        public async Task<Result<Tweet, Exception>> PostTweetAsync(string status)
        {
            Uri requestUri = new Uri("https://api.twitter.com/1.1/statuses/update.json");

            var parameters = GenerateOAuthParameters(consumerKey, tokenKey);
            parameters.Add("status", status);

            var signature = CreateOAuthSignature(
                consumerSecret,
                tokenSecret,
                "POST",
                requestUri,
                parameters
            );

            parameters.Add("oauth_signature", signature);

            var authHeader = CreateOAuthHeader(parameters);

            var req = new HttpRequestMessage(HttpMethod.Post, requestUri) {
                Content = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("status", status)
                })
            };
            req.Headers.Add("User-Agent", "Swedish-Bot");
            req.Headers.Add("Authorization", authHeader);

            var res = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            var content = await res.Content.ReadAsStringAsync();

            try {
                res.EnsureSuccessStatusCode();
                return Ok<Tweet, Exception>(JsonConvert.DeserializeObject<Tweet>(content));
            } catch (Exception e) {
                return Err<Tweet, Exception>(e);
            }
        }

        static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        static Dictionary<string, string> GenerateOAuthParameters(string consumerKey, string token)
            => new Dictionary<string, string>() {
                {"oauth_consumer_key", consumerKey},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_timestamp", ((DateTimeOffset.UtcNow - UnixEpoch).Ticks / 10000000L).ToString("D")},
                {"oauth_nonce", GetNonce() },//new Random().Next(int.MinValue, int.MaxValue).ToString("X")},
                {"oauth_version", "1.0"},
                {"oauth_token", token}
            };

        static string CreateOAuthHeader(Dictionary<string, string> oauthParameters)
        {
            var sortedBits = oauthParameters.Keys.Where(k => k.StartsWith("oauth", StringComparison.OrdinalIgnoreCase))
                                            .OrderBy(k => k)
                                            .Select(k => $"{Encode(k)}=\"{Encode(oauthParameters[k])}\"");
            return $"OAuth {string.Join(", ", sortedBits)}";
        }


        static string CreateOAuthSignature(
            string consumerSecret,
            string tokenSecret,
            string httpMethod,
            Uri url,
            Dictionary<string, string> parameters
        )
        {
            var hmacKey = Encoding.UTF8.GetBytes($"{Encode(consumerSecret)}&{Encode(tokenSecret)}");
            var paramList = parameters.Select(x => (key: Encode(x.Key), value: Encode(x.Value)))
                                      .Concat(url.Query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(x => {
                                                     var s = x.Split('=');
                                                     return (key: s[0], value: s[1]);
                                                 }))
                                      .OrderBy(x => x.key).ThenBy(x => x.value)
                                      .Select(x => x.key + "=" + x.value);
            var parameterString = string.Join("&", paramList);
            var msg = Encoding.UTF8.GetBytes(
                string.Format("{0}&{1}&{2}",
                    httpMethod.ToString().ToUpperInvariant(),
                    Encode(url.GetComponents(Components, UriFormat.UriEscaped)),
                    Encode(parameterString)
                ));
            using (var signer = new HMACSHA1(hmacKey))
                return Convert.ToBase64String(signer.ComputeHash(msg));
        }

        static string GetNonce(int size = 32)
        {
            var nonce = new byte[size];
            rng.GetBytes(nonce);
            return Convert.ToBase64String(nonce);
        }

        static string Encode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return string.Join("", Encoding.UTF8.GetBytes(text)
                .Select(x => x < 0x80 && "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~"
                        .Contains(((char)x).ToString()) ? ((char)x).ToString() : ('%' + x.ToString("X2"))));
        }
    }
}
