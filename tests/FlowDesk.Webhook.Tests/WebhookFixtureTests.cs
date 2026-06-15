using System.Text.Json;
using Xunit;

namespace FlowDesk.Webhook.Tests;

public sealed class WebhookFixtureTests
{
    [Fact]
    public void FixtureJson_CanReadStripeEventId()
    {
        const string fixture = """
            {
              "id": "evt_test_flowdesk",
              "type": "checkout.session.completed"
            }
            """;

        using var document = JsonDocument.Parse(fixture);

        Assert.Equal("evt_test_flowdesk", document.RootElement.GetProperty("id").GetString());
    }
}
