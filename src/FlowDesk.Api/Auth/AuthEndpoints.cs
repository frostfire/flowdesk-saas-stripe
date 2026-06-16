using System.Security.Claims;
using FlowDesk.Contracts.Auth;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace FlowDesk.Api.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", Logout);
        group.MapGet("/whoami", WhoAmI).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokens,
        HttpContext httpContext)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(ToValidationErrors(result));
        }

        if (request.RememberMe)
        {
            SetRefreshCookie(httpContext, tokens.CreateRefreshToken(user));
        }

        return Results.Ok(tokens.CreateToken(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokens,
        HttpContext httpContext)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return Results.Unauthorized();
        }

        if (request.RememberMe)
        {
            SetRefreshCookie(httpContext, tokens.CreateRefreshToken(user));
        }

        return Results.Ok(tokens.CreateToken(user));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokens)
    {
        if (!httpContext.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            return Results.Unauthorized();
        }

        ClaimsPrincipal principal;
        try
        {
            principal = tokens.ValidateRefreshToken(refreshToken) ?? throw new SecurityTokenException("Invalid refresh token.");
        }
        catch (SecurityTokenException)
        {
            ClearRefreshCookie(httpContext);
            return Results.Unauthorized();
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            ClearRefreshCookie(httpContext);
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            ClearRefreshCookie(httpContext);
            return Results.Unauthorized();
        }

        return Results.Ok(tokens.CreateToken(user));
    }

    private static IResult Logout(HttpContext httpContext)
    {
        ClearRefreshCookie(httpContext);
        return Results.NoContent();
    }

    private static IResult WhoAmI(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? string.Empty;
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;

        return Results.Ok(new WhoAmIResponse(id, email));
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result)
    {
        return result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
    }

    private const string RefreshCookieName = "flowdesk_refresh";

    private static void SetRefreshCookie(HttpContext httpContext, string token)
    {
        httpContext.Response.Cookies.Append(
            RefreshCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/auth",
            });
    }

    private static void ClearRefreshCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(
            RefreshCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/auth",
            });
    }
}
