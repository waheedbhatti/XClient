using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XClient.Domain
{
    public class XData<T> where T : class
    {
        [JsonPropertyName("data")]
        public required T Data { get; set; }
    }
}
