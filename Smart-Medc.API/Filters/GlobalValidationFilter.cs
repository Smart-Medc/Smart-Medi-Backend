using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smart_Medc.API.Filters
{
    public class GlobalValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(err => new
                    {
                        Field = ms.Key,
                        Error = err.ErrorMessage
                    })).ToList();

                context.Result = new BadRequestObjectResult(new
                {
                    Success = false,
                    Message = "Validation failed.",
                    Errors = errors
                });

                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
