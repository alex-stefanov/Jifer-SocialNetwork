using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Domain.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<Ticket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetByHolderAsync(Guid holderId, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<int> GetCheckedInCountAsync(Guid eventId, CancellationToken ct = default);
}
