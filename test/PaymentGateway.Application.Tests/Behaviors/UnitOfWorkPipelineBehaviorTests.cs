using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Core.Behaviors;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Application.Tests.Fakes;

namespace PaymentGateway.Application.Tests.Behaviors;

public sealed class UnitOfWorkPipelineBehaviorTests
{
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task SavesChangesWhenCommandSucceeds()
    {
        var expected = Result.Success(new PostPaymentResponse { CardNumberLastFour = "8877", Currency = "GBP" });

        var result = await Handle(expected);

        Assert.Same(expected, result);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DoesNotSaveWhenCommandFails()
    {
        var expected = Result.Failure<PostPaymentResponse>(ApplicationErrors.BankUnavailable);

        var result = await Handle(expected);

        Assert.Same(expected, result);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    private async Task<Result<PostPaymentResponse>> Handle(Result<PostPaymentResponse> handlerResult)
    {
        var behavior = new UnitOfWorkPipelineBehavior<PostPaymentCommand, Result<PostPaymentResponse>>(
            _unitOfWork,
            NullLogger<UnitOfWorkPipelineBehavior<PostPaymentCommand, Result<PostPaymentResponse>>>.Instance);

        return await behavior.Handle(
            TestCommands.ValidPostPayment(),
            (_, _) => ValueTask.FromResult(handlerResult),
            CancellationToken.None);
    }
}
