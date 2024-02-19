using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FriendlyMeme.Handlers.WebRequests;

/// <summary>
/// Represents an HTTP Handler for making requests to different endpoints.
/// </summary>
public class HttpHandler
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the HttpHandler class with optional proxy configuration.
    /// </summary>
    /// <param name="proxyHost">The host address of the proxy, if any.</param>
    /// <param name="proxyPort">The port number of the proxy, if any.</param>
    /// <param name="timeout">The timespan to wait before the request times out. If not specified, default is set to 100 seconds.</param>
    public HttpHandler(string proxyHost = "", int proxyPort = 0, TimeSpan? timeout = null)
    {
        HttpClientHandler clientHandler = GetConfiguredClientHandler(proxyHost, proxyPort);
        _client = new HttpClient(clientHandler, true)
        {
            
            Timeout = timeout ?? TimeSpan.FromSeconds(100)
        };
    }

#if NET8_0_OR_GREATER
    private HttpClientHandler GetConfiguredClientHandler(string proxyHost, int proxyPort)
    {
        var clientHandler = new HttpClientHandler
        {
            AutomaticDecompression =
                DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback =  (sender, certificate, chain, sslPolicyErrors) => true
        };

        if (!string.IsNullOrEmpty(proxyHost) && proxyPort > 0)
        {
            clientHandler.Proxy = new WebProxy(proxyHost, proxyPort);
            clientHandler.UseProxy = true;
        }

        return clientHandler;
    }

    /// <summary>
    /// Sends a HTTP request asynchronously to the specified URI using a specified HTTP method.
    /// </summary>
    /// <param name="uri">The URI to send the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="timeout">Optional. The timespan to wait before the request times out. If not specified, HttpClient's timeout is used.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the HttpResponseMessage.</returns>
    public async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method, TimeSpan? timeout = null)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = method,
        };

        using var cts = new CancellationTokenSource(timeout ?? _client.Timeout);
        return await _client.SendAsync(request, cts.Token);
    }

    /// <summary>
    /// Sends a HTTP request with a JSON payload asynchronously to the specified URI using a specified HTTP method.
    /// </summary>
    /// <param name="uri">The URI to send the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="json">The JSON content to include in the request body.</param>
    /// <param name="timeout">Optional. The timespan to wait before the request times out. If not specified, HttpClient's timeout is used.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the HttpResponseMessage.</returns>
    public async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method,
        string json, TimeSpan? timeout = null)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = method,
            Content = new StringContent(json)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            },
        };

        using var cts = new CancellationTokenSource(timeout ?? _client.Timeout);
        return await _client.SendAsync(request, cts.Token);
    }

    /// <summary>
    ///  Sends a HTTP request with a JSON payload asynchronously to the specified URI using a specified HTTP method.
    /// </summary>
    /// <param name="uri">The URI to send the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="json">The JSON content to include in the request body.</param>
    /// <param name="requestHeaders">An array of RequestHeadersEx objects representing custom headers for the request.</param>
    /// <param name="timeout">Optional. The timespan to wait before the request times out. If not specified, HttpClient's timeout is used.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the HttpResponseMessage.</returns>
    public async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method,
        string? json, RequestHeadersEx[] requestHeaders, TimeSpan? timeout = null)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = method,
            Content = new StringContent(json)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            },
        };


        foreach (var requestHeader in requestHeaders)
        {
            request.Headers.Add(requestHeader.Key, requestHeader.Value);
        }

        using var cts = new CancellationTokenSource(timeout ?? _client.Timeout);
        return await _client.SendAsync(request, cts.Token);
    }

    /// <summary>
    /// Sends a HTTP request with specified HTTP method, an object as FormUrlEncodedContent payload and custom headers asynchronously to the specified URI. 
    /// </summary>
    /// <param name="uri">The URI to send the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="requestHeaders">A collection of <see cref="RequestHeadersEx" /> objects representing custom headers for the request.</param>
    /// <param name="data">The object to be sent as FormUrlEncodedContent in the request body.</param>
    /// <param name="timeout">Optional timespan to wait before the request times out. If not specified, HttpClient's timeout is used.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the HttpResponseMessage.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the timeout expires before the task completes.</exception>
    public async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method,
        IEnumerable<RequestHeadersEx> requestHeaders, object data, TimeSpan? timeout = null)
    {
        var jsonString = JsonSerializer.Serialize(data);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
        var content = new FormUrlEncodedContent(dictionary ?? new Dictionary<string, string>());

        var request = new HttpRequestMessage(method, uri)
        {
            Content = content
        };

        foreach (var header in requestHeaders)
        {
            if (header.Key != "Content-Type" && !request.Headers.Contains(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        using var cts = new CancellationTokenSource(timeout ?? _client.Timeout);
        return await _client.SendAsync(request, cts.Token);
    }


    /// <summary>
    /// Sends a HTTP request asynchronously to the specified URI using a specified HTTP method.
    /// </summary>
    /// <param name="uri">The URI to send the request to.</param>
    /// <param name="method">The HTTP method to use for the request.</param>
    /// <param name="requestHeaders">An array of RequestHeadersEx objects representing custom headers for the request.</param>
    /// <param name="timeout">Optional. The timespan to wait before the request times out. If not specified, HttpClient's timeout is used.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the HttpResponseMessage.</returns>
    public async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method,
        RequestHeadersEx[] requestHeaders, TimeSpan? timeout = null)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = method
        };

        foreach (var requestHeader in requestHeaders)
        {
            request.Headers.Add(requestHeader.Key, requestHeader.Value);
        }

        using var cts = new CancellationTokenSource(timeout ?? _client.Timeout);
        return await _client.SendAsync(request, cts.Token);
    }

    /// <summary>
    ///Downloads a file from the specified URL and saves it to the specified directory path asynchronously.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="path">The local file path where the file should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method will save the file in the provided path using the file name extracted from the URL.
    /// If a file with the same name already exists in the destination folder, it will be overwritten.
    /// If the provided path is not valid or accessible, an UnauthorizedAccessException will be thrown.
    /// </remarks>
    public async Task DownloadFileAsync(string? url, string path)
    {
        var tempPath = Path.GetTempPath();
        var fullPath = Path.Combine(tempPath, path);

        try
        {
            var streamAsync = await _client.GetStreamAsync(url);

            await using var fileStream =
                new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await streamAsync.CopyToAsync(fileStream);
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"Error: Access denied to path: {fullPath}. Exception: {e.Message}");
        }
    }


    /// <summary>
    /// Sends a HTTP request asynchronously to the specified URI using a specified HTTP method.
    /// </summary>
    /// <param name="url">The URL to get the content from.</param>
    /// <returns>A task representing the asynchronous operation, with a result containing the content as a string.</returns>
    public async Task<string> GetStringAsync(string url)
        => await _client.GetStringAsync(url);


    /// <summary>
    /// Disposes the HttpClient
    /// </summary>
    public void Dispose()
        => _client.Dispose();


    public record RequestHeadersEx(string Key, string Value);
#endif
}
