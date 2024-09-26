using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using XClient.Domain.Auth;
using XClient.Errors;
using XClient.Helpers;
using XClient.Logging;


namespace XClient.Authentication
{
    public class XAuth
    {
        private const string RequestTokenUrl = "https://api.x.com/oauth/request_token";
        private const string AuthUrl = "https://api.x.com/oauth/authorize";
        private const string AccessTokenUrl = "https://api.twitter.com/oauth/access_token";

        private readonly ILogger<XAuth> logger;
        private readonly HttpClient httpClient;

        public XAuth(ILogger<XAuth> logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            httpClient = httpClientFactory.CreateClient(nameof(XClient));
        }


        // Create a user authorization url.
        public async Task<Result<string>> CreateAuthUrlAsync(string apiKey, string apiSecret, string callbackUrl, CancellationToken cancellationToken=default)
        {
            SortedDictionary<string, string> authParameters = new()
            {
                { "oauth_callback", callbackUrl }
            };

            SortedDictionary<string, string> urlParameters = new()
            {
                { "x_auth_access_type", "write" }  // access type of write also allows read
            };

            logger.LogInformation( "Generate X oauth header");
            string authorizationHeader = OAuthHeader.Authorization(HttpMethod.Post.ToString(), RequestTokenUrl, apiKey, apiSecret, accessTokenSecret: null, authParameters, urlParameters);

            // set authorization header
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeader);
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            // append parameters to uri
            string uri = $"{RequestTokenUrl}?{string.Join("&", urlParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";

            // make request to get token
            var result = await HttpRequestHelper.SendHttpRequestAsync<Stream>(httpClient, uri, HttpMethod.Post, logger, content: null, cancellationToken);
            logger.LogResult(result, "Get X token");
            if (!result.Success)
                return result.Error;

            Stream responseStream = result.Value;
            string responseString = new StreamReader(responseStream).ReadToEnd();

            // extract oauth token
            var extractTokenResult = ExtractOAuthToken(responseString);
            logger.LogResult(extractTokenResult, "Extract X oauth token");
            if (!extractTokenResult.Success)
                return extractTokenResult.Error
                    .SetClientMessage("Could not authenticate you");

            string oauthToken = extractTokenResult.Value;

            // generate authorization url
            string clientAuthUrl = $"{AuthUrl}?oauth_token={oauthToken}";
            return clientAuthUrl;
        }


        // Extract oauth token from the response text.
        private static Result<string> ExtractOAuthToken(string responseText)
        {
            var queryParams = HttpUtility.ParseQueryString(responseText);
            string? oauthToken = queryParams["oauth_token"];
            if (oauthToken == null)
                return new ArgumentError("oauth_token not found in response");

            return oauthToken;
        }


        // Get access token.
        public async Task<Result<XAccountCredentials>> GetAccessTokenAsync(string apiKey, string apiSecret, string oauthToken, string oauthVerifier, CancellationToken cancellationToken = default)
        {
            SortedDictionary<string, string> authParameters = new()
            {
                { "oauth_token", oauthToken },
                { "oauth_verifier", oauthVerifier }
            };

            // set authorization header
            var authorizationHeader = OAuthHeader.Authorization(HttpMethod.Post.ToString(), AccessTokenUrl, apiKey, apiSecret, accessTokenSecret: null, authParameters);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeader);
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            // get access token
            var result = await HttpRequestHelper.SendHttpRequestAsync<Stream>(httpClient, AccessTokenUrl, HttpMethod.Post, logger, content: null, cancellationToken);
            logger.LogResult(result, "Get X access token");
            if (!result.Success)
                return result.Error;

            Stream responseStream = result.Value;
            string responseString = new StreamReader(responseStream).ReadToEnd();

            // extract account credentials
            var credentialsResult = ExtractAccountCredentials(responseString);
            logger.LogResult(credentialsResult, "Extract X account credentials");
            if (!credentialsResult.Success)
                return credentialsResult.Error
                    .SetClientMessage("Could not authenticate you");

            return credentialsResult.Value;
        }


        // Extract account credentials from the response text.
        private static Result<XAccountCredentials> ExtractAccountCredentials(string responseString)
        {
            // extract oauth_token, oauth_token_secret, user_id, and screen_name
            var queryParams = HttpUtility.ParseQueryString(responseString);
            string? oauthToken = queryParams["oauth_token"];
            string? oauthTokenSecret = queryParams["oauth_token_secret"];
            string? userId = queryParams["user_id"];
            string? screenName = queryParams["screen_name"];

            // return an argument error if any of the above values is null or empty
            if (string.IsNullOrEmpty(oauthToken) || string.IsNullOrEmpty(oauthTokenSecret) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(screenName))
                return new ArgumentError("Could not extract account credentials");

            return new XAccountCredentials(userId, screenName, oauthToken, oauthTokenSecret);
        }

    }
}
