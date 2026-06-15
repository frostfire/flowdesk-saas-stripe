using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence;

public sealed class FlowDeskDbContext(DbContextOptions<FlowDeskDbContext> options) : DbContext(options);
