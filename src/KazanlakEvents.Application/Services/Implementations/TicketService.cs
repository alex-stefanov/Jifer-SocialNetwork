using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Application.Services.Implementations;

public class TicketService(
    ITicketRepository ticketRepository,
    IRepository<TicketType> ticketTypeRepository,
    IRepository<Order> orderRepository,
    IRepository<OrderItem> orderItemRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    IQrCodeService qrCodeService,
    IFileStorageService fileStorageService,
    IEmailService emailService,
    ILogger<TicketService> logger) : ITicketService
{
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;

    public async Task<IReadOnlyList<TicketType>> GetTicketTypesForEventAsync(
        Guid eventId, CancellationToken ct = default)
    {
        return await ticketTypeRepository.FindAsync(tt => tt.EventId == eventId, ct);
    }

    public async Task<TicketType> CreateTicketTypeAsync(
        TicketType ticketType, CancellationToken ct = default)
    {
        var created = await ticketTypeRepository.AddAsync(ticketType, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Created TicketType {Id} for Event {EventId}", created.Id, created.EventId);
        return created;
    }

    public async Task<TicketType> UpdateTicketTypeAsync(
        TicketType ticketType, CancellationToken ct = default)
    {
        var existing = await ticketTypeRepository.GetByIdAsync(ticketType.Id, ct)
            ?? throw new NotFoundException(nameof(TicketType), ticketType.Id);

        existing.Name = ticketType.Name;
        existing.Description = ticketType.Description;
        existing.Quantity = ticketType.Quantity;
        existing.SalesStartDate = ticketType.SalesStartDate;
        existing.SalesEndDate = ticketType.SalesEndDate;
        existing.MaxPerOrder = ticketType.MaxPerOrder;
        existing.SortOrder = ticketType.SortOrder;

        ticketTypeRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteTicketTypeAsync(Guid ticketTypeId, CancellationToken ct = default)
    {
        var tt = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
            ?? throw new NotFoundException(nameof(TicketType), ticketTypeId);

        ticketTypeRepository.Remove(tt);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Deleted TicketType {Id}", ticketTypeId);
    }

    public async Task<Order?> GetOrderWithDetailsAsync(Guid orderId, CancellationToken ct = default)
        => await orderRepository.Query()
            .Where(o => o.Id == orderId)
            .Include(o => o.Items).ThenInclude(i => i.TicketType)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Ticket>> RegisterForEventAsync(
        Guid eventId, Guid userId, Guid ticketTypeId, int quantity = 1,
        CancellationToken ct = default)
    {
        var tt = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
            ?? throw new NotFoundException(nameof(TicketType), ticketTypeId);

        if (tt.EventId != eventId)
            throw new InvalidOperationException("Ticket type does not belong to the specified event.");

        // Max 5 tickets per user per event regardless of ticket type.
        var existingCount = await ticketRepository.Query()
            .Where(t => t.HolderId == userId &&
                        t.TicketType.EventId == eventId &&
                        t.Status != TicketStatus.Cancelled)
            .CountAsync(ct);
        if (existingCount + quantity > 5)
            throw new InvalidOperationException("You can have at most 5 tickets per event.");

        var available = tt.Quantity - tt.QuantitySold;
        if (available < quantity)
            throw new InvalidOperationException($"Only {available} spots remaining.");

        var orderNumber = $"KE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";

        var order = await orderRepository.AddAsync(new Order
        {
            OrderNumber    = orderNumber,
            UserId         = userId,
            EventId        = eventId,
            Status         = OrderStatus.Confirmed,
            TotalAmount    = 0,
            DiscountAmount = 0,
            Currency       = "EUR",
            CreatedAt      = DateTime.UtcNow
        }, ct);

        var orderItem = await orderItemRepository.AddAsync(new OrderItem
        {
            OrderId      = order.Id,
            TicketTypeId = ticketTypeId,
            Quantity     = quantity,
            UnitPrice    = 0,
            Subtotal     = 0
        }, ct);

        var tickets = new List<Ticket>(quantity);
        for (int i = 0; i < quantity; i++)
        {
            var qrCodeValue = Guid.NewGuid().ToString("N");
            var ticket = await ticketRepository.AddAsync(new Ticket
            {
                TicketNumber = $"TK-{GenerateRandomString(8)}",
                OrderItemId  = orderItem.Id,
                TicketTypeId = ticketTypeId,
                HolderId     = userId,
                QrCode       = qrCodeValue,
                Status       = TicketStatus.Valid,
                IssuedAt     = DateTime.UtcNow
            }, ct);

            try
            {
                var pngBytes = await qrCodeService.GenerateQrCodePngAsync(qrCodeValue);
                using var ms = new MemoryStream(pngBytes);
                ticket.QrCodeImageUrl = await fileStorageService.UploadAsync(
                    ms, $"qr-{qrCodeValue}.png", "image/png", ct);
            }
            catch
            {
                // QR image upload is non-critical; the plain QrCode string is the check-in key.
            }

            tickets.Add(ticket);
        }

        tt.QuantitySold += quantity;
        ticketTypeRepository.Update(tt);

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "User {UserId} registered {Qty} spot(s) for Event {EventId} (Order {OrderNumber})",
            userId, quantity, eventId, orderNumber);

        // Link to the first ticket so the profile page can auto-expand its QR.
        var firstTicketId = tickets[0].Id;
        await notificationService.SendNotificationAsync(
            userId,
            NotificationType.TicketPurchased,
            "Registered for event",
            $"You successfully registered for {quantity} spot(s). Reference: {orderNumber}.",
            linkUrl: $"/Profile/Index?tab=tickets&highlightTicket={firstTicketId}",
            ct: ct);

        return tickets.AsReadOnly();
    }

    public async Task<Ticket?> GetTicketByQrCodeAsync(string qrCode, CancellationToken ct = default)
    {
        return await ticketRepository.GetByQrCodeAsync(qrCode, ct);
    }

    public async Task<Ticket> CheckInTicketAsync(
        string qrCode, Guid checkedInById, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByQrCodeAsync(qrCode, ct)
            ?? throw new NotFoundException("Ticket", qrCode);

        if (ticket.Status != TicketStatus.Valid)
            throw new InvalidOperationException(
                $"Ticket is not valid for check-in. Current status: {ticket.Status}.");

        ticket.Status       = TicketStatus.CheckedIn;
        ticket.CheckedInAt  = DateTime.UtcNow;
        ticket.CheckedInById = checkedInById;

        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Ticket {TicketNumber} checked in by {UserId}", ticket.TicketNumber, checkedInById);
        return ticket;
    }

    public async Task<IReadOnlyList<Ticket>> GetUserTicketsAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await ticketRepository.GetByHolderAsync(userId, ct);
    }

    public async Task<Ticket> TransferTicketAsync(
        Guid ticketId, Guid newHolderId, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException(nameof(Ticket), ticketId);

        if (ticket.Status != TicketStatus.Valid)
            throw new InvalidOperationException(
                $"Only valid tickets can be transferred. Current status: {ticket.Status}.");

        var previousHolder = ticket.HolderId;
        ticket.HolderId = newHolderId;
        ticket.Status   = TicketStatus.Transferred;

        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Ticket {TicketNumber} transferred from {From} to {To}",
            ticket.TicketNumber, previousHolder, newHolderId);
        return ticket;
    }

    public async Task<Ticket> TransferTicketToEmailAsync(
        Guid ticketId, string recipientEmail, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException(nameof(Ticket), ticketId);

        if (ticket.Status != TicketStatus.Valid)
            throw new InvalidOperationException(
                $"Only valid tickets can be transferred. Current status: {ticket.Status}.");

        var previousHolder = ticket.HolderId;
        ticket.HolderId    = null;
        ticket.HolderEmail = recipientEmail;
        ticket.Status      = TicketStatus.Transferred;

        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Ticket {TicketNumber} transferred from {From} to email {Email}",
            ticket.TicketNumber, previousHolder, recipientEmail);

        try
        {
            await _emailService.SendEmailAsync(
                recipientEmail,
                "You received a ticket!",
                $"<p>You have received ticket <strong>{ticket.TicketNumber}</strong>. " +
                $"Create or log in to your account to claim it.</p>",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send transfer email to {Email}", recipientEmail);
        }

        return ticket;
    }

    public async Task CancelTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException(nameof(Ticket), ticketId);

        if (ticket.Status is TicketStatus.CheckedIn or TicketStatus.Cancelled)
            throw new InvalidOperationException(
                $"Cannot cancel a ticket with status {ticket.Status}.");

        var tt = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, ct);
        if (tt != null)
        {
            tt.QuantitySold = Math.Max(0, tt.QuantitySold - 1);
            ticketTypeRepository.Update(tt);
        }

        ticket.Status = TicketStatus.Cancelled;
        ticketRepository.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Ticket {TicketNumber} cancelled", ticket.TicketNumber);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }
}
