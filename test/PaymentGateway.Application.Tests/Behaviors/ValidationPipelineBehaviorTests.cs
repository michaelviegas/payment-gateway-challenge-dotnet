using FluentValidation;

using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Core.Behaviors;
using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Application.Tests.Behaviors;

public sealed class ValidationPipelineBehaviorTests
{
    private readonly Result<PostPaymentResponse> _handlerResult = Result.Success(new PostPaymentResponse { CardNumberLastFour = "8877", Currency = "GBP" });

    private int _handlerCalls;

    [Fact]
    public async Task ValidRequestIsPassedToHandler()
    {
        var result = await Handle(TestCommands.ValidPostPayment(), new PostPaymentValidator(TestCommands.Clock()));

        Assert.Same(_handlerResult, result);
        Assert.Equal(1, _handlerCalls);
    }

    [Fact]
    public async Task NoValidatorsPassesRequestToHandler()
    {
        var result = await Handle(TestCommands.ValidPostPayment() with { Amount = -1 });

        Assert.Same(_handlerResult, result);
    }

    [Fact]
    public async Task InvalidRequestFailsWithAnErrorPerFailureAndSkipsHandler()
    {
        var command = TestCommands.ValidPostPayment() with { Amount = 0, Cvv = "1" };

        var result = await Handle(command, new PostPaymentValidator(TestCommands.Clock()));

        Assert.True(result.IsFailure);
        Assert.Equal(["Amount", "Cvv"], result.Errors.Select(error => error.Value.Code).Order());
        Assert.Equal(0, _handlerCalls);
    }

    [Fact]
    public async Task CollectsErrorsFromEveryValidator()
    {
        var command = TestCommands.ValidPostPayment() with { Amount = 0 };

        var result = await Handle(command, new PostPaymentValidator(TestCommands.Clock()), new AlwaysFailsValidator());

        Assert.Equal(["Always", "Amount"], result.Errors.Select(error => error.Value.Code).Order());
    }

    private async Task<Result<PostPaymentResponse>> Handle(
        PostPaymentCommand command,
        params IValidator<PostPaymentCommand>[] validators)
    {
        var behavior = new ValidationPipelineBehavior<PostPaymentCommand, Result<PostPaymentResponse>>(validators);

        return await behavior.Handle(
            command,
            (_, _) =>
            {
                _handlerCalls++;
                return ValueTask.FromResult(_handlerResult);
            },
            CancellationToken.None);
    }

    private sealed class AlwaysFailsValidator : AbstractValidator<PostPaymentCommand>
    {
        public AlwaysFailsValidator() => RuleFor(command => command.CardNumber).Must(_ => false).OverridePropertyName("Always");
    }
}
