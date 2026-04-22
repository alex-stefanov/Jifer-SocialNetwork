using KazanlakEvents.Domain.Entities;

namespace KazanlakEvents.Application.Services.Interfaces;

public interface ITicketService
{
    Task<IReadOnlyList<TicketType>> GetTicketTypesForEventAsync(Guid eventId, CancellationToken ct = default);
    Task<TicketType> CreateTicketTypeAsync(TicketType ticketType, CancellationToken ct = default);
    Task<TicketType> UpdateTicketTypeAsync(TicketType ticketType, CancellationToken ct = default);
    Task DeleteTicketTypeAsync(Guid ticketTypeId, CancellationToken ct = default);

    Task<Order?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> RegisterForEventAsync(Guid eventId, Guid userId, Guid ticketTypeId, int quantity = 1, CancellationToken ct = default);
    Task<Ticket?> GetTicketByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<Ticket> CheckInTicketAsync(string qrCode, Guid checkedInById, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> GetUserTicketsAsync(Guid userId, CancellationToken ct = default);
    Task<Ticket> TransferTicketAsync(Guid ticketId, Guid newHolderId, CancellationToken ct = default);
    Task<Ticket> TransferTicketToEmailAsync(Guid ticketId, string recipientEmail, CancellationToken ct = default);
    Task CancelTicketAsync(Guid ticketId, CancellationToken ct = default);
}
