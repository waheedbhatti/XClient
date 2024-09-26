using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using XClient.Authentication;
using XClient.Domain;
using XClient.Domain.Auth;
using XClient.Errors;
using XClient.Helpers;
using XClient.Logging;

namespace XClient
{
    public class XClient : IXClient
    {
        public const string ExternalResourceClientName = nameof(XClient) + "_EXTERNAL";

        private const string UserInfoUrl = "https://api.x.com/2/users/me";
        private const string UploadMediaUri = "https://upload.x.com/1.1/media/upload.json";
        private const string TweetsUrl = "https://api.x.com/2/tweets";

        private readonly ILogger<XClient> logger;
        private readonly XAuth auth;
        private readonly HttpClient apiClient;
        private readonly HttpClient externalResourcesClient;

        public XClient(ILogger<XClient> logger, XAuth auth, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.auth = auth;
            this.apiClient = httpClientFactory.CreateClient(nameof(XClient));
            this.externalResourcesClient = httpClientFactory.CreateClient(ExternalResourceClientName);
        }


        #region User information

        public async Task<Result<XUser>> GetUserAsync(string apiKey, string apiSecret, string accessToken, string accessTokenSecret, CancellationToken cancellationToken = default)
        {
            SortedDictionary<string, string> queryParams = new()
            {
                { "user.fields", "id,name,username,profile_image_url" }
            };

            var result = await SendHttpRequestAsync<XData<XUser>>(UserInfoUrl, HttpMethod.Get, apiKey, apiSecret, accessToken, accessTokenSecret, queryParams, cancellationToken: cancellationToken);
            logger.LogResult(result, "Get X user");
            if (!result.Success)
                return result.Error;

            return result.Value.Data;
        }


        #endregion


        #region Auth

        // Create a url to authorize a user.
        public async Task<Result<string>> CreateAuthUrlAsync(string apiKey, string apiSecret, string callbackUrl, CancellationToken cancellationToken=default)
        {
            var result = await auth.CreateAuthUrlAsync(apiKey, apiSecret, callbackUrl, cancellationToken);
            logger.LogResult(result, "Create X auth url");
            if (!result.Success)
                return result.Error;

            return result.Value;
        }


        // Get an access token.
        public async Task<Result<XAccountCredentials>> GetAccessTokenAsync(string apiKey, string apiSecret, string oauthToken, string oauthVerifier, CancellationToken cancellationToken = default)
        {
            var result = await auth.GetAccessTokenAsync(apiKey, apiSecret, oauthToken, oauthVerifier, cancellationToken);
            logger.LogResult(result, "Get X access token");
            if (!result.Success)
                return result.Error;

            return result.Value;
        }

        #endregion


        #region Upload media

        public async Task<Result<XMedia>> UploadMediaAsync(string mediaUri, string mediaType, string apiKey, string apiSecret, string accessToken, string accessTokenSecret, CancellationToken cancellationToken=default)
        {
            // get media category
            string mediaCategory;
            if (mediaType == PostMediaTypes.Image)
                mediaCategory = "tweet_image";
            else if (mediaType == PostMediaTypes.Video)
                mediaCategory = "tweet_video";
            else
                return new ArgumentError($"Unsupported media type: {mediaType}");

            // begin fetching media
            long mediaLength;
            HttpResponseMessage? httpResponse;
            try
            {
                // do not use api client as it contains authorization headers which will be exposed to 
                // the external resource
                httpResponse = await externalResourcesClient.GetAsync(mediaUri, cancellationToken);
            }
            catch (HttpRequestException requestException)
            {
                logger.LogWarning(requestException, $"Failed to fetch media from uri: {mediaUri}");
                return new Error(requestException).SetClientMessage("Failed to fetch media");
            }

            // try to get media length
            if (httpResponse.Content.Headers.ContentLength.HasValue)
                mediaLength = httpResponse.Content.Headers.ContentLength.Value;
            else
            {
                logger.LogWarning($"Failed to get media length from uri: {mediaUri}");
                return new ArgumentError("Media length not found in response. Ensure media uri returns a content-length header.")
                    .SetClientMessage("Failed to obtain media size");
            }

            if (mediaLength == 0 || mediaLength > 300_000_000)
            {
                return new ArgumentError("Media file is empty or larger than 300MB");
            }

            SortedDictionary<string, string> queryParams = new()
            {
                { "command", "INIT" },
                { "media_type", "media" },
                { "media_category", mediaCategory },
                { "total_bytes", mediaLength.ToString() }
            };

            var initRequestResult = await SendHttpRequestAsync<XMedia>(UploadMediaUri, HttpMethod.Post, apiKey, apiSecret, accessToken, accessTokenSecret, queryParams, cancellationToken: cancellationToken);
            logger.LogResult(initRequestResult, "Initiate X media upload");
            if (!initRequestResult.Success)
                return initRequestResult.Error;

            // write chunks
            XMedia media = initRequestResult.Value;
            using (var mediaStream = httpResponse.Content.ReadAsStream(cancellationToken))
            {
                // Create a buffer to hold the chunk of data
                Memory<byte> buffer = new byte[1024 * 1024]; // 1MB buffer

                int bytesRead;
                int segmentIndex = 0;
                while ((bytesRead = await mediaStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    SortedDictionary<string, string> appendParams = new()
                    {
                        { "command", "APPEND" },
                        { "media_id", media.MediaIdString },
                        { "segment_index", segmentIndex.ToString() }
                    };

                    // content
                    MultipartFormDataContent content = new()
                    {
                        { new ByteArrayContent(buffer.ToArray(), 0, bytesRead), "media" }
                    };

                    var appendResult = await SendHttpRequestAsync<Stream>(UploadMediaUri, HttpMethod.Post, apiKey, apiSecret, accessToken, accessTokenSecret, appendParams, 
                        content, cancellationToken);
                    logger.LogResult(appendResult, $"Append {bytesRead} bytes to media {media.MediaIdString}");
                    if (!appendResult.Success)
                        return appendResult.Error
                            .SetClientMessage("Failed to upload media to X");

                    // increment segment index
                    segmentIndex++;
                }
            }

            // finalize media upload
            SortedDictionary<string, string> finalizeParams = new()
            {
                { "command", "FINALIZE" },
                { "media_id", media.MediaIdString }
            };
            
            var finalizeResult = await SendHttpRequestAsync<XMedia>(UploadMediaUri, HttpMethod.Post, apiKey, apiSecret, accessToken, accessTokenSecret, finalizeParams, cancellationToken: cancellationToken);
            logger.LogResult(finalizeResult, $"Finalize upload for X media {media.MediaIdString}");
            if (!finalizeResult.Success)
                return finalizeResult.Error;

            media = finalizeResult.Value;
            return media;
        }

        #endregion


        #region Post tweet

        // Post a tweet.
        public async Task<Result<Tweet>> PostTweetAsync(string apiKey, string apiSecret, string accessToken, string accessTokenSecret, string tweetText, string[]? mediaIds, CancellationToken cancellationToken = default)
        {
            // create request
            PostTweetRequest request = new()
            {
                Text = tweetText,
                Media = (mediaIds != null && mediaIds.Length >= 1) ? new() { MediaIds = mediaIds } : null
            };

            // create json content for http request
            JsonContent content = JsonContent.Create(request);

            // make request to post tweet
            var result = await SendHttpRequestAsync<XData<Tweet>>(TweetsUrl, HttpMethod.Post, apiKey, apiSecret, accessToken, accessTokenSecret, null, content, cancellationToken);
            logger.LogResult(result, "Post X tweet");
            if (!result.Success)
                return result.Error;

            return result.Value.Data;
        }


        #endregion


        #region HTTP request helper
        private async Task<Result<T>> SendHttpRequestAsync<T>(string url, HttpMethod method, string apiKey, string apiSecret, string accessToken, string accessTokenSecret, 
            SortedDictionary<string, string>? queryParams, HttpContent? content = null, CancellationToken cancellationToken = default) where T : class
        {
            // generate authorization header
            SortedDictionary<string, string> authParams = new()
            {
                { "oauth_token", accessToken }
            };
            string authorizationHeader = OAuthHeader.Authorization(method.ToString(), url, apiKey, apiSecret, accessTokenSecret, authParams, queryParams);

            // set authorization header
            apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", authorizationHeader);
            apiClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

            // construct uri with query params
            string finalUrl = url;
            if (queryParams != null && queryParams.Count != 0)
            {
                finalUrl += "?";
                foreach (var param in queryParams)
                    finalUrl += $"{param.Key}={param.Value}&";
                finalUrl = finalUrl.TrimEnd('&');
            }

            // make request
            var result = await HttpRequestHelper.SendHttpRequestAsync<T>(apiClient, finalUrl, method, logger, content, cancellationToken);
            logger.LogResult(result, "Send X http request");
            if (!result.Success)
                return result.Error;

            return result.Value;
        }

        #endregion
    }
}
