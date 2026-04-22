using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazanlakEvents.Infrastructure.Repositories;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default)
        => await _context.Tickets
            .AsNoTracking()
            .Include(t => t.TicketType)
            .FirstOrDefaultAsync(t => t.QrCode == qrCode, ct);

    public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default)
        => await _context.Tickets
            .AsNoTracking()
            .Include(t => t.TicketType)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, ct);

    public async Task<IReadOnlyList<Ticket>> GetByHolderAsync(Guid holderId, CancellationToken ct = default)
        => await _context.Tickets
            .AsNoTracking()
            .Where(t => t.HolderId == holderId)
            .Include(t => t.TicketType).ThenInclude(tt => tt.Event).ThenInclude(e => e.Venue)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Ticket>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
        => await _context.Tickets
            .AsNoTracking()
            .Where(t => t.TicketType.EventId == eventId)
            .Include(t => t.TicketType)
            .OrderBy(t => t.TicketNumber)
            .ToListAsync(ct);

    public async Task<int> GetCheckedInCountAsync(Guid eventId, CancellationToken ct = default)
        => await _context.Tickets
            .Where(t => t.TicketType.EventId == eventId && t.CheckedInAt != null)
            .CountAsync(ct);
}
