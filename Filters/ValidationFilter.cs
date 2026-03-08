using AuthAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthAPI.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => new FieldError
            {
                Field = e.Key,
                Message = e.Value!.Errors.First().ErrorMessage
            })
            .ToList();

        context.Result = new BadRequestObjectResult(
            ApiErrorResponse.Create("VALIDATION_ERROR", "One or more validation errors occurred.", errors));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
