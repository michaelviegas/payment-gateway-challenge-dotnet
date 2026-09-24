using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Domain.ValueObjects;
using PaymentGateway.Infrastructure.Bank.DTOs;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PaymentGateway.Infrastructure.Bank;

internal sealed class BankClient(HttpClient httpClient, ILogger<BankClient> logger) : IBankClient
{
    public async Task<BankAuthorizationResult> AuthorizeAsync(CardDetails card, Money money, CancellationToken cancellationToken)
    {
        var request = BankAuthorizationRequest.Create(card, money);

        try
        {
            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

            return await InterpretAsync(response, card, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning(
                "Acquiring bank circuit is open; payment for card ****{LastFourCardDigits} was not attempted.",
                card.LastFourCardDigits);
        }
        catch (TimeoutRejectedException exception)
        {
            logger.LogWarning(
                "Acquiring bank did not answer within {Timeout} for card ****{LastFourCardDigits}. "
                + "The payment may or may not have been taken, so it is deliberately not retried.",
                exception.Timeout,
                card.LastFourCardDigits);
        }
        catch (HttpRequestException exception) when (
            exception.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError)
        {
            logger.LogWarning(
                exception,
                "Acquiring bank was unreachable ({HttpRequestError}) for card ****{LastFourCardDigits}.",
                exception.HttpRequestError,
                card.LastFourCardDigits);
        }
        catch (HttpRequestException exception)
        {
            // The connection was established, so the bank may have processed the request.
            logger.LogWarning(
                exception,
                "Acquiring bank call failed mid-request ({HttpRequestError}) for card ****{LastFourCardDigits}.",
                exception.HttpRequestError,
                card.LastFourCardDigits);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Acquiring bank returned an unparseable body for card ****{LastFourCardDigits}.",
                card.LastFourCardDigits);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Acquiring bank call was abandoned for card ****{LastFourCardDigits}.",
                card.LastFourCardDigits);
        }

        return BankAuthorizationResult.Rejected;

    }

    private async Task<BankAuthorizationResult> InterpretAsync(
        HttpResponseMessage response,
        CardDetails card,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Acquiring bank returned {StatusCode} for card ****{LastFourCardDigits}.",
                (int)response.StatusCode,
                card.LastFourCardDigits);

            return BankAuthorizationResult.Rejected;
        }

        BankAuthorizationResponse? body =
            await response.Content.ReadFromJsonAsync<BankAuthorizationResponse>(cancellationToken);

        if (body is null)
        {
            logger.LogWarning(
                "Acquiring bank returned an empty body for card ****{LastFourCardDigits}.",
                card.LastFourCardDigits);

            return BankAuthorizationResult.Rejected;
        }

        return body.Authorized
            ? BankAuthorizationResult.Authorized(body.AuthorizationCode!)
            : BankAuthorizationResult.Declined;
    }
}
