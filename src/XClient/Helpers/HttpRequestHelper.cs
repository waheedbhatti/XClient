using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using XClient.Errors;
using HttpRequestError = XClient.Errors.HttpRequestError;

namespace XClient.Helpers
{
    public static class HttpRequestHelper
    {
        public static async Task<Result<T>> SendHttpRequestAsync<T>(HttpClient httpClient, string uri, HttpMethod method, ILogger logger, 
            HttpContent? content = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var request = new HttpRequestMessage(method, uri)
                {
                    Content = content
                };

                // tracing
                if (Activity.Current != null)
                {
                    // Add the traceparent header
                    request.Headers.Add("traceparent", Activity.Current.Id);
                }

                var httpResponse = await httpClient.SendAsync(request, cancellationToken);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    string? errorContent = null;
                    if ((httpResponse.Content.Headers.ContentLength ?? 0) > 0)
                    {
                        errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    }
                    logger.LogError("Request failed: {statusCode} {reason}. Body: {errorContent}", httpResponse.StatusCode, httpResponse.ReasonPhrase, errorContent);

                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new NotFoundError();
                    }
                    return new HttpRequestError((int)httpResponse.StatusCode, errorContent);
                }

                if (typeof(T) == typeof(Stream))
                {
                    Stream stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken: cancellationToken);
                    return (stream as T)!;
                }

                var response = await httpResponse.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
                if (response == null)
                {
                    string responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Could not parse response: {response}", responseContent);
                    return new Error("Could not parse response");
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Request failed: {uri}", uri);
                return new Error("Service request failed");
            }
        }
    }
}
