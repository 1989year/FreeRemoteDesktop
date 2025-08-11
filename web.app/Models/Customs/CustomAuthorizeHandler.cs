using System.Security.Claims;
using System.Text;

namespace Microsoft.AspNetCore.Authentication;

public class CustomAuthorizeHandler: IAuthenticationHandler, IAuthenticationSignInHandler, IAuthenticationSignOutHandler
{
    public static string AuthenticationScheme { get => Convert.ToHexStringLower(Encoding.UTF8.GetBytes(nameof(CustomAuthorizeHandler))); }

    public AuthenticationScheme Scheme { get; private set; }

    protected HttpContext Context { get; private set; }

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        Scheme = scheme;
        Context = context;
        return Task.CompletedTask;
    }

    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        return await Task.Run(() => {
            try {
                var s = Context.Request.Cookies[Scheme.Name];
                if (string.IsNullOrEmpty(s)) {
                    throw new ArgumentNullException();
                }
                return AuthenticateResult.Success((new TicketSerializer()).Deserialize(Convert.FromBase64String(s)));
            } catch {
                return AuthenticateResult.NoResult();
            }
        }).ConfigureAwait(false);
    }

    public Task ChallengeAsync(AuthenticationProperties properties)
    {
        Context.Response.StatusCode = 403;
        Context.Response.WriteAsync("<!DOCTYPE html>\r\n" +
            "<html>\r\n" +
            "<body>\r\n" +
            "<script>\r\n" +
            $"location='/identity/signin?_={Environment.TickCount64:x}'\r\n" +
            "</script>\r\n" +
            "</body>\r\n" +
            "</html>").ConfigureAwait(false);
        return Task.CompletedTask;
    }

    public Task ForbidAsync(AuthenticationProperties properties)
    {
        Context.Response.StatusCode = 403;
        Context.Response.WriteAsync("<!DOCTYPE html>\r\n" +
            "<html>\r\n" +
            "<body>\r\n" +
            "<script>\r\n" +
            $"location='/identity/signin?_={Environment.TickCount64:x}'\r\n" +
            "</script>\r\n" +
            "</body>\r\n" +
            "</html>").ConfigureAwait(false);
        return Task.CompletedTask;
    }

    public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
    {
        var ticket = new AuthenticationTicket(user, properties, Scheme.Name);
        Context.Response.Cookies.Append(Scheme.Name, Convert.ToBase64String((new TicketSerializer()).Serialize(ticket)));
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties properties)
    {
        Context.Response.Cookies.Delete(Scheme.Name);
        return Task.CompletedTask;
    }
}