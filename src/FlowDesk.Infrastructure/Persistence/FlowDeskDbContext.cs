using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Billing;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence;

public sealed class FlowDeskDbContext(DbContextOptions<FlowDeskDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BillingCustomer> BillingCustomers => Set<BillingCustomer>();

    public DbSet<SubscriptionRecord> Subscriptions => Set<SubscriptionRecord>();

    public DbSet<StripeWebhookEventRecord> StripeWebhookEvents => Set<StripeWebhookEventRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BillingCustomer>(entity =>
        {
            entity.ToTable("BillingCustomers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.UserId).IsRequired();
            entity.Property(customer => customer.StripeCustomerId).IsRequired();
            entity.Property(customer => customer.CreatedAt).IsRequired();
            entity.Property(customer => customer.UpdatedAt).IsRequired();
            entity.HasIndex(customer => customer.UserId).IsUnique();
            entity.HasIndex(customer => customer.StripeCustomerId).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(customer => customer.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionRecord>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(subscription => subscription.Id);
            entity.Property(subscription => subscription.UserId).IsRequired();
            entity.Property(subscription => subscription.PlanCode)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue(PlanCode.Free);
            entity.Property(subscription => subscription.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue(SubscriptionStatus.None);
            entity.Property(subscription => subscription.CancelAtPeriodEnd).HasDefaultValue(false);
            entity.Property(subscription => subscription.CreatedAt).IsRequired();
            entity.Property(subscription => subscription.UpdatedAt).IsRequired();
            entity.HasIndex(subscription => subscription.UserId).IsUnique();
            entity.HasIndex(subscription => subscription.StripeSubscriptionId)
                .IsUnique()
                .HasFilter("\"StripeSubscriptionId\" IS NOT NULL");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(subscription => subscription.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StripeWebhookEventRecord>(entity =>
        {
            entity.ToTable("StripeWebhookEvents");
            entity.HasKey(webhookEvent => webhookEvent.Id);
            entity.Property(webhookEvent => webhookEvent.StripeEventId).IsRequired();
            entity.Property(webhookEvent => webhookEvent.Type).IsRequired();
            entity.Property(webhookEvent => webhookEvent.ReceivedAt).IsRequired();
            entity.HasIndex(webhookEvent => webhookEvent.StripeEventId).IsUnique();
        });
    }
}
