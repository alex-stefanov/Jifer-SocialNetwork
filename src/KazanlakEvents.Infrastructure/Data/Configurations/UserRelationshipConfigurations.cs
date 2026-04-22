using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KazanlakEvents.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // 1:1 relationship must be expressed in Fluent API
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        // 1:1 relationship must be expressed in Fluent API
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<NotificationPreference>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.HolderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.CheckedInById).OnDelete(DeleteBehavior.SetNull);
    }
}

public class UserWarningConfiguration : IEntityTypeConfiguration<UserWarning>
{
    public void Configure(EntityTypeBuilder<UserWarning> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(w => w.IssuedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ReviewedById).OnDelete(DeleteBehavior.ClientSetNull);
    }
}

public class EventSeriesConfiguration : IEntityTypeConfiguration<EventSeries>
{
    public void Configure(EntityTypeBuilder<EventSeries> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.OrganizerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(v => v.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VolunteerSignupConfiguration : IEntityTypeConfiguration<VolunteerSignup>
{
    public void Configure(EntityTypeBuilder<VolunteerSignup> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(vs => vs.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
