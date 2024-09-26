using Microsoft.Extensions.DependencyInjection;
using Polly;
using XClient.Authentication;

namespace XClient
{
    public static class DependencyInjection
    {
        public static void AddXClient(this IServiceCollection services)
        {
            services.AddScoped<XAuth>();
            services.AddScoped<IXClient, XClient>();


            string httpClientName = nameof(XClient);
            services.AddHttpClient(httpClientName, client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "XClient");
            })
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddHttpClient(XClient.ExternalResourceClientName, client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json, image/jpeg, image/png, image/webp, image/gif, video/mp4, video/mpeg, video/mov");
                client.DefaultRequestHeaders.Add("User-Agent", "XClient");
            })
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        }
    }
}
