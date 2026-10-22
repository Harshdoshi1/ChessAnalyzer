using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient for forwarding /api calls to the backend API service
builder.Services.AddHttpClient("BackendApi", client =>
{
    var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7155";
    client.BaseAddress = new Uri(apiBase.TrimEnd('/'));
    client.Timeout = TimeSpan.FromMinutes(5);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Forward any /api requests to ChessAnalyzer.Api backend
app.Map("/api/{**catch-all}", async (HttpContext context, IHttpClientFactory httpClientFactory, IConfiguration config) =>
{
    var client = httpClientFactory.CreateClient("BackendApi");
    var relativePath = context.Request.Path + context.Request.QueryString;

    var forwardRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), relativePath);

    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method))
    {
        forwardRequest.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrEmpty(context.Request.ContentType))
        {
            forwardRequest.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    foreach (var header in context.Request.Headers)
    {
        if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) continue;
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
        forwardRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    try
    {
        using var response = await client.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in response.Content.Headers)
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        context.Response.ContentType = "application/json";
        var apiBase = config["ApiBaseUrl"] ?? "https://localhost:7155";
        await context.Response.WriteAsJsonAsync(new
        {
            message = $"Unable to connect to ChessAnalyzer.Api at {apiBase}. Please ensure the backend API project is running.",
            error = ex.Message
        });
    }
});

var distBrowserPath = Path.Combine(builder.Environment.ContentRootPath, "dist", "chess-analyzer.ui", "browser");

if (Directory.Exists(distBrowserPath))
{
    var fileProvider = new PhysicalFileProvider(distBrowserPath);

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        OnPrepareResponse = ctx =>
        {
            // Disable caching on HTML and service worker files so browser always fetches latest SPA bundles
            if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
        }
    });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();

app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "API endpoint not found." });
        return;
    }

    var indexPath = Path.Combine(distBrowserPath, "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html";
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Expires", "0");
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(
            "<html><body><h2>Chess Analyzer UI</h2><p>Please build the client application using <code>npm run build</code>.</p></body></html>");
    }
});

app.Run();
