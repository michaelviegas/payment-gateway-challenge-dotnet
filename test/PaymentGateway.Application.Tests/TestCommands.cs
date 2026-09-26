using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Application.Commands.PostPayment;

namespace PaymentGateway.Application.Tests;

internal static class TestCommands
{
    public static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Clock() => new(Now);

    public static PostPaymentCommand ValidPostPayment() =>
        new("2222405343248877", 4, Now.Year + 1, "GBP", 100, "123");
}
