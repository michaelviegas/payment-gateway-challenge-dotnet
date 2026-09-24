# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.

## Design notes

### Architecture

The solution has four projects in a clean-architecture layout. Dependencies point inward.

```
Api  ->  Application  ->  Domain
 |            ^
 +-> Infrastructure
```

- `Domain` holds `Payment`, its status, and the value objects (`CardDetails`, `CardInfo`, `Money`, `PaymentId`). It has no dependencies.
- `Application` holds the use cases: `PostPaymentCommand` and `GetPaymentQuery`, their handlers, validation, and the interfaces the handlers need (`IBankClient`, `IPaymentRepository`, `IUnitOfWork`, `IIdempotencyStore`).
- `Infrastructure` implements those interfaces: an HTTP client for the bank simulator, and in-memory stores for payments and idempotency keys.
- `Api` is the HTTP layer: one controller, correlation-id and exception middleware, and Serilog.

Storage is in memory, as the spec allows. `InMemoryDbContext` tracks changes per request and commits them to a shared store at the end, so swapping in a real database only touches `Infrastructure`.

### API design

| Method | Route | Result |
|---|---|---|
| `POST` | `/api/payments` | `200` with the payment, status `Authorized` or `Declined` |
| `GET` | `/api/payments/{id}` | `200` with the payment, or `404` |

Both return the same shape:

```json
{
  "id": "3f2c...",
  "status": "Authorized",
  "cardNumberLastFour": 8877,
  "expiryMonth": 4,
  "expiryYear": 2027,
  "currency": "GBP",
  "amount": 1050
}
```

The full card number and CVV are never stored or returned. Only the last four digits and the expiry are kept with the payment.

Errors use RFC 7807 problem details. Invalid input returns `400` with one entry per failing field. If the bank call fails (including the simulator's `503` for cards ending in `0`), the gateway returns `400` with `Payment.Rejected` and stores nothing. A missing `Idempotency-Key` header returns `400`, and a key that is still being processed returns `409`. Unhandled errors return `500`.

Validation choices the spec left open:

- Currency is limited to `GBP`, `USD` and `EUR`, and is case-sensitive.
- A card expiring in the current month is still valid.
- Amount must be greater than zero.

### Code design

Controllers stay thin. They map the request to a command or query, send it through [Mediator](https://github.com/martinothamar/Mediator), and turn the result into an HTTP response.

Handlers return `Result<T>` instead of throwing for expected failures such as validation errors or a payment not found. Exceptions are kept for bugs and infrastructure faults.

Cross-cutting concerns run as pipeline behaviors, in this order:

1. `ExceptionHandling` logs anything that throws.
2. `RequestLogging` logs each request and its outcome.
3. `Validation` runs the FluentValidation rules and stops invalid requests before any other work.
4. `Idempotency` (commands only) claims the key before the handler runs.
5. `UnitOfWork` (commands only) saves changes if the handler succeeded.

The bank client uses a typed `HttpClient` with a Polly resilience pipeline. It retries only DNS and connection errors, because those requests never reached the bank. Timeouts and dropped responses are not retried, since the bank may already have charged the card. A circuit breaker stops calls while the bank keeps failing.

Tests are mostly integration tests through `WebApplicationFactory`. Most swap `IBankClient` for a fake. `BankClientTests` keep the real client and stub the HTTP handler instead, so the JSON mapping and the retry rules are tested too.

### Idempotency

`POST /api/payments` requires an `Idempotency-Key` header. Without one, the request is rejected with `400`.

Commands that implement `IIdempotentCommand` go through `IdempotencyPipelineBehavior`. It claims the key atomically before the handler runs, scoped to the command type, and then does one of three things depending on how the handler finishes:

- If it succeeds, the response is stored and replayed to any retry with the same key.
- If it returns a failure such as `Rejected`, the key is released and the merchant can retry.
- If it throws, the key stays claimed and retries get `409`. This is deliberate. The bank simulator doesn't support idempotency, so calling it a second time could authorize the card twice, and I'd rather return a conflict than risk a double charge.

### Follow-ups

The idempotency store is in-memory, so a process crash wipes it along with everything else. The pattern only really works with durable storage: a unique index on the idempotency key, ideally written in the same transaction as the payment.

Reusing a key with a different request body currently replays the original payment. It should be rejected instead, probably with `422`.
