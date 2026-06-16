using FlowDesk.Api.Admin;
using FlowDesk.Api.Analytics;
using FlowDesk.Api.Auth;
using FlowDesk.Api.Billing;
using FlowDesk.Api.Cases;
using FlowDesk.Api.Webhooks;
using FlowDesk.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "JWT signing key is required.")
    .ValidateOnStart();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        var jwtTokenService = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(jwtOptions));
        options.TokenValidationParameters = jwtTokenService.CreateValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenType = context.Principal?.FindFirst(JwtTokenService.TokenTypeClaim)?.Value;
                if (tokenType != JwtTokenService.AccessTokenType)
                {
                    context.Fail("Invalid token type.");
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy("Spa", policy =>
    {
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Behind a reverse proxy that terminates TLS (nginx), honor the forwarded scheme so
// HTTPS redirection and link generation see the original https request. The API port is
// bound to loopback and only reachable through the proxy, so the forwarded headers are
// trusted from any source.
var forwardedHeaderOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeaderOptions.KnownIPNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaderOptions);

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyDatabaseMigrationsAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapBillingEndpoints();
app.MapCaseEndpoints();
app.MapAnalyticsEndpoints();
app.MapStripeWebhookEndpoints();
app.MapAdminEndpoints();

app.MapGet("/", () => Results.Redirect("/health"));

app.Run();

public partial class Program;
