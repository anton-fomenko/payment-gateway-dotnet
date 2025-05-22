using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace PaymentGateway.Api.Validation
{
    public class ValidationFilter(ProblemDetailsFactory problemDetailsFactory) : IActionFilter
    {
        private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var modelState = context.ModelState;

            if (!modelState.IsValid)
            {
                // Use the default ProblemDetailsFactory to create ValidationProblemDetails
                var problemDetails = _problemDetailsFactory.CreateValidationProblemDetails(
                    context.HttpContext,
                    modelState,
                    statusCode: StatusCodes.Status400BadRequest);

                // Customize the Title
                problemDetails.Title = "Payment Rejected";

                context.Result = new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action required after execution
        }
    }
}