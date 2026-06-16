using System.Net.Http.Json;
using FlowDesk.Application.CaseFlow;
using FlowDesk.Contracts.Cases;

namespace FlowDesk.Infrastructure.CaseFlow;

public sealed class HttpCaseFlowClient : ICaseFlowClient
{
    private readonly HttpClient _httpClient;

    public HttpCaseFlowClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<CaseSummaryResponse>> ListCasesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<CaseSummaryResponse[]>("/cases", cancellationToken) ?? [];
    }

    public Task<CaseDetailResponse?> GetCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        return _httpClient.GetFromJsonAsync<CaseDetailResponse>($"/cases/{Uri.EscapeDataString(id)}", cancellationToken);
    }

    public async Task<CaseDetailResponse> CreateCaseAsync(
        string userId,
        CreateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/cases", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CaseDetailResponse>(cancellationToken)
            ?? throw new InvalidOperationException("CaseFlow create case response was empty.");
    }

    public async Task<CaseDetailResponse?> UpdateCaseAsync(
        string userId,
        string id,
        UpdateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/cases/{Uri.EscapeDataString(id)}", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CaseDetailResponse>(cancellationToken);
    }

    public async Task<bool> DeleteCaseAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/cases/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public Task<CaseDetailResponse?> ApproveCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        return PostCaseActionAsync(id, "approve", cancellationToken);
    }

    public Task<CaseDetailResponse?> RejectCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        return PostCaseActionAsync(id, "reject", cancellationToken);
    }

    private async Task<CaseDetailResponse?> PostCaseActionAsync(
        string id,
        string action,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            $"/cases/{Uri.EscapeDataString(id)}/{action}",
            content: null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CaseDetailResponse>(cancellationToken);
    }
}
