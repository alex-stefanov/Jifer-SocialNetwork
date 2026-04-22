using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KazanlakEvents.Infrastructure.Data.Configurations;

public class EventTagConfiguration : IEntityTypeConfiguration<EventTag>
{
    public void Configure(EntityTypeBuilder<EventTag> builder)
    {
        builder.HasKey(et => new { et.EventId, et.TagId });
        builder.HasOne(et => et.Event).WithMany(e => e.EventTags).HasForeignKey(et => et.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(et => et.Tag).WithMany(t => t.EventTags).HasForeignKey(et => et.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventSponsorConfiguration : IEntityTypeConfiguration<EventSponsor>
{
    public void Configure(EntityTypeBuilder<EventSponsor> builder)
    {
        builder.HasKey(es => new { es.EventId, es.SponsorId });
        builder.HasOne(es => es.Event).WithMany(e => e.Sponsors).HasForeignKey(es => es.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(es => es.Sponsor).WithMany(s => s.EventSponsors).HasForeignKey(es => es.SponsorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlogPostTagConfiguration : IEntityTypeConfiguration<BlogPostTag>
{
    public void Configure(EntityTypeBuilder<BlogPostTag> builder)
    {
        builder.HasKey(bpt => new { bpt.BlogPostId, bpt.TagId });
        builder.HasOne(bpt => bpt.BlogPost).WithMany(p => p.Tags).HasForeignKey(bpt => bpt.BlogPostId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(bpt => bpt.Tag).WithMany(t => t.BlogPostTags).HasForeignKey(bpt => bpt.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => new { f.UserId, f.EventId });
        builder.HasOne(f => f.Event).WithMany(e => e.Favorites).HasForeignKey(f => f.EventId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => new { f.FollowerId, f.FolloweeId });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(f => f.FolloweeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
{
    public void Configure(EntityTypeBuilder<EventAttendance> builder)
    {
        builder.HasKey(ea => new { ea.UserId, ea.EventId });
        builder.HasOne(ea => ea.Event).WithMany(e => e.Attendances).HasForeignKey(ea => ea.EventId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(ea => ea.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.HasOne(r => r.Event).WithMany(e => e.Ratings).HasForeignKey(r => r.EventId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasOne(c => c.Event).WithMany(e => e.Comments).HasForeignKey(c => c.EventId);
        builder.HasOne(c => c.ParentComment).WithMany(c => c.Replies).HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
