namespace FlowDesk.Infrastructure.CaseFlow;

public sealed class CaseFlowOptions
{
    public string ClientMode { get; init; } = "Fake";

    public string BaseUrl { get; init; } = "http://localhost:5000";
}
