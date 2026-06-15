using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence;

public sealed class FlowDeskDbContext(DbContextOptions<FlowDeskDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);
