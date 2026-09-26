using Mediator;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Application.Queries.GetPayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.Controllers;

public sealed class PaymentsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        var services = new ServiceCollection().AddLogging().AddMvcCore().Services.BuildServiceProvider();

        _controller = new PaymentsController(_sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }

    [Fact]
    public async Task PostReturnsOkWithPayment()
    {
        var payment = new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatus.Authorized, CardNumberLastFour = "8877", Currency = "GBP" };
        GivenPostPaymentReturns(Result.Success(payment));

        var result = await _controller.PostPaymentAsync(ValidRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(payment, ok.Value);
    }

    [Fact]
    public async Task PostSendsRequestAsCommand()
    {
        GivenPostPaymentReturns(Result.Success(new PostPaymentResponse { CardNumberLastFour = "8877", Currency = "GBP" }));
        var request = ValidRequest();

        await _controller.PostPaymentAsync(request, CancellationToken.None);

        await _sender.Received(1).Send(request.ToCommand(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostBankUnavailableReturnsBadRequestProblem()
    {
        GivenPostPaymentReturns(Result.Failure<PostPaymentResponse>(ApplicationErrors.BankUnavailable));

        var result = await _controller.PostPaymentAsync(ValidRequest(), CancellationToken.None);

        var problem = AssertProblem<ProblemDetails>(result.Result);
        Assert.Equal(ApplicationErrors.BankUnavailable.Value.Code, problem.Title);
        Assert.Equal(ApplicationErrors.BankUnavailable.Value.Message, problem.Detail);
    }

    [Fact]
    public async Task PostValidationFailureReturnsValidationProblemWithEveryError()
    {
        GivenPostPaymentReturns(Result.Failure<PostPaymentResponse>(
        [
            Error.From(new ErrorRecord("Amount", "Amount must be greater than zero.")),
            Error.From(new ErrorRecord("Cvv", "CVV is required."))
        ]));

        var result = await _controller.PostPaymentAsync(ValidRequest(), CancellationToken.None);

        var problem = AssertProblem<ValidationProblemDetails>(result.Result);
        Assert.Equal(ApplicationErrors.PaymentRejected.Value.Code, problem.Title);
        Assert.Equal(["Amount must be greater than zero."], problem.Errors["Amount"]);
        Assert.Equal(["CVV is required."], problem.Errors["Cvv"]);
    }

    [Fact]
    public async Task GetReturnsOkWithPayment()
    {
        var id = Guid.NewGuid();
        var payment = new GetPaymentResponse { Id = id, Status = PaymentStatus.Declined, CardNumberLastFour = "8877", Currency = "USD" };
        _sender.Send(new GetPaymentQuery(id), Arg.Any<CancellationToken>()).Returns(Result.Success(payment));

        var result = await _controller.GetPaymentAsync(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(payment, ok.Value);
    }

    [Fact]
    public async Task GetUnknownPaymentReturnsNotFound()
    {
        _sender.Send(Arg.Any<GetPaymentQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<GetPaymentResponse>(ApplicationErrors.PaymentNotFound));

        var result = await _controller.GetPaymentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private void GivenPostPaymentReturns(Result<PostPaymentResponse> result) =>
        _sender.Send(Arg.Any<PostPaymentCommand>(), Arg.Any<CancellationToken>()).Returns(result);

    private static T AssertProblem<T>(IActionResult? result) where T : ProblemDetails
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        var problem = Assert.IsType<T>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);

        return problem;
    }

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };
}
