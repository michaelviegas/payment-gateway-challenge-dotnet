# Payment Gateway

A .NET 8 payment gateway for the [Checkout.com engineering assessment](https://github.com/cko-recruitment/). Merchants use it to process a card payment through an acquiring bank and to retrieve past payments.

## Running it

### Everything in Docker

```
docker compose up --build
```

| Service | URL |
|---|---|
| Payment gateway | http://localhost:5000 |
| Bank simulator | http://localhost:8080 |
| Traces and metrics (Aspire dashboard) | http://localhost:18888 |

### Gateway from source

Start only the bank simulator, then run the API:

```
docker compose up bank_simulator
dotnet run --project src/PaymentGateway.Api
```

The API listens on http://localhost:5067, with Swagger at http://localhost:5067/swagger.

### Trying it out

The simulator decides based on the last digit of the card: odd is authorized, even is declined, and `0` makes the bank unavailable.

```
curl -i http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: demo-1" \
  -d '{"cardNumber":"2222405343248877","expiryMonth":4,"expiryYear":2030,"currency":"GBP","amount":1050,"cvv":"123"}'
```

```
HTTP/1.1 200 OK
X-Correlation-Id: demo-1

{"id":"3f2c…","status":"Authorized","cardNumberLastFour":"8877","expiryMonth":4,"expiryYear":2030,"currency":"GBP","amount":1050}
```

Retrieve it with `curl http://localhost:5000/api/payments/{id}`.

An invalid request is rejected without calling the bank:

```
HTTP/1.1 400 Bad Request
X-Correlation-Id: 81e96587-4832-4c62-8bc8-663d819cd815

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Payment.Rejected",
  "status": 400,
  "detail": "No payment could be created as invalid information was supplied to the payment gateway and therefore it has rejected the request without calling the acquiring bank.",
  "errors": {
    "Cvv": ["CVV is required."],
    "ExpiryYear": ["Card has expired."]
  },
  "traceId": "00-fe5ae50e5d42701b632a809a5eb1dec2-48cec0f09d156e94-01"
}
```

### Tests

```
dotnet test
```

The tests don't need Docker or the bank simulator.

## Repository layout

```
src/
    PaymentGateway.Api             ASP.NET Core host: controller, middleware, observability, health endpoints
    PaymentGateway.Application     use cases, validation, pipeline behaviors, business metrics
    PaymentGateway.Domain          payment entity and value objects
    PaymentGateway.Infrastructure  bank client, bank health check, in-memory storage
test/
    one unit test project per source project, plus PaymentGateway.IntegrationTests
imposters/                         bank simulator configuration (provided, unchanged)
Dockerfile                         container image for the API
docker-compose.yml                 gateway, bank simulator and dashboard
.github/workflows/ci.yml           build, test and container smoke test
```

## Design notes

### Architecture

The solution has four projects in a clean-architecture layout. Dependencies point inward.

```
Api  ->  Application  ->  Domain
 |            ^
 +-> Infrastructure
```

- `Domain` holds `Payment`, its status, and the value objects (`CardDetails`, `CardInfo`, `Money`, `PaymentId`). It has no dependencies.
- `Application` holds the use cases: `PostPaymentCommand` and `GetPaymentQuery`, their handlers, validation, and the interfaces the handlers need (`IBankClient`, `IPaymentRepository`, `IUnitOfWork`).
- `Infrastructure` implements those interfaces: an HTTP client for the bank simulator, and an in-memory payment store.
- `Api` is the HTTP layer: one controller, correlation-id and exception middleware, and Serilog.

Storage is in memory, as the spec allows. `InMemoryDbContext` tracks changes per request and commits them to a shared store at the end, so swapping in a real database only touches `Infrastructure`.

### API design

| Method | Route | Result |
|---|---|---|
| `POST` | `/api/payments` | `200` with the payment, status `Authorized` or `Declined` |
| `GET` | `/api/payments/{id}` | `200` with the payment, or `404` |
| `GET` | `/health/live` | `200 Healthy` while the process can serve requests |
| `GET` | `/health/ready` | `200 Healthy`, or `200 Degraded` when the bank is unreachable (see [Health checks](#health-checks)) |

Both return the same shape:

```json
{
  "id": "3f2c...",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2027,
  "currency": "GBP",
  "amount": 1050
}
```

The full card number and CVV are never stored or returned. Only the last four digits and the expiry are kept with the payment. `cardNumberLastFour` is a string, so a card ending in `0012` isn't returned as `12`. `PostPaymentCommand` overrides `ToString` to mask the card number and leave out the CVV, so neither can leak into logs or exception messages.

Rejections use RFC 7807 problem details.

- **Rejected** is represented by `400 Bad Request` with the title `Payment.Rejected`, not by a status in the body. Nothing is stored, so a rejected payment can't be retrieved later.
- Invalid input is rejected before the bank is called. The response has one entry per failing field, and one message per field: the first rule that fails.
- A body that can't be read (malformed JSON, or a value of the wrong type such as `"amount": 10.5`) gets ASP.NET's default `400` validation problem, keyed by JSON path (`$.amount`). Serializer messages are turned off (`AllowInputFormatterExceptionMessages = false`), so .NET type names aren't exposed.
- If the bank is unavailable (the simulator's `503` for cards ending in `0`, a timeout, an unreadable response), the payment is also rejected. The bank client logs the reason as a warning, with the card masked to its last four digits.
- Unhandled errors return `500` with an empty body. The exception is logged with the correlation id, and no details reach the caller.

Validation choices the spec left open:

- Currency is limited to `GBP`, `USD` and `EUR`, and is case-sensitive.
- A card expiring in the current month is still valid. "Now" comes from an injected `TimeProvider` (UTC), so tests pin the date and cover the month and year boundaries.
- Amount must be greater than zero.

### Code design

Controllers stay thin. They map the request to a command or query, send it through [Mediator](https://github.com/martinothamar/Mediator), and turn the result into an HTTP response.

Handlers return `Result<T>` instead of throwing for expected failures such as validation errors or a payment not found. Exceptions are kept for bugs and infrastructure faults.

Cross-cutting concerns run as pipeline behaviors, in this order:

1. `RequestLogging` logs each request and its outcome. A failed `Result` is logged as a warning, because a rejected payment or unknown id is an expected outcome, not a fault.
2. `Validation` runs the FluentValidation rules and stops invalid requests before any other work.
3. `UnitOfWork` (commands only) saves changes if the handler succeeded.

Exceptions are logged once, at `Error`, by `ExceptionHandlingMiddleware` in the API. It sees everything that throws, including failures outside the Mediator pipeline, and it logs with the correlation id.

The bank client uses a typed `HttpClient` with a Polly resilience pipeline. It retries only DNS and connection errors, because those requests never reached the bank. Timeouts and dropped responses are not retried, since the bank may already have charged the card. A circuit breaker stops calls while the bank keeps failing.

Bank settings are bound to `BankOptions` and validated when the app starts, so a missing or malformed URL stops the app instead of failing the first payment:

```json
"Bank": {
  "BaseUrl": "http://localhost:8080",
  "AttemptTimeout": "00:00:10",
  "TotalTimeout": "00:00:30"
}
```

`AttemptTimeout` limits a single call to the bank. `TotalTimeout` limits the whole operation, retries included. Every setting can be overridden with an environment variable such as `Bank__BaseUrl`.

### Observability

**Logs.** Serilog writes to stdout. In Development the output is readable text. Everywhere else it's one JSON object per line (`RenderedCompactJsonFormatter`), ready for a log pipeline to index. Every event carries:

- `CorrelationId`, from the caller's `X-Correlation-Id` header or generated. It's echoed in the response and forwarded to the bank on every attempt, so one id follows the payment across both systems. Ids longer than 64 characters, or with characters outside `A-Z a-z 0-9 - _ . :`, are replaced, because the value ends up in logs and in the bank request.
- `@tr` and `@sp`, the OpenTelemetry trace and span ids, so a log line leads straight to its trace.

Card data never reaches the logs. Only the last four digits are logged, and a test checks this on every logging path (see [Testing](#testing)).

**Traces.** OpenTelemetry traces incoming requests and outgoing HttpClient calls, so a trace shows the gateway request, each attempt to the bank, and how long each took. Spans are tagged with `correlation.id`. Health probes are excluded.

**Metrics.**

| Metric | Source | Answers |
|---|---|---|
| `payments.processed` (`outcome`, `currency`) | `PaymentMetrics` | Authorized, declined and rejected-by-bank rates. A rise in `Rejected` means the bank is failing |
| `http.server.request.duration` | ASP.NET Core | Gateway latency and error rate, by route and status code |
| `http.client.request.duration` | HttpClient | Bank latency and error rate |
| `resilience.polly.*` | Polly | Retries, timeouts, circuit breaker opening and closing |
| `process.runtime.dotnet.*` | Runtime | GC, thread pool, exceptions |

Requests rejected by validation never reach the bank, so they show up as `400`s in `http.server.request.duration`, not in `payments.processed`.

Nothing is exported unless `OTEL_EXPORTER_OTLP_ENDPOINT` is set, so local runs and tests need no collector. `docker compose` sets it to the bundled Aspire dashboard. In production it would point at the platform's OpenTelemetry collector.

### Health checks

- `/health/live` runs no checks. It only fails if the process can't serve requests, which is when an orchestrator should restart it.
- `/health/ready` checks that the bank is reachable. Any HTTP response counts, since the simulator answers unknown routes with `400`. The probe uses its own `HttpClient`, outside the resilience pipeline, so probes never count towards the circuit breaker.

When the bank is unreachable, readiness reports `Degraded` with a `200`, not `Unhealthy` with a `503`. If every gateway instance failed readiness, the load balancer would remove them all, and merchants would get connection errors instead of a clear `Payment.Rejected`. The bank outage still shows in the readiness body, the logs and the `payments.processed` metric.

### Packaging and hosting

The `Dockerfile` is a multi-stage build. The restore layer only depends on the `.csproj` files, so it stays cached until a dependency changes. The runtime image is the chiseled Ubuntu ASP.NET image: it has no shell or package manager and runs as a non-root user.

The container listens on port 8080 over plain HTTP. In production, TLS would terminate at the load balancer or ingress, which is also where the health endpoints would be probed. Configuration comes from environment variables (`Bank__BaseUrl`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `ASPNETCORE_ENVIRONMENT`), so the same image runs in every environment.

The payment store is in memory, so the service can only run as a single instance for now. Scaling out needs a shared database, and nothing else in the service holds state.

### Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request:

1. Restores, fails the build if any direct or transitive package has a known vulnerability, builds, and runs all tests with coverage.
2. Builds the image and starts the whole stack with `docker compose`. It then checks liveness, sends a real payment through the gateway to the bank simulator, and checks readiness. This catches problems the in-process tests can't see, such as a broken image, wrong container config, or the gateway failing to talk to the real simulator.

### Testing

Each source project has its own unit test project, and one integration test project covers the whole app. All use xUnit.

- **Unit tests** exercise one class at a time with no host. `Domain.Tests` covers `Payment` and the value objects. `Application.Tests` covers the validator, the handlers, the `payments.processed` metric (with `MetricCollector`) and each pipeline behavior, using small hand-written fakes for `IBankClient`, `IPaymentRepository` and `IUnitOfWork`. `Infrastructure.Tests` runs `BankClient` against a stub `HttpMessageHandler` to check the bank's JSON format and how each failure maps to a result. It also covers the bank health check, the validation of `BankOptions`, and the in-memory store, context and repository. `Api.Tests` covers the middleware, correlation id forwarding and the controller's mapping from `Result` to HTTP responses, with NSubstitute standing in for `ISender`.
- **Integration tests** run the full app through `WebApplicationFactory`. Most swap `IBankClient` for a fake and `TimeProvider` for a `FakeTimeProvider` pinned to a fixed date. `BankIntegrationTests` keep the real client and stub the HTTP handler instead, so the resilience pipeline is tested too, including the retry rules and the correlation id sent on every attempt. They keep the system clock, because Polly reads `TimeProvider` for its timeouts, retry delays and circuit breaker, and a frozen clock would stall them. `HealthCheckTests` cover liveness and readiness with the bank up and down.
- **Log redaction.** `LogRedactionTests` send a real card number through every path that logs: success, the bank returning an error, the bank unreachable, the connection dropping, an unreadable bank response, a validation failure and malformed JSON. Each time, it captures what Serilog would ship, in the production JSON format, and asserts that the full card number, its first twelve digits and any `cvv` field are absent. Adding a log line that includes the bank request makes the test fail.
- **Container smoke test.** CI runs the real image against the real simulator (see [Continuous integration](#continuous-integration)).

Internal classes are exposed to their own test project with `InternalsVisibleTo`.

Run everything with `dotnet test`. The tests don't need the bank simulator.

### Assumptions and scope

The spec asks for two things: process a payment and retrieve it. I kept the API to that and left out anything it doesn't ask for.

**Authentication and merchant isolation.** The spec doesn't cover how merchants authenticate, so I assume the gateway sits behind an auth layer and every caller is trusted. Payment ids are random GUIDs, so they can't be guessed. In production, an API key or JWT would resolve to a `MerchantId`, each `Payment` would store it, and `GET` would filter by it. A payment owned by another merchant would return `404`, not `403`, so its existence isn't revealed.

**Idempotency.** Not implemented. In production, `POST` should accept an `Idempotency-Key` header so a merchant can safely retry after a timeout without charging the card twice. It needs durable storage, with a unique index on the key written in the same transaction as the payment. A replay with the same key returns the original response, and a reused key with a different body returns `422`.

**Timeouts.** A bank timeout rejects the payment and nothing is stored, but the bank may still have authorized the card. A real gateway would record the payment as pending before calling the bank and reconcile it afterwards.
