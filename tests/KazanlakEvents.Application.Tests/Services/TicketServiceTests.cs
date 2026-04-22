using KazanlakEvents.Application.Common.Exceptions;
using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class TicketServiceTests
{
    private Mock<ITicketRepository>      _ticketRepo = null!;
    private Mock<IRepository<TicketType>> _ttRepo = null!;
    private Mock<IRepository<Order>>     _orderRepo = null!;
    private Mock<IRepository<OrderItem>> _orderItemRepo = null!;
    private Mock<IUnitOfWork>            _uow = null!;
    private Mock<ICurrentUserService>    _currentUser = null!;
    private Mock<INotificationService>   _notifications = null!;
    private Mock<IQrCodeService>         _qrCode = null!;
    private Mock<IFileStorageService>    _fileStorage = null!;
    private Mock<IEmailService>          _email = null!;
    private Mock<ILogger<TicketService>> _logger = null!;
    private TicketService                _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ticketRepo    = new Mock<ITicketRepository>();
        _ttRepo        = new Mock<IRepository<TicketType>>();
        _orderRepo     = new Mock<IRepository<Order>>();
        _orderItemRepo = new Mock<IRepository<OrderItem>>();
        _uow           = new Mock<IUnitOfWork>();
        _currentUser   = new Mock<ICurrentUserService>();
        _notifications = new Mock<INotificationService>();
        _qrCode        = new Mock<IQrCodeService>();
        _fileStorage   = new Mock<IFileStorageService>();
        _email         = new Mock<IEmailService>();
        _logger        = new Mock<ILogger<TicketService>>();

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifications.Setup(n => n.SendNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _qrCode.Setup(q => q.GenerateQrCodePngAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        _fileStorage.Setup(f => f.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.test/qr.png");
        _email.Setup(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new TicketService(
            _ticketRepo.Object, _ttRepo.Object, _orderRepo.Object, _orderItemRepo.Object,
            _uow.Object, _currentUser.Object, _notifications.Object,
            _qrCode.Object, _fileStorage.Object, _email.Object, _logger.Object);
    }

    private static TicketType MakeTicketType(Guid eventId, int quantity = 100, int sold = 0) => new()
    {
        Id           = Guid.NewGuid(),
        EventId      = eventId,
        Name         = "General",
        Price        = 10.00m,
        Currency     = "EUR",
        Quantity     = quantity,
        QuantitySold = sold,
        MaxPerOrder  = 10,
        SortOrder    = 1
    };

    // ── GetTicketTypesForEventAsync ──────────────────────────────────────────

    [Test]
    public async Task GetTicketTypesForEventAsync_ReturnsTicketTypesForEvent()
    {
        var eventId = Guid.NewGuid();
        var types   = new List<TicketType> { MakeTicketType(eventId), MakeTicketType(eventId) };

        _ttRepo.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<TicketType, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(types.AsReadOnly());

        var result = await _sut.GetTicketTypesForEventAsync(eventId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── CreateTicketTypeAsync ────────────────────────────────────────────────

    [Test]
    public async Task CreateTicketTypeAsync_AddsAndSavesTicketType()
    {
        var tt = MakeTicketType(Guid.NewGuid());
        _ttRepo.Setup(r => r.AddAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tt);

        var result = await _sut.CreateTicketTypeAsync(tt);

        Assert.That(result, Is.EqualTo(tt));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateTicketTypeAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateTicketTypeAsync_ExistingTicketType_UpdatesFields()
    {
        var existing = MakeTicketType(Guid.NewGuid());
        _ttRepo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var updated = new TicketType
        {
            Id            = existing.Id,
            EventId       = existing.EventId,
            Name          = "VIP",
            Description   = "VIP access",
            Quantity      = 50,
            MaxPerOrder   = 2,
            SortOrder     = 0,
            SalesStartDate = DateTime.UtcNow,
            SalesEndDate   = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.UpdateTicketTypeAsync(updated);

        Assert.That(result.Name,        Is.EqualTo("VIP"));
        Assert.That(result.Quantity,    Is.EqualTo(50));
        Assert.That(result.MaxPerOrder, Is.EqualTo(2));
        _ttRepo.Verify(r => r.Update(existing), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateTicketTypeAsync_NotFound_ThrowsNotFoundException()
    {
        _ttRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateTicketTypeAsync(new TicketType { Id = Guid.NewGuid() }));
    }

    // ── DeleteTicketTypeAsync ────────────────────────────────────────────────

    [Test]
    public async Task DeleteTicketTypeAsync_ExistingTicketType_RemovesAndSaves()
    {
        var tt = MakeTicketType(Guid.NewGuid());
        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);

        await _sut.DeleteTicketTypeAsync(tt.Id);

        _ttRepo.Verify(r => r.Remove(tt), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteTicketTypeAsync_NotFound_ThrowsNotFoundException()
    {
        _ttRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteTicketTypeAsync(Guid.NewGuid()));
    }

    // ── RegisterForEventAsync ────────────────────────────────────────────────

    [Test]
    public async Task RegisterForEventAsync_HappyPath_CreatesOrderAndTickets()
    {
        var eventId  = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var tt       = MakeTicketType(eventId, quantity: 100, sold: 0);
        var order    = new Order { Id = Guid.NewGuid(), UserId = userId, EventId = eventId, OrderNumber = "KE-TEST", Currency = "EUR" };
        var orderItem = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, TicketTypeId = tt.Id, Quantity = 2, UnitPrice = 0, Subtotal = 0 };

        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);
        _ticketRepo.Setup(r => r.Query()).Returns(new List<Ticket>().AsQueryable().BuildMock());
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderItemRepo.Setup(r => r.AddAsync(It.IsAny<OrderItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);
        _ticketRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket t, CancellationToken _) => t);

        var result = await _sut.RegisterForEventAsync(eventId, userId, tt.Id, quantity: 2);

        Assert.That(result,          Has.Count.EqualTo(2));
        Assert.That(tt.QuantitySold, Is.EqualTo(2));
        Assert.That(result.All(t => t.Status == TicketStatus.Valid), Is.True);
        Assert.That(result.All(t => t.HolderId == userId), Is.True);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.SendNotificationAsync(
            userId, NotificationType.TicketPurchased,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegisterForEventAsync_TicketTypeNotFound_ThrowsNotFoundException()
    {
        _ttRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType?)null);

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.RegisterForEventAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Test]
    public async Task RegisterForEventAsync_WrongEvent_ThrowsInvalidOperationException()
    {
        var tt = MakeTicketType(Guid.NewGuid()); // belongs to a different event
        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterForEventAsync(Guid.NewGuid(), Guid.NewGuid(), tt.Id));
    }

    [Test]
    public async Task RegisterForEventAsync_ExceedsFiveTickets_ThrowsInvalidOperationException()
    {
        var eventId  = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var tt       = MakeTicketType(eventId, quantity: 100, sold: 0);
        var ttId     = tt.Id;

        // User already has 4 valid tickets; requesting 2 more would exceed limit
        var existingTickets = Enumerable.Range(0, 4).Select(_ => new Ticket
        {
            Id           = Guid.NewGuid(),
            TicketNumber = "TK-EXIST",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = ttId,
            TicketType   = tt,
            HolderId     = userId,
            QrCode       = Guid.NewGuid().ToString(),
            Status       = TicketStatus.Valid
        }).ToList();

        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);
        _ticketRepo.Setup(r => r.Query()).Returns(existingTickets.AsQueryable().BuildMock());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterForEventAsync(eventId, userId, tt.Id, quantity: 2));

        Assert.That(ex!.Message, Does.Contain("5 tickets"));
    }

    [Test]
    public async Task RegisterForEventAsync_NotEnoughSpots_ThrowsInvalidOperationException()
    {
        var eventId = Guid.NewGuid();
        var userId  = Guid.NewGuid();
        var tt      = MakeTicketType(eventId, quantity: 5, sold: 4); // only 1 left

        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);
        _ticketRepo.Setup(r => r.Query()).Returns(new List<Ticket>().AsQueryable().BuildMock());

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterForEventAsync(eventId, userId, tt.Id, quantity: 3));

        Assert.That(ex!.Message, Does.Contain("spots remaining"));
    }

    [Test]
    public async Task RegisterForEventAsync_QrCodeUploadFails_StillCreatesTicket()
    {
        var eventId  = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var tt       = MakeTicketType(eventId);
        var order    = new Order { Id = Guid.NewGuid(), UserId = userId, EventId = eventId, OrderNumber = "KE-TEST", Currency = "EUR" };
        var orderItem = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, TicketTypeId = tt.Id, Quantity = 1, UnitPrice = 0, Subtotal = 0 };

        _ttRepo.Setup(r => r.GetByIdAsync(tt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tt);
        _ticketRepo.Setup(r => r.Query()).Returns(new List<Ticket>().AsQueryable().BuildMock());
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderItemRepo.Setup(r => r.AddAsync(It.IsAny<OrderItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);
        _ticketRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket t, CancellationToken _) => t);
        _qrCode.Setup(q => q.GenerateQrCodePngAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("QR fail"));

        // Should not throw - QR failure is non-critical
        var result = await _sut.RegisterForEventAsync(eventId, userId, tt.Id, quantity: 1);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    // ── GetTicketByQrCodeAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetTicketByQrCodeAsync_DelegatesToRepository()
    {
        var qr = "abc123";
        var ticket = new Ticket { Id = Guid.NewGuid(), TicketNumber = "TK-1", QrCode = qr, OrderItemId = Guid.NewGuid(), TicketTypeId = Guid.NewGuid() };
        _ticketRepo.Setup(r => r.GetByQrCodeAsync(qr, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _sut.GetTicketByQrCodeAsync(qr);

        Assert.That(result, Is.EqualTo(ticket));
    }

    // ── CheckInTicketAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CheckInTicketAsync_ValidTicket_ChecksInSuccessfully()
    {
        var checkedInById = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = Guid.NewGuid(),
            TicketNumber = "TK-001",
            QrCode       = "qr123",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.Valid
        };
        _ticketRepo.Setup(r => r.GetByQrCodeAsync("qr123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await _sut.CheckInTicketAsync("qr123", checkedInById);

        Assert.That(result.Status,        Is.EqualTo(TicketStatus.CheckedIn));
        Assert.That(result.CheckedInAt,   Is.Not.Null);
        Assert.That(result.CheckedInById, Is.EqualTo(checkedInById));
        _ticketRepo.Verify(r => r.Update(ticket), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CheckInTicketAsync_AlreadyCheckedIn_ThrowsInvalidOperationException()
    {
        var ticket = new Ticket
        {
            Id           = Guid.NewGuid(),
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.CheckedIn
        };
        _ticketRepo.Setup(r => r.GetByQrCodeAsync("qr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckInTicketAsync("qr", Guid.NewGuid()));
    }

    [Test]
    public async Task CheckInTicketAsync_TicketNotFound_ThrowsNotFoundException()
    {
        _ticketRepo.Setup(r => r.GetByQrCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.CheckInTicketAsync("bad-qr", Guid.NewGuid()));
    }

    // ── GetUserTicketsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetUserTicketsAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), TicketNumber = "TK-1", QrCode = "q1", OrderItemId = Guid.NewGuid(), TicketTypeId = Guid.NewGuid() }
        };
        _ticketRepo.Setup(r => r.GetByHolderAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets.AsReadOnly());

        var result = await _sut.GetUserTicketsAsync(userId);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    // ── TransferTicketAsync ──────────────────────────────────────────────────

    [Test]
    public async Task TransferTicketAsync_ValidTicket_TransfersToNewHolder()
    {
        var ticketId    = Guid.NewGuid();
        var newHolderId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            HolderId     = Guid.NewGuid(),
            Status       = TicketStatus.Valid
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _sut.TransferTicketAsync(ticketId, newHolderId);

        Assert.That(result.HolderId, Is.EqualTo(newHolderId));
        Assert.That(result.Status,   Is.EqualTo(TicketStatus.Transferred));
        _ticketRepo.Verify(r => r.Update(ticket), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TransferTicketAsync_CancelledTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.Cancelled
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.TransferTicketAsync(ticketId, Guid.NewGuid()));
    }

    [Test]
    public async Task TransferTicketAsync_TicketNotFound_ThrowsNotFoundException()
    {
        _ticketRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.TransferTicketAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── TransferTicketToEmailAsync ───────────────────────────────────────────

    [Test]
    public async Task TransferTicketToEmailAsync_ValidTicket_TransfersAndSendsEmail()
    {
        var ticketId = Guid.NewGuid();
        var email    = "recipient@example.com";
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            HolderId     = Guid.NewGuid(),
            Status       = TicketStatus.Valid
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _sut.TransferTicketToEmailAsync(ticketId, email);

        Assert.That(result.HolderEmail, Is.EqualTo(email));
        Assert.That(result.HolderId,    Is.Null);
        Assert.That(result.Status,      Is.EqualTo(TicketStatus.Transferred));
        _email.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TransferTicketToEmailAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.CheckedIn
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransferTicketToEmailAsync(ticketId, "someone@example.com"));
    }

    [Test]
    public async Task TransferTicketToEmailAsync_EmailFails_StillCompletesTransfer()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            HolderId     = Guid.NewGuid(),
            Status       = TicketStatus.Valid
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _email.Setup(e => e.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("email failure"));

        // Should not throw - email failure is non-critical
        var result = await _sut.TransferTicketToEmailAsync(ticketId, "someone@example.com");

        Assert.That(result.Status, Is.EqualTo(TicketStatus.Transferred));
    }

    // ── CancelTicketAsync ────────────────────────────────────────────────────

    [Test]
    public async Task CancelTicketAsync_ValidTicket_CancelsAndDecrementsQuantitySold()
    {
        var ttId   = Guid.NewGuid();
        var tt     = new TicketType { Id = ttId, EventId = Guid.NewGuid(), Name = "G", Quantity = 10, QuantitySold = 3, MaxPerOrder = 10, SortOrder = 1 };
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = ttId,
            Status       = TicketStatus.Valid
        };

        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _ttRepo.Setup(r => r.GetByIdAsync(ttId, It.IsAny<CancellationToken>())).ReturnsAsync(tt);

        await _sut.CancelTicketAsync(ticketId);

        Assert.That(ticket.Status,   Is.EqualTo(TicketStatus.Cancelled));
        Assert.That(tt.QuantitySold, Is.EqualTo(2));
        _ttRepo.Verify(r => r.Update(tt), Times.Once);
        _ticketRepo.Verify(r => r.Update(ticket), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelTicketAsync_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.Cancelled
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CancelTicketAsync(ticketId));
    }

    [Test]
    public async Task CancelTicketAsync_CheckedInTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.CheckedIn
        };
        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CancelTicketAsync(ticketId));
    }

    [Test]
    public async Task CancelTicketAsync_TicketNotFound_ThrowsNotFoundException()
    {
        _ticketRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelTicketAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task CancelTicketAsync_TicketTypeNotFound_StillCancelsTicket()
    {
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket
        {
            Id           = ticketId,
            TicketNumber = "TK-001",
            QrCode       = "qr",
            OrderItemId  = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Status       = TicketStatus.Valid
        };

        _ticketRepo.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _ttRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType?)null);

        await _sut.CancelTicketAsync(ticketId);

        Assert.That(ticket.Status, Is.EqualTo(TicketStatus.Cancelled));
    }

    // ── GetOrderWithDetailsAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetOrderWithDetailsAsync_ExistingOrder_ReturnsOrderWithItems()
    {
        var orderId = Guid.NewGuid();
        var order   = new Order { Id = orderId, UserId = Guid.NewGuid(), EventId = Guid.NewGuid(), OrderNumber = "KE-TEST", Currency = "EUR" };
        _orderRepo.Setup(r => r.Query())
            .Returns(new List<Order> { order }.AsQueryable().BuildMock());

        var result = await _sut.GetOrderWithDetailsAsync(orderId);

        Assert.That(result,     Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(orderId));
    }

    [Test]
    public async Task GetOrderWithDetailsAsync_NonExistentOrder_ReturnsNull()
    {
        _orderRepo.Setup(r => r.Query())
            .Returns(new List<Order>().AsQueryable().BuildMock());

        var result = await _sut.GetOrderWithDetailsAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }
}
