using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XClient.Domain
{
    public class XUser
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("username")]
        public required string Name { get; set; }

        [JsonPropertyName("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
    }
}
