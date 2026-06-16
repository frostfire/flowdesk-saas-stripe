using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowDesk.Contracts.Auth;
using FlowDesk.Contracts.Cases;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class CaseEndpointTests
{
    [Fact]
    public async Task ListCases_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/cases");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListCases_WithToken_ReturnsSeededCases()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var cases = await client.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");

        Assert.NotNull(cases);
        Assert.Equal(3, cases.Length);
        Assert.Contains(cases, item => item.Reference == "CF-1001");
    }

    [Fact]
    public async Task CaseMutations_ForProUser_PersistWithinUserStore()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        await SeedSubscriptionAsync(factory, auth.User.Id, PlanCode.Pro, SubscriptionStatus.Active);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Contract review", "Approve the amended terms.", "Tailspin Toys"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CaseDetailResponse>();
        Assert.NotNull(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/cases/{created.Id}",
            new UpdateCaseRequest("Contract review updated", "Approve the final terms.", "Tailspin Toys"));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CaseDetailResponse>();
        Assert.Equal("Contract review updated", updated?.Title);

        var approveResponse = await client.PostAsync($"/cases/{created.Id}/approve", content: null);
        approveResponse.EnsureSuccessStatusCode();
        var approved = await approveResponse.Content.ReadFromJsonAsync<CaseDetailResponse>();
        Assert.Equal("Approved", approved?.Status);

        var rejectResponse = await client.PostAsync($"/cases/{created.Id}/reject", content: null);
        rejectResponse.EnsureSuccessStatusCode();
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<CaseDetailResponse>();
        Assert.Equal("Rejected", rejected?.Status);

        var deleteResponse = await client.DeleteAsync($"/cases/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var cases = await client.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");
        Assert.DoesNotContain(cases ?? [], item => item.Id == created.Id);
    }

    [Fact]
    public async Task CaseMutations_ForFreeUser_ReturnForbidden()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var cases = await client.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");
        var caseId = cases?.First().Id ?? throw new InvalidOperationException("Seed case missing.");

        var create = await client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Free create", "Should fail.", "Northwind Components"));
        var update = await client.PutAsJsonAsync(
            $"/cases/{caseId}",
            new UpdateCaseRequest("Free update", "Should fail.", "Northwind Components"));
        var delete = await client.DeleteAsync($"/cases/{caseId}");
        var approve = await client.PostAsync($"/cases/{caseId}/approve", content: null);
        var reject = await client.PostAsync($"/cases/{caseId}/reject", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
    }

    [Fact]
    public async Task Cases_AreScopedPerUser()
    {
        using var factory = CreateFactory();
        using var firstClient = factory.CreateClient();
        using var secondClient = factory.CreateClient();
        var firstAuth = await RegisterAsync(firstClient);
        var secondAuth = await RegisterAsync(secondClient);
        await SeedSubscriptionAsync(factory, firstAuth.User.Id, PlanCode.Pro, SubscriptionStatus.Active);
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstAuth.AccessToken);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondAuth.AccessToken);

        var createResponse = await firstClient.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Private case", "Only one user sees this.", "Private Customer"));
        createResponse.EnsureSuccessStatusCode();

        var firstCases = await firstClient.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");
        var secondCases = await secondClient.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");

        Assert.Contains(firstCases ?? [], item => item.Title == "Private case");
        Assert.DoesNotContain(secondCases ?? [], item => item.Title == "Private case");
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest($"cases-{Guid.NewGuid():N}@flowdesk.test", "password1"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Register response was empty.");
    }

    private static async Task SeedSubscriptionAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        PlanCode plan,
        SubscriptionStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();
        var now = DateTimeOffset.UtcNow;

        dbContext.Subscriptions.Add(new SubscriptionRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanCode = plan,
            Status = status,
            StripeCustomerId = $"cus_{Guid.NewGuid():N}",
            StripeSubscriptionId = $"sub_{Guid.NewGuid():N}",
            StripePriceId = $"price_{plan}",
            CurrentPeriodEnd = now.AddMonths(1),
            CreatedAt = now,
            UpdatedAt = now,
        });

        await dbContext.SaveChangesAsync();
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"flowdesk-case-tests-{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<FlowDeskDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<FlowDeskDbContext>>();
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.AddDbContext<FlowDeskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }
}
