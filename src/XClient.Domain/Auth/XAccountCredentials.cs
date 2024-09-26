using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Domain.Auth
{
    public class XAccountCredentials(string id, string name, string accessToken, string accessTokenSecret)
    {
        public string Id { get; } = id;
        public string Name { get; } = name;
        public string AccessToken { get; } = accessToken;
        public string AccessTokenSecret { get; } = accessTokenSecret;
    }
}
