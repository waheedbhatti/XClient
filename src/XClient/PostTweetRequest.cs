using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XClient
{
    public class PostTweetRequest
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }

        [JsonPropertyName("media"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PostTweetRequestMedia? Media { get; set; }
    }

    public class PostTweetRequestMedia
    {
        [JsonPropertyName("media_ids")]
        public required string[] MediaIds { get; set; }
    }
}
