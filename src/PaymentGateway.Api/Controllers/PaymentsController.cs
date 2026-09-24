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
public class PaymentsController(ISender sender) : Controller
{
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCommand(idempotencyKey), cancellationToken);

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

        if (error == ApplicationErrors.PaymentRejected || error == ApplicationErrors.IdempotencyKeyMissing)
        {
            return Problem(error, StatusCodes.Status400BadRequest);
        }

        if (error == ApplicationErrors.IdempotentRequestInProgress)
        {
            return Problem(error, StatusCodes.Status409Conflict);
        }

        foreach (var validationError in errors)
        {
            ModelState.AddModelError(validationError.Value.Code, validationError.Value.Message);
        }

        return ValidationProblem(ModelState);
    }

    private ObjectResult Problem(Error error, int statusCode) =>
        Problem(title: error.Value.Code, detail: error.Value.Message, statusCode: statusCode);
}
