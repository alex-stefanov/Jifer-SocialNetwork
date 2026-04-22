using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class AdminServiceTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private AdminService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db  = new Mock<IApplicationDbContext>();
        _uow = new Mock<IUnitOfWork>();
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new AdminService(_db.Object, _uow.Object);
    }

    // ── GetTotalUsersCountAsync ──────────────────────────────────────────────

    [Test]
    public async Task GetTotalUsersCountAsync_ReturnsCountOfAllUserProfiles()
    {
        var profiles = new List<UserProfile>
        {
            new() { UserId = Guid.NewGuid(), FirstName = "A", LastName = "B" },
            new() { UserId = Guid.NewGuid(), FirstName = "C", LastName = "D" },
            new() { UserId = Guid.NewGuid(), FirstName = "E", LastName = "F" }
        };
        _db.Setup(d => d.UserProfiles).Returns(profiles.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetTotalUsersCountAsync();

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task GetTotalUsersCountAsync_EmptyTable_ReturnsZero()
    {
        _db.Setup(d => d.UserProfiles).Returns(new List<UserProfile>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetTotalUsersCountAsync();

        Assert.That(result, Is.EqualTo(0));
    }

    // ── GetTotalEventsCountAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetTotalEventsCountAsync_CountsOnlyPublishedEvents()
    {
        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "E1", Slug = "e1", Description = "d", CategoryId = 1, Status = EventStatus.Published, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "E2", Slug = "e2", Description = "d", CategoryId = 1, Status = EventStatus.Published, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "E3", Slug = "e3", Description = "d", CategoryId = 1, Status = EventStatus.Draft,      StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) }
        };
        _db.Setup(d => d.Events).Returns(events.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetTotalEventsCountAsync();

        Assert.That(result, Is.EqualTo(2));
    }

    // ── GetPendingApprovalsCountAsync ────────────────────────────────────────

    [Test]
    public async Task GetPendingApprovalsCountAsync_CountsOnlyPendingApproval()
    {
        var events = new List<Event>
        {
            new() { Id = Guid.NewGuid(), Title = "E1", Slug = "e1", Description = "d", CategoryId = 1, Status = EventStatus.PendingApproval, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "E2", Slug = "e2", Description = "d", CategoryId = 1, Status = EventStatus.Published,       StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1) }
        };
        _db.Setup(d => d.Events).Returns(events.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetPendingApprovalsCountAsync();

        Assert.That(result, Is.EqualTo(1));
    }

    // ── GetTotalTicketsSoldAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetTotalTicketsSoldAsync_ReturnsCountOfAllTickets()
    {
        var ticketTypeId = Guid.NewGuid();
        var tickets = new List<Ticket>
        {
            new() { Id = Guid.NewGuid(), TicketNumber = "TK1", OrderItemId = Guid.NewGuid(), TicketTypeId = ticketTypeId, QrCode = "q1" },
            new() { Id = Guid.NewGuid(), TicketNumber = "TK2", OrderItemId = Guid.NewGuid(), TicketTypeId = ticketTypeId, QrCode = "q2" }
        };
        _db.Setup(d => d.Tickets).Returns(tickets.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetTotalTicketsSoldAsync();

        Assert.That(result, Is.EqualTo(2));
    }

    // ── WarnUserAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task WarnUserAsync_AddsWarningAndSavesChanges()
    {
        var warnings = new List<UserWarning>();
        var mockSet = warnings.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<UserWarning>())).Callback<UserWarning>(w => warnings.Add(w));
        _db.Setup(d => d.UserWarnings).Returns(mockSet.Object);

        var userId     = Guid.NewGuid();
        var issuedById = Guid.NewGuid();

        await _sut.WarnUserAsync(userId, issuedById, "Bad behaviour", WarningType.Warning);

        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].UserId,     Is.EqualTo(userId));
        Assert.That(warnings[0].IssuedById, Is.EqualTo(issuedById));
        Assert.That(warnings[0].Type,       Is.EqualTo(WarningType.Warning));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WarnUserAsync_TempBan_SetsExpiresAt()
    {
        var warnings = new List<UserWarning>();
        var mockSet = warnings.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<UserWarning>())).Callback<UserWarning>(w => warnings.Add(w));
        _db.Setup(d => d.UserWarnings).Returns(mockSet.Object);

        var expiresAt = DateTime.UtcNow.AddDays(7);
        await _sut.WarnUserAsync(Guid.NewGuid(), Guid.NewGuid(), "Temp ban reason", WarningType.TempBan, expiresAt);

        Assert.That(warnings[0].ExpiresAt, Is.EqualTo(expiresAt));
        Assert.That(warnings[0].Type,      Is.EqualTo(WarningType.TempBan));
    }

    // ── GetUserWarningsAsync ─────────────────────────────────────────────────

    [Test]
    public async Task GetUserWarningsAsync_ReturnsWarningsForUser_OrderedDescByCreatedAt()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var warnings = new List<UserWarning>
        {
            new() { Id = Guid.NewGuid(), UserId = userId,  Reason = "Old",  Type = WarningType.Warning, IssuedById = otherId, CreatedAt = now.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = userId,  Reason = "New",  Type = WarningType.Warning, IssuedById = otherId, CreatedAt = now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), UserId = otherId, Reason = "Other",Type = WarningType.Warning, IssuedById = userId,  CreatedAt = now }
        };
        _db.Setup(d => d.UserWarnings).Returns(warnings.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetUserWarningsAsync(userId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Reason, Is.EqualTo("New"));
        Assert.That(result[1].Reason, Is.EqualTo("Old"));
    }

    // ── IsUserBannedAsync ────────────────────────────────────────────────────

    [Test]
    public async Task IsUserBannedAsync_PermBan_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var warnings = new List<UserWarning>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = WarningType.PermBan, Reason = "r", IssuedById = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        };
        _db.Setup(d => d.UserWarnings).Returns(warnings.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsUserBannedAsync(userId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsUserBannedAsync_ActiveTempBan_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var warnings = new List<UserWarning>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = WarningType.TempBan, ExpiresAt = DateTime.UtcNow.AddDays(1), Reason = "r", IssuedById = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        };
        _db.Setup(d => d.UserWarnings).Returns(warnings.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsUserBannedAsync(userId);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsUserBannedAsync_ExpiredTempBan_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var warnings = new List<UserWarning>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = WarningType.TempBan, ExpiresAt = DateTime.UtcNow.AddDays(-1), Reason = "r", IssuedById = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        };
        _db.Setup(d => d.UserWarnings).Returns(warnings.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsUserBannedAsync(userId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserBannedAsync_OnlyWarning_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var warnings = new List<UserWarning>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = WarningType.Warning, Reason = "r", IssuedById = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
        };
        _db.Setup(d => d.UserWarnings).Returns(warnings.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsUserBannedAsync(userId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsUserBannedAsync_NoWarnings_ReturnsFalse()
    {
        _db.Setup(d => d.UserWarnings).Returns(new List<UserWarning>().AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.IsUserBannedAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    // ── GetAuditLogsAsync ────────────────────────────────────────────────────

    private static List<AuditLog> BuildAuditLogs()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        return new List<AuditLog>
        {
            new() { Id = 1, Action = "Login",  UserId = userId,  Timestamp = now.AddDays(-3), EntityType = "User" },
            new() { Id = 2, Action = "Create", UserId = userId,  Timestamp = now.AddDays(-2), EntityType = "Event" },
            new() { Id = 3, Action = "Delete", UserId = Guid.NewGuid(), Timestamp = now.AddDays(-1), EntityType = "Event" },
            new() { Id = 4, Action = "Login",  UserId = Guid.NewGuid(), Timestamp = now,            EntityType = "User" }
        };
    }

    [Test]
    public async Task GetAuditLogsAsync_NoFilters_ReturnsPaged()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsAsync(page: 1, pageSize: 2);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAuditLogsAsync_ActionFilter_ReturnsMatchingLogs()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsAsync(page: 1, pageSize: 10, action: "Login");

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(l => l.Action.Contains("Login")), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_UserIdFilter_ReturnsMatchingLogs()
    {
        var logs = BuildAuditLogs();
        var targetUserId = logs[0].UserId!.Value;
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsAsync(page: 1, pageSize: 10, userId: targetUserId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(l => l.UserId == targetUserId), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_DateFromFilter_ReturnsLogsAfterDate()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var from = DateTime.UtcNow.AddDays(-2);
        var result = await _sut.GetAuditLogsAsync(page: 1, pageSize: 10, from: from);

        Assert.That(result.All(l => l.Timestamp >= from), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_DateToFilter_ReturnsLogsBeforeDate()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var to = DateTime.UtcNow.AddDays(-2);
        var result = await _sut.GetAuditLogsAsync(page: 1, pageSize: 10, to: to);

        Assert.That(result.All(l => l.Timestamp <= to), Is.True);
    }

    [Test]
    public async Task GetAuditLogsAsync_Page2_ReturnsCorrectItems()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsAsync(page: 2, pageSize: 2);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // ── GetAuditLogsTotalCountAsync ──────────────────────────────────────────

    [Test]
    public async Task GetAuditLogsTotalCountAsync_NoFilters_ReturnsTotalCount()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsTotalCountAsync();

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public async Task GetAuditLogsTotalCountAsync_ActionFilter_ReturnsFilteredCount()
    {
        var logs = BuildAuditLogs();
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsTotalCountAsync(action: "Create");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAuditLogsTotalCountAsync_AllFilters_ReturnsCorrectCount()
    {
        var logs = BuildAuditLogs();
        var targetUserId = logs[0].UserId!.Value;
        _db.Setup(d => d.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAuditLogsTotalCountAsync(
            action: "Login",
            userId: targetUserId,
            from: DateTime.UtcNow.AddDays(-5),
            to: DateTime.UtcNow.AddDays(1));

        Assert.That(result, Is.EqualTo(1));
    }
}
