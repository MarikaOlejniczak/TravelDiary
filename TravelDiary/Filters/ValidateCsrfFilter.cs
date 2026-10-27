
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TravelDiary.Filters
{
    public class ValidateCsrfFilter : IAsyncAuthorizationFilter
    {
        private readonly IAntiforgery _antiforgery;

        public ValidateCsrfFilter(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public async Task OnAuthorizationAsync(
            AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;

            // Żądania, które nie zmieniają danych.
            if (HttpMethods.IsGet(request.Method) ||
                HttpMethods.IsHead(request.Method) ||
                HttpMethods.IsOptions(request.Method) ||
                HttpMethods.IsTrace(request.Method))
            {
                return;
            }

            // Wymagamy tokenu CSRF dla wszystkich
            // pozostałych żądań API.
            try
            {
                await _antiforgery.ValidateRequestAsync(
                    context.HttpContext
                );
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new BadRequestObjectResult(
                    new
                    {
                        message =
                            "Nieprawidłowy lub brakujący token CSRF."
                    }
                );
            }
        }
    }
}
