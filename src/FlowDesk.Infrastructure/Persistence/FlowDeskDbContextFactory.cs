using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowDesk.Infrastructure.Persistence;

public sealed class FlowDeskDbContextFactory : IDesignTimeDbContextFactory<FlowDeskDbContext>
{
    public FlowDeskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FlowDeskDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=flowdesk;Username=flowdesk;Password=flowdesk_dev_password")
            .Options;

        return new FlowDeskDbContext(options);
    }
}
