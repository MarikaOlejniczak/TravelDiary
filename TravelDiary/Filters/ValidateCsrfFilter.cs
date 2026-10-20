
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

            // Żądania odczytujące dane nie wymagają CSRF.
            if (HttpMethods.IsGet(request.Method) ||
                HttpMethods.IsHead(request.Method) ||
                HttpMethods.IsOptions(request.Method) ||
                HttpMethods.IsTrace(request.Method))
            {
                return;
            }

            // Nagłówek Bearer ma pierwszeństwo przed ciasteczkiem.
            // Samodzielne uwierzytelnianie Bearer nie wymaga CSRF.
            var authorization =
                request.Headers.Authorization.ToString();

            if (authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Jeżeli przeglądarka nie wysyła ciasteczka
            // uwierzytelniającego, nie ma sesji cookie do ochrony.
            if (!request.Cookies.ContainsKey("TravelDiaryAuth"))
            {
                return;
            }

            // Sprawdzamy token CSRF dla żądań z ciasteczkiem.
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
