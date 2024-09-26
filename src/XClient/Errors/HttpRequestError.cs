using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class HttpRequestError : Error
    {
        public int StatusCode { get; private init; }
        public string? ErrorContent { get; set; }

        public HttpRequestError(int statusCode, string? errorContent) : base($"Response status code does not indicate success: {statusCode}")
        {
            StatusCode = statusCode;
            ErrorContent = errorContent;
        }
    }
}
