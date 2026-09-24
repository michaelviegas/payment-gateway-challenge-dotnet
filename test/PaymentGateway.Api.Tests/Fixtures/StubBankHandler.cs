using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace PaymentGateway.Api.Tests.Fixtures;

public sealed class StubBankHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly ConcurrentQueue<(HttpRequestMessage Request, string Body)> _requests = new();

    public IReadOnlyCollection<(HttpRequestMessage Request, string Body)> Requests => _requests;

    public static StubBankHandler Returning(HttpStatusCode statusCode, string? json = null) => new(() =>
        new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json")
        });

    public static StubBankHandler Throwing(Exception exception) => new(() => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Enqueue((request, body));

        return respond();
    }
}
