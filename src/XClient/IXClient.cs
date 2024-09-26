using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XClient.Domain;
using XClient.Domain.Auth;

namespace XClient
{
    public interface IXClient
    {
        Task<Result<string>> CreateAuthUrlAsync(string apiKey, string apiSecret, string callbackUrl, CancellationToken cancellationToken = default);
        Task<Result<XAccountCredentials>> GetAccessTokenAsync(string apiKey, string apiSecret, string oauthToken, string oauthVerifier, CancellationToken cancellationToken = default);
        Task<Result<XUser>> GetUserAsync(string apiKey, string apiSecret, string accessToken, string accessTokenSecret, CancellationToken cancellationToken = default);
        Task<Result<Tweet>> PostTweetAsync(string apiKey, string apiSecret, string accessToken, string accessTokenSecret, string tweetText, string[]? mediaIds, CancellationToken cancellationToken = default);
        Task<Result<XMedia>> UploadMediaAsync(string mediaUri, string mediaType, string apiKey, string apiSecret, string accessToken, string accessTokenSecret, CancellationToken cancellationToken = default);
    }
}
