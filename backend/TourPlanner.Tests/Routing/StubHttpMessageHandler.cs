using System.Net;
using System.Text;

namespace TourPlanner.Tests.Routing;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/>.
/// Records every outbound request and lets the test supply a response per URL match,
/// or a default response, or throw <see cref="HttpRequestException"/>.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Requests captured in the order they were sent.</summary>
    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>Captured request bodies (matched by index with <see cref="Requests"/>).</summary>
    public List<string?> RequestBodies { get; } = new();

    /// <summary>Ordered (matcher, responseFactory) pairs. First match wins.</summary>
    public List<(Func<HttpRequestMessage, bool> Match, Func<HttpResponseMessage> Response)> Handlers { get; } = new();

    /// <summary>Fallback response when no handler matches.</summary>
    public Func<HttpResponseMessage> Default { get; set; } =
        () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };

    /// <summary>When set, every send throws this exception (simulates network failure).</summary>
    public Exception? Throw { get; set; }

    public StubHttpMessageHandler When(
        Func<HttpRequestMessage, bool> match, HttpStatusCode status, string json)
    {
        Handlers.Add((match, () => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));
        return this;
    }

    public StubHttpMessageHandler WhenUrlContains(string fragment, HttpStatusCode status, string json)
        => When(r => r.RequestUri!.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase), status, json);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (Throw is not null) throw Throw;

        foreach (var (match, response) in Handlers)
        {
            if (match(request)) return response();
        }
        return Default();
    }
}
