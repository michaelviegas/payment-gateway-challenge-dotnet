using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.IntegrationTests.Fixtures;

internal static class PaymentsApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static PostPaymentRequest ValidRequest(TimeProvider clock) => new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = clock.GetUtcNow().Year + 1,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    public static Task<HttpResponseMessage> PostPaymentAsync(this HttpClient client, PostPaymentRequest request) =>
        client.PostAsJsonAsync("/api/Payments", request);

    public static Task<HttpResponseMessage> GetPaymentAsync(this HttpClient client, Guid id) =>
        client.GetAsync($"/api/Payments/{id}");

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Json);

        Assert.NotNull(body);

        return body;
    }
}
