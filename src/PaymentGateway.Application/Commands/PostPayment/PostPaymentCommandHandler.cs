using PaymentGateway.Application.Abstractions.Bank;
using PaymentGateway.Application.Abstractions.Data;
using PaymentGateway.Application.Abstractions.Messaging;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Domain;

namespace PaymentGateway.Application.Commands.PostPayment;

internal sealed class PostPaymentCommandHandler(
    IBankClient bankClient,
    IPaymentRepository paymentRepository) : ICommandHandler<PostPaymentCommand, PostPaymentResponse>
{
    public async ValueTask<Result<PostPaymentResponse>> Handle(PostPaymentCommand command, CancellationToken cancellationToken)
    {
        var result = await bankClient.AuthorizeAsync(command.CardDetails, command.Money, CancellationToken.None);

        if (result.PaymentRejected)
        {
            return Result.Failure<PostPaymentResponse>(ApplicationErrors.PaymentRejected);
        }

        var payment = Payment.Create(result.PaymentStatus, command.CardDetails.ToCardInfo, command.Money);
        paymentRepository.Add(payment);

        return Result.Success(PostPaymentResponse.Create(payment));
    }
}
