using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Core.Diagnostics;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Application.Tests.Fakes;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Tests.Commands;

public sealed class PostPaymentCommandHandlerTests
{
    private readonly FakeBankClient _bank = new();
    private readonly FakePaymentRepository _repository = new();
    private readonly IMeterFactory _meterFactory =
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    public static TheoryData<BankAuthorizationResult, PaymentStatus> BankDecisions => new()
    {
        { BankAuthorizationResult.Authorized("auth-code"), PaymentStatus.Authorized },
        { BankAuthorizationResult.Declined, PaymentStatus.Declined }
    };

    [Theory]
    [MemberData(nameof(BankDecisions))]
    public async Task StoresPaymentWithBankDecision(BankAuthorizationResult decision, PaymentStatus expectedStatus)
    {
        _bank.Result = decision;

        var result = await Handle(TestCommands.ValidPostPayment());

        Assert.True(result.IsSuccess);
        var payment = Assert.Single(_repository.Payments);
        Assert.Equal(expectedStatus, payment.Status);
        Assert.Equal(PostPaymentResponse.Create(payment), result.Value);
    }

    [Fact]
    public async Task ResponseContainsMaskedCardDetails()
    {
        var command = TestCommands.ValidPostPayment();

        var response = (await Handle(command)).Value;

        Assert.Equal("8877", response.CardNumberLastFour);
        Assert.Equal(command.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(command.ExpiryYear, response.ExpiryYear);
        Assert.Equal(command.Currency, response.Currency);
        Assert.Equal(command.Amount, response.Amount);
    }

    [Fact]
    public async Task SendsCardAndMoneyToBank()
    {
        var command = TestCommands.ValidPostPayment();

        await Handle(command);

        var call = Assert.Single(_bank.Calls);
        Assert.Equal(command.ToCardDetails(), call.Card);
        Assert.Equal(command.ToMoney(), call.Money);
    }

    [Fact]
    public void CommandToStringMasksCardNumberAndOmitsCvv()
    {
        var command = TestCommands.ValidPostPayment() with { Cvv = "987" };

        var text = command.ToString();

        Assert.DoesNotContain(command.CardNumber, text);
        Assert.DoesNotContain("987", text);
        Assert.Contains("****8877", text);
    }

    [Fact]
    public async Task RejectedByBankFailsWithoutStoringPayment()
    {
        _bank.Result = BankAuthorizationResult.Rejected;

        var result = await Handle(TestCommands.ValidPostPayment());

        Assert.True(result.IsFailure);
        Assert.Equal([ApplicationErrors.BankUnavailable], result.Errors);
        Assert.Empty(_repository.Payments);
    }

    [Theory]
    [MemberData(nameof(BankDecisions))]
    public async Task CountsProcessedPaymentByOutcomeAndCurrency(BankAuthorizationResult decision, PaymentStatus expectedStatus)
    {
        _bank.Result = decision;
        using var processed = ProcessedPayments();

        await Handle(TestCommands.ValidPostPayment());

        AssertCounted(processed, expectedStatus.ToString());
    }

    [Fact]
    public async Task CountsBankRejectionAsRejected()
    {
        _bank.Result = BankAuthorizationResult.Rejected;
        using var processed = ProcessedPayments();

        await Handle(TestCommands.ValidPostPayment());

        AssertCounted(processed, "Rejected");
    }

    private MetricCollector<long> ProcessedPayments() =>
        new(_meterFactory, PaymentMetrics.MeterName, "payments.processed");

    private static void AssertCounted(MetricCollector<long> collector, string expectedOutcome)
    {
        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(1, measurement.Value);
        Assert.Equal(expectedOutcome, measurement.Tags["outcome"]);
        Assert.Equal(TestCommands.ValidPostPayment().Currency, measurement.Tags["currency"]);
    }

    private ValueTask<Result<PostPaymentResponse>> Handle(PostPaymentCommand command) =>
        new PostPaymentCommandHandler(_bank, _repository, new PaymentMetrics(_meterFactory)).Handle(command, CancellationToken.None);
}
