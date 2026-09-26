using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using PaymentGateway.Application.Core.Primitives;

namespace PaymentGateway.Api.Models;

internal static class RejectedRequestResponse
{
    private const string RequestField = "Request";

    public static IActionResult Create(ActionContext context)
    {
        var errors = new ModelStateDictionary();

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0 || context.ActionDescriptor.Parameters.Any(parameter => parameter.Name == key))
            {
                continue;
            }

            if (IsJsonPath(key))
            {
                var field = FieldName(key);
                errors.AddModelError(field, field == RequestField
                    ? "The request body must be a valid JSON payment request."
                    : $"{field} has an invalid value.");
            }
            else
            {
                foreach (var error in entry.Errors)
                {
                    errors.AddModelError(key, error.ErrorMessage);
                }
            }
        }

        if (errors.ErrorCount == 0)
        {
            errors.AddModelError(RequestField, "The request body must be a valid JSON payment request.");
        }

        var rejected = ApplicationErrors.PaymentRejected.Value;
        var problem = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>()
            .CreateValidationProblemDetails(
                context.HttpContext,
                errors,
                StatusCodes.Status400BadRequest,
                title: rejected.Code,
                detail: rejected.Message);

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    private static bool IsJsonPath(string key) => key.Length == 0 || key.StartsWith('$');

    private static string FieldName(string key)
    {
        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : string.Empty;

        return name.Length == 0 ? RequestField : char.ToUpperInvariant(name[0]) + name[1..];
    }
}
