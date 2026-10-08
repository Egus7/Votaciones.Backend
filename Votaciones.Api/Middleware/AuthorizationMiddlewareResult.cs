using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Votaciones.Api.Middleware
{
    public class AuthorizationMiddlewareResult : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate requestDelegate, HttpContext httpContext, AuthorizationPolicy authorizationPolicy, 
            PolicyAuthorizationResult policyAuthorizationResult)
        {

            if (policyAuthorizationResult.Forbidden)
            {
                var mensaje = policyAuthorizationResult.AuthorizationFailure?.FailureReasons.FirstOrDefault()?.Message 
                    ?? "No tiene permisos para realizar esta acción.";

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                httpContext.Response.ContentType = "application/json";

                await httpContext.Response.WriteAsJsonAsync(new
                {
                    message = mensaje
                });
                return;
            }
            await _defaultHandler.HandleAsync(requestDelegate, httpContext, authorizationPolicy, policyAuthorizationResult);
        }

    }
}
