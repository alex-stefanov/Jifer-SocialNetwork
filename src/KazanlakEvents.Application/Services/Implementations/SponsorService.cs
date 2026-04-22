using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Application.Services.Implementations;

public class SponsorService(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork) : ISponsorService
{
    public async Task<IReadOnlyList<Sponsor>> GetActiveSponsorsAsync(CancellationToken ct = default)
        => await db.Sponsors
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Tier)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EventSponsor>> GetEventSponsorsAsync(Guid eventId, CancellationToken ct = default)
        => await db.EventSponsors
            .Include(es => es.Sponsor)
            .Where(es => es.EventId == eventId && es.Sponsor.IsActive)
            .OrderByDescending(es => es.Sponsor.Tier)
            .ThenBy(es => es.Sponsor.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Sponsor>> GetAllAsync(CancellationToken ct = default)
        => await db.Sponsors
            .OrderByDescending(s => s.Tier)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Sponsor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Sponsors.FindAsync(new object[] { id }, ct);

    public async Task<Sponsor> CreateAsync(Sponsor sponsor, CancellationToken ct = default)
    {
        db.Sponsors.Add(sponsor);
        await unitOfWork.SaveChangesAsync(ct);
        return sponsor;
    }

    public async Task<Sponsor> UpdateAsync(Sponsor sponsor, CancellationToken ct = default)
    {
        var existing = await db.Sponsors.FindAsync(new object[] { sponsor.Id }, ct)
            ?? throw new InvalidOperationException("Sponsor not found.");

        existing.Name        = sponsor.Name;
        existing.LogoUrl     = sponsor.LogoUrl;
        existing.WebsiteUrl  = sponsor.WebsiteUrl;
        existing.Description = sponsor.Description;
        existing.Tier        = sponsor.Tier;
        existing.IsActive    = sponsor.IsActive;

        await unitOfWork.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sponsor = await db.Sponsors.FindAsync(new object[] { id }, ct)
            ?? throw new InvalidOperationException("Sponsor not found.");

        db.Sponsors.Remove(sponsor);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task AssociateSponsorWithEventAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default)
    {
        var exists = await db.EventSponsors
            .AnyAsync(es => es.EventId == eventId && es.SponsorId == sponsorId, ct);

        if (!exists)
        {
            db.EventSponsors.Add(new EventSponsor
            {
                EventId   = eventId,
                SponsorId = sponsorId
            });
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveSponsorFromEventAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default)
    {
        var link = await db.EventSponsors
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.SponsorId == sponsorId, ct);

        if (link != null)
        {
            db.EventSponsors.Remove(link);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementImpressionAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default)
    {
        var link = await db.EventSponsors
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.SponsorId == sponsorId, ct);

        if (link != null)
        {
            link.ImpressionCount++;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementClickAsync(Guid eventId, Guid sponsorId, CancellationToken ct = default)
    {
        var link = await db.EventSponsors
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.SponsorId == sponsorId, ct);

        if (link != null)
        {
            link.ClickCount++;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
