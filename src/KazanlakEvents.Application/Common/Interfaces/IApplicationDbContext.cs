using KazanlakEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }
    DbSet<Venue> Venues { get; }
    DbSet<Category> Categories { get; }
    DbSet<Tag> Tags { get; }
    DbSet<EventTag> EventTags { get; }
    DbSet<EventImage> EventImages { get; }
    DbSet<EventSeries> EventSeries { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<TicketType> TicketTypes { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<Favorite> Favorites { get; }
    DbSet<EventAttendance> EventAttendances { get; }
    DbSet<Follow> Follows { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Report> Reports { get; }
    DbSet<Sponsor> Sponsors { get; }
    DbSet<EventSponsor> EventSponsors { get; }
    DbSet<UserWarning> UserWarnings { get; }
    DbSet<VolunteerTask> VolunteerTasks { get; }
    DbSet<VolunteerShift> VolunteerShifts { get; }
    DbSet<VolunteerSignup> VolunteerSignups { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<BlogCategory> BlogCategories { get; }
    DbSet<BlogPostTag> BlogPostTags { get; }
    DbSet<WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<OrganizedEvent> OrganizedEvents { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<OrganizedEventSponsor> OrganizedEventSponsors { get; }
    DbSet<OrganizerRequest> OrganizerRequests { get; }
    DbSet<VenueRequest> VenueRequests { get; }
    DbSet<BlogAuthorRequest> BlogAuthorRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
