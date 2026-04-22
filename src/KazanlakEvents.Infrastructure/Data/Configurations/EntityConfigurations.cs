using KazanlakEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KazanlakEvents.Infrastructure.Data.Configurations;

public class EventImageConfiguration : IEntityTypeConfiguration<EventImage>
{
    public void Configure(EntityTypeBuilder<EventImage> builder)
    {
        builder.HasOne(i => i.Event).WithMany(e => e.Images).HasForeignKey(i => i.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.HasOne(tt => tt.Event).WithMany(e => e.TicketTypes).HasForeignKey(tt => tt.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasOne(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(oi => oi.TicketType).WithMany().HasForeignKey(oi => oi.TicketTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TicketOrderItemConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasOne(t => t.TicketType).WithMany(tt => tt.Tickets).HasForeignKey(t => t.TicketTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany(oi => oi.Tickets).HasForeignKey(t => t.OrderItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.HasOne(p => p.Event).WithMany().HasForeignKey(p => p.EventId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        // Filtered unique index cannot be expressed as a data annotation
        builder.HasIndex(d => d.StripePaymentIntentId).IsUnique().HasFilter("[StripePaymentIntentId] IS NOT NULL");
        builder.HasOne(d => d.Campaign).WithMany(c => c.Donations).HasForeignKey(d => d.CampaignId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasOne(t => t.OrganizedEvent).WithMany(e => e.TeamMembers).HasForeignKey(t => t.OrganizedEventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrganizedEventSponsorConfiguration : IEntityTypeConfiguration<OrganizedEventSponsor>
{
    public void Configure(EntityTypeBuilder<OrganizedEventSponsor> builder)
    {
        builder.HasOne(s => s.OrganizedEvent).WithMany(e => e.Sponsors).HasForeignKey(s => s.OrganizedEventId).OnDelete(DeleteBehavior.Cascade);
    }
}
