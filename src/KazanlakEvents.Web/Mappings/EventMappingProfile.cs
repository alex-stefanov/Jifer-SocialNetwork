using AutoMapper;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Web.ViewModels.Api;
using KazanlakEvents.Web.ViewModels.Event;
using KazanlakEvents.Web.ViewModels.Volunteer;

namespace KazanlakEvents.Web.Mappings;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<VolunteerSignup, VolunteerSignupViewModel>()
            .ForMember(d => d.EventId,
                o => o.MapFrom(s => s.Shift.Task.EventId))
            .ForMember(d => d.EventTitle,
                o => o.MapFrom(s => s.Shift.Task.Event != null ? s.Shift.Task.Event.Title : string.Empty))
            .ForMember(d => d.EventSlug,
                o => o.MapFrom(s => s.Shift.Task.Event != null ? s.Shift.Task.Event.Slug : string.Empty))
            .ForMember(d => d.TaskName,
                o => o.MapFrom(s => s.Shift.Task.Name))
            .ForMember(d => d.ShiftStart,
                o => o.MapFrom(s => s.Shift.StartTime))
            .ForMember(d => d.ShiftEnd,
                o => o.MapFrom(s => s.Shift.EndTime));


        CreateMap<Event, EventCardViewModel>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.CategoryIcon,
                o => o.MapFrom(s => s.Category != null ? s.Category.IconCssClass : null))
            .ForMember(d => d.VenueName,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Name : null))
            .ForMember(d => d.VenueCity,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.City : null))
            .ForMember(d => d.AttendeeCount,
                o => o.MapFrom(s => s.Attendances.Count))
            .ForMember(d => d.AverageRating,
                o => o.MapFrom(s => s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0.0))
            .ForMember(d => d.CommentCount,
                o => o.MapFrom(s => s.Comments.Count))
            // Event-level coordinates take priority; fall back to venue coordinates when absent
            .ForMember(d => d.Latitude,
                o => o.MapFrom(s => s.Latitude.HasValue ? s.Latitude
                    : s.Venue != null ? (decimal?)s.Venue.Latitude : null))
            .ForMember(d => d.Longitude,
                o => o.MapFrom(s => s.Longitude.HasValue ? s.Longitude
                    : s.Venue != null ? (decimal?)s.Venue.Longitude : null));

        CreateMap<Event, EventDetailViewModel>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.CategoryIcon,
                o => o.MapFrom(s => s.Category != null ? s.Category.IconCssClass : null))
            .ForMember(d => d.VenueName,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Name : null))
            .ForMember(d => d.VenueAddress,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Address : null))
            .ForMember(d => d.VenueCity,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.City : null))
            .ForMember(d => d.Tags,
                o => o.MapFrom(s => s.EventTags
                    .Where(et => et.Tag != null)
                    .Select(et => et.Tag.Name)
                    .ToList()))
            .ForMember(d => d.Images,
                o => o.MapFrom(s => s.Images.OrderBy(i => i.SortOrder).ToList()))
            .ForMember(d => d.AverageRating,
                o => o.MapFrom(s => s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0.0))
            .ForMember(d => d.RatingCount,
                o => o.MapFrom(s => s.Ratings.Count))
            .ForMember(d => d.AttendeeCount,
                o => o.MapFrom(s => s.Attendances.Count))
            // OrganizerName/AvatarUrl require UserManager lookup; SeriesTitle requires a separate query;
            // user-context flags are set by the controller after mapping
            .ForMember(d => d.OrganizerName, o => o.Ignore())
            .ForMember(d => d.OrganizerAvatarUrl, o => o.Ignore())
            .ForMember(d => d.SeriesTitle, o => o.Ignore())
            .ForMember(d => d.IsOrganizer, o => o.Ignore())
            .ForMember(d => d.IsFavorited, o => o.Ignore())
            .ForMember(d => d.UserAttendance, o => o.Ignore())
            .ForMember(d => d.CurrentUserRating, o => o.Ignore());

        CreateMap<Event, EventEditViewModel>()
            .ForMember(d => d.ExistingCoverImageUrl, o => o.MapFrom(s => s.CoverImageUrl))
            .ForMember(d => d.SelectedTagIds,
                o => o.MapFrom(s => s.EventTags.Select(et => et.TagId).ToList()))
            .ForMember(d => d.CoverImage, o => o.Ignore())
            .ForMember(d => d.Categories, o => o.Ignore())
            .ForMember(d => d.Venues, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());

        CreateMap<EventCreateViewModel, Event>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Slug, o => o.Ignore())
            .ForMember(d => d.OrganizerId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.ViewCount, o => o.Ignore())
            .ForMember(d => d.CoverImageUrl, o => o.Ignore())
            .ForMember(d => d.ApprovedById, o => o.Ignore())
            .ForMember(d => d.ApprovedAt, o => o.Ignore())
            .ForMember(d => d.RejectionReason, o => o.Ignore())
            .ForMember(d => d.SeriesId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.ModifiedAt, o => o.Ignore())
            .ForMember(d => d.ModifiedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore())
            .ForMember(d => d.Venue, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Series, o => o.Ignore())
            .ForMember(d => d.EventTags, o => o.Ignore())
            .ForMember(d => d.Images, o => o.Ignore())
            .ForMember(d => d.TicketTypes, o => o.Ignore())
            .ForMember(d => d.Comments, o => o.Ignore())
            .ForMember(d => d.Ratings, o => o.Ignore())
            .ForMember(d => d.Favorites, o => o.Ignore())
            .ForMember(d => d.Attendances, o => o.Ignore())
            .ForMember(d => d.VolunteerTasks, o => o.Ignore())
            .ForMember(d => d.Sponsors, o => o.Ignore());

        // AvailableQuantity and IsAvailable are [NotMapped] computed properties — AutoMapper maps them by name convention
        CreateMap<TicketType, TicketTypeApiDto>();

        CreateMap<Event, EventApiDto>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.VenueName,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Name : null))
            .ForMember(d => d.VenueCity,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.City : null))
            .ForMember(d => d.LowestPrice,
                o => o.MapFrom(s => s.IsFree || !s.TicketTypes.Any()
                    ? (decimal?)null
                    : s.TicketTypes.Min(t => t.Price)))
            .ForMember(d => d.AttendeeCount,
                o => o.MapFrom(s => s.Attendances.Count))
            .ForMember(d => d.AverageRating,
                o => o.MapFrom(s => s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0.0));

        CreateMap<Event, EventApiDetailDto>()
            .ForMember(d => d.CategoryName,
                o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.VenueName,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Name : null))
            .ForMember(d => d.VenueCity,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.City : null))
            .ForMember(d => d.VenueAddress,
                o => o.MapFrom(s => s.Venue != null ? s.Venue.Address : null))
            .ForMember(d => d.Latitude,
                o => o.MapFrom(s => s.Latitude.HasValue ? s.Latitude
                    : s.Venue != null ? (decimal?)s.Venue.Latitude : null))
            .ForMember(d => d.Longitude,
                o => o.MapFrom(s => s.Longitude.HasValue ? s.Longitude
                    : s.Venue != null ? (decimal?)s.Venue.Longitude : null))
            .ForMember(d => d.LowestPrice,
                o => o.MapFrom(s => s.IsFree || !s.TicketTypes.Any()
                    ? (decimal?)null
                    : s.TicketTypes.Min(t => t.Price)))
            .ForMember(d => d.AttendeeCount,
                o => o.MapFrom(s => s.Attendances.Count))
            .ForMember(d => d.AverageRating,
                o => o.MapFrom(s => s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0.0))
            .ForMember(d => d.Tags,
                o => o.MapFrom(s => s.EventTags
                    .Where(et => et.Tag != null)
                    .Select(et => et.Tag.Name)
                    .ToList()))
            .ForMember(d => d.TicketTypes,
                o => o.MapFrom(s => s.TicketTypes.OrderBy(t => t.SortOrder).ToList()))
            // OrganizerUserName requires an Identity lookup unavailable at the mapping layer
            .ForMember(d => d.OrganizerUserName, o => o.Ignore());

        CreateMap<EventImage, EventImageViewModel>();

        CreateMap<Category, CategoryViewModel>()
            .ForMember(d => d.EventCount,
                o => o.MapFrom(s => s.Events.Count));

        // AvailableQuantity and IsAvailable are [NotMapped] computed properties — AutoMapper maps them by name convention
        CreateMap<TicketType, TicketTypeViewModel>();

        // AuthorName/AuthorAvatarUrl require Identity lookup — set by controller after mapping
        CreateMap<Comment, CommentViewModel>()
            .ForMember(d => d.AuthorName, o => o.Ignore())
            .ForMember(d => d.AuthorAvatarUrl, o => o.Ignore());
    }
}
