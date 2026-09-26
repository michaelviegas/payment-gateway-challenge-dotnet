using System.Net;
using System.Text;

namespace PaymentGateway.Infrastructure.Tests.Bank;

internal sealed class StubHttpHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly List<(HttpRequestMessage Request, string Body)> _requests = [];

    public IReadOnlyList<(HttpRequestMessage Request, string Body)> Requests => _requests;

    public static StubHttpHandler Returning(HttpStatusCode statusCode, string? json = null) => new(() =>
        new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json")
        });

    public static StubHttpHandler Throwing(Exception exception) => new(() => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Add((request, body));

        return respond();
    }
}
