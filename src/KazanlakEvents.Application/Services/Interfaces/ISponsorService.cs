using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface ISponsorService
{
    Task<IReadOnlyList<Sponsor>> GetActiveSponsorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EventSponsor>> GetEventSponsorsAsync(Guid eventId, CancellationToken ct = default);

    Task<IReadOnlyList<Sponsor>> GetAllAsync(CancellationToken ct = default);
    Task<Sponsor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Sponsor> CreateAsync(Sponsor sponsor, CancellationToken ct = default);
    Task<Sponsor> UpdateAsync(Sponsor sponsor, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task AssociateSponsorWithEventAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default);
    Task RemoveSponsorFromEventAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default);

    Task IncrementImpressionAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default);
    Task IncrementClickAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default);
}
