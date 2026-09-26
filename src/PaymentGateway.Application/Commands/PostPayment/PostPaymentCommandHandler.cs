using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Application.Core.Diagnostics;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Domain;

namespace PaymentGateway.Application.Commands.PostPayment;

internal sealed class PostPaymentCommandHandler(
    IBankClient bankClient,
    IPaymentRepository paymentRepository,
    PaymentMetrics metrics) : ICommandHandler<PostPaymentCommand, PostPaymentResponse>
{
    public async ValueTask<Result<PostPaymentResponse>> Handle(PostPaymentCommand command, CancellationToken cancellationToken)
    {
        var card = command.ToCardDetails();
        var money = command.ToMoney();

        var result = await bankClient.AuthorizeAsync(card, money, cancellationToken);

        if (result.PaymentRejected)
        {
            metrics.PaymentRejected(command.Currency);
            return Result.Failure<PostPaymentResponse>(ApplicationErrors.BankUnavailable);
        }

        var payment = Payment.Create(result.PaymentStatus, card.ToCardInfo, money);
        paymentRepository.Add(payment);
        metrics.PaymentProcessed(payment.Status, command.Currency);

        return Result.Success(PostPaymentResponse.Create(payment));
    }
}
