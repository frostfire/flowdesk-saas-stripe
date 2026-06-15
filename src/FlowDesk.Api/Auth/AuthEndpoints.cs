using System.Security.Claims;
using FlowDesk.Contracts.Auth;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FlowDesk.Api.Auth;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/whoami", WhoAmI).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokens)
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

        return Results.Ok(tokens.CreateToken(user));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokens)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var isValidPassword = await userManager.CheckPasswordAsync(user, request.Password);
        return isValidPassword ? Results.Ok(tokens.CreateToken(user)) : Results.Unauthorized();
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
}
