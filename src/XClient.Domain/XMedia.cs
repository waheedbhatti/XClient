using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XClient.Domain
{
    public class XMedia
    {
        [JsonPropertyName("media_id")]
        public long MediaId { get; set; } // do not use this, see below

        [JsonPropertyName("media_id_string")]
        public required string MediaIdString { get; set; } // i have noticed this is the same as media_id but docs say this should be used

        [JsonPropertyName("expires_after_secs")]
        public int ExpiresAfterSecs { get; set; }

        [JsonPropertyName("media_key")]
        public required string MediaKey { get; set; }
    }
}
