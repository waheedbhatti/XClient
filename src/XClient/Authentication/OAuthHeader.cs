using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Authentication
{
    internal class OAuthHeader
    {
        private const string OAuthVersion = "1.0";
        private const string OAuthSignatureMethod = "HMAC-SHA1";

        public static string Authorization(string httpMethod, string url, string apiKey, string apiSecret, string? accessTokenSecret, 
            SortedDictionary<string, string>? authParameters, SortedDictionary<string, string>? urlParameters = null)
        {
            var oauthNonce = Guid.NewGuid().ToString("N");
            var oauthTimestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            var oauthParameters = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", apiKey },
                { "oauth_nonce", oauthNonce },
                { "oauth_signature_method", OAuthSignatureMethod },
                { "oauth_timestamp", oauthTimestamp },
                { "oauth_version", OAuthVersion }
            };

            // add auth paramters
            if (authParameters != null)
            {
                foreach (var parameter in authParameters)
                {
                    oauthParameters.Add(parameter.Key, parameter.Value);
                }
            }

            // add url parameters
            if (urlParameters != null)
            {
                foreach (var parameter in urlParameters)
                {
                    oauthParameters.Add(parameter.Key, parameter.Value);
                }
            }

            var baseString = GenerateBaseString(httpMethod, url, oauthParameters);
            var compositeKey = $"{Uri.EscapeDataString(apiSecret)}&{Uri.EscapeDataString(accessTokenSecret ?? string.Empty)}";
            var oauthSignature = GenerateSignature(baseString, compositeKey);

            return GenerateAuthorizationHeader(oauthParameters, oauthSignature);
        }

        private static string GenerateBaseString(string httpMethod, string url, SortedDictionary<string, string> parameters)
        {
            var parameterString = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{httpMethod.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString)}";
        }

        private static string GenerateSignature(string baseString, string compositeKey)
        {
            Encoding encoding = new UTF8Encoding();
            using (var hasher = new HMACSHA1(encoding.GetBytes(compositeKey)))
            {
                string base64String = Convert.ToBase64String(hasher.ComputeHash(encoding.GetBytes(baseString)));
                return Uri.EscapeDataString(base64String);
            }
        }

        private static string GenerateAuthorizationHeader(SortedDictionary<string, string> parameters, string signature)
        {
            var headerString = string.Join(",", parameters
                .Where(kvp => kvp.Key.StartsWith("oauth_"))
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}=\"{Uri.EscapeDataString(kvp.Value)}\""));

            headerString += ",oauth_signature=\"" + signature + "\"";
            return headerString;
        }
    }
}
