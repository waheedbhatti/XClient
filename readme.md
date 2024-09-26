#Getting Started

Call the `AddXClient()` extension method of `IServiceCollection` to register the XClient services.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddXClient();

var app = builder.Build();
```

You can now inject the `IXClient` service into your controllers or services.

```csharp
public class MyController : ControllerBase
{
    private readonly IXClient _xClient;

    public MyController(IXClient xClient)
    {
        _xClient = xClient;
    }

}
```

#Creating an Authorization Url

```csharp
var result = await client.CreateAuthUrlAsync(apiKey, apiSecret, callbackUrl, cancellationToken);
```

Convert the oauth verifier to a token

```csharp
var result = await client.GetAccessTokenAsync(apiKey, apiSecret, oauthToken, oauthVerifier, cancellationToken);
```

#Posting media to twitter

First upload each media to twitter:

```csharp
var uploadResult = await client.UploadMediaAsync(m, mediaType, apiKey, apiSecret, accessToken, accessTokenSecret, cancellationToken);
if (!uploadResult.Success)
    return uploadResult.Error;

string mediaId = uploadResult.Value.MediaIdString;
```

Then post the tweet using media ids:

```csharp
var result = await client.PostTweetAsync(apiKey, apiSecret, accessToken, accessTokenSecret, text, [.. mediaIds], cancellationToken);
```
