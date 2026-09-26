using Mediator;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Application.Commands.PostPayment;
using PaymentGateway.Application.Core.Primitives;
using PaymentGateway.Application.Queries.GetPayment;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(), cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return Failure(result.Errors);
        }

        return Ok(result.Value);
    }

    private ActionResult Failure(IReadOnlyList<Error> errors)
    {
        var error = errors[0];

        if (error == ApplicationErrors.PaymentNotFound)
        {
            return NotFound();
        }

        if (error == ApplicationErrors.BankUnavailable)
        {
            return Problem(error, StatusCodes.Status400BadRequest);
        }

        foreach (var validationError in errors)
        {
            ModelState.AddModelError(validationError.Value.Code, validationError.Value.Message);
        }

        var rejected = ApplicationErrors.PaymentRejected.Value;

        return ValidationProblem(title: rejected.Code, detail: rejected.Message, modelStateDictionary: ModelState);
    }

    private ObjectResult Problem(Error error, int statusCode) =>
        Problem(title: error.Value.Code, detail: error.Value.Message, statusCode: statusCode);
}
