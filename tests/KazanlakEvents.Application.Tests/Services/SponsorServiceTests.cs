using KazanlakEvents.Application.Common.Interfaces;
using KazanlakEvents.Application.Services.Implementations;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Enums;
using KazanlakEvents.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;

namespace KazanlakEvents.Application.Tests.Services;

[TestFixture]
public class SponsorServiceTests
{
    private Mock<IApplicationDbContext> _db = null!;
    private Mock<IUnitOfWork>           _uow = null!;
    private SponsorService              _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db  = new Mock<IApplicationDbContext>();
        _uow = new Mock<IUnitOfWork>();
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new SponsorService(_db.Object, _uow.Object);
    }

    private static Sponsor MakeSponsor(bool isActive = true, SponsorTier tier = SponsorTier.Bronze) => new()
    {
        Id        = Guid.NewGuid(),
        Name      = "ACME Corp",
        Tier      = tier,
        IsActive  = isActive,
        CreatedAt = DateTime.UtcNow
    };

    // ── GetActiveSponsorsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetActiveSponsorsAsync_ReturnsOnlyActiveSponsors_OrderedByTierThenName()
    {
        var s1 = MakeSponsor(isActive: true,  tier: SponsorTier.Bronze);  s1.Name = "Z Bronze";
        var s2 = MakeSponsor(isActive: true,  tier: SponsorTier.Gold);    s2.Name = "A Gold";
        var s3 = MakeSponsor(isActive: false, tier: SponsorTier.Silver);  s3.Name = "Inactive";
        var sponsors = new List<Sponsor> { s1, s2, s3 };
        _db.Setup(d => d.Sponsors).Returns(sponsors.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetActiveSponsorsAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("A Gold"));
        Assert.That(result[1].Name, Is.EqualTo("Z Bronze"));
    }

    // ── GetEventSponsorsAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetEventSponsorsAsync_ReturnsActiveSponsorsForEvent()
    {
        var eventId  = Guid.NewGuid();
        var otherEvt = Guid.NewGuid();

        var goldSponsor   = MakeSponsor(isActive: true,  tier: SponsorTier.Gold);
        var silverSponsor = MakeSponsor(isActive: true,  tier: SponsorTier.Silver);
        var inactiveSponsor = MakeSponsor(isActive: false);

        var eventSponsors = new List<EventSponsor>
        {
            new() { EventId = eventId,  SponsorId = goldSponsor.Id,     Sponsor = goldSponsor },
            new() { EventId = eventId,  SponsorId = silverSponsor.Id,   Sponsor = silverSponsor },
            new() { EventId = eventId,  SponsorId = inactiveSponsor.Id, Sponsor = inactiveSponsor },
            new() { EventId = otherEvt, SponsorId = goldSponsor.Id,     Sponsor = goldSponsor }
        };
        _db.Setup(d => d.EventSponsors).Returns(eventSponsors.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetEventSponsorsAsync(eventId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(es => es.Sponsor.IsActive), Is.True);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsAllSponsorsOrderedByTierThenName()
    {
        var g1 = MakeSponsor(isActive: false, tier: SponsorTier.Gold);   g1.Name = "B Gold";
        var g2 = MakeSponsor(isActive: true,  tier: SponsorTier.Gold);   g2.Name = "A Gold";
        var g3 = MakeSponsor(isActive: true,  tier: SponsorTier.Bronze); g3.Name = "Bronze";
        var sponsors = new List<Sponsor> { g1, g2, g3 };
        _db.Setup(d => d.Sponsors).Returns(sponsors.AsQueryable().BuildMockDbSet().Object);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Has.Count.EqualTo(3));
        // Gold sponsors first (tier desc), then within same tier alphabetically
        Assert.That(result[0].Name, Is.EqualTo("A Gold"));
        Assert.That(result[1].Name, Is.EqualTo("B Gold"));
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ExistingSponsor_ReturnsSponsor()
    {
        var sponsor = MakeSponsor();
        var mockSet = new List<Sponsor> { sponsor }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(sponsor);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        var result = await _sut.GetByIdAsync(sponsor.Id);

        Assert.That(result,     Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(sponsor.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var mockSet = new List<Sponsor>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Sponsor?)null);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_AddsAndSavesSponsor()
    {
        var sponsors = new List<Sponsor>();
        var mockSet  = sponsors.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<Sponsor>())).Callback<Sponsor>(sponsors.Add);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        var sponsor = MakeSponsor();
        var result  = await _sut.CreateAsync(sponsor);

        Assert.That(sponsors, Has.Count.EqualTo(1));
        Assert.That(result, Is.EqualTo(sponsor));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ExistingSponsor_UpdatesFieldsAndSaves()
    {
        var existing = MakeSponsor();
        var mockSet  = new List<Sponsor> { existing }.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(existing);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        var updated = new Sponsor
        {
            Id          = existing.Id,
            Name        = "Updated Name",
            LogoUrl     = "https://logo.url",
            WebsiteUrl  = "https://website.url",
            Description = "New desc",
            Tier        = SponsorTier.Gold,
            IsActive    = false
        };

        var result = await _sut.UpdateAsync(updated);

        Assert.That(result.Name,        Is.EqualTo("Updated Name"));
        Assert.That(result.Tier,        Is.EqualTo(SponsorTier.Gold));
        Assert.That(result.IsActive,    Is.False);
        Assert.That(result.WebsiteUrl,  Is.EqualTo("https://website.url"));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_SponsorNotFound_ThrowsInvalidOperationException()
    {
        var mockSet = new List<Sponsor>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Sponsor?)null);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(new Sponsor { Id = Guid.NewGuid() }));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_ExistingSponsor_RemovesAndSaves()
    {
        var sponsor  = MakeSponsor();
        var sponsors = new List<Sponsor> { sponsor };
        var mockSet  = sponsors.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(sponsor);
        mockSet.Setup(s => s.Remove(It.IsAny<Sponsor>())).Callback<Sponsor>(s => sponsors.Remove(s));
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        await _sut.DeleteAsync(sponsor.Id);

        Assert.That(sponsors, Is.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_SponsorNotFound_ThrowsInvalidOperationException()
    {
        var mockSet = new List<Sponsor>().AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Sponsor?)null);
        _db.Setup(d => d.Sponsors).Returns(mockSet.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    // ── AssociateSponsorWithEventAsync ───────────────────────────────────────

    [Test]
    public async Task AssociateSponsorWithEventAsync_NotExisting_AddsLink()
    {
        var eventId   = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var links     = new List<EventSponsor>();
        var mockSet   = links.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Add(It.IsAny<EventSponsor>())).Callback<EventSponsor>(links.Add);
        _db.Setup(d => d.EventSponsors).Returns(mockSet.Object);

        await _sut.AssociateSponsorWithEventAsync(eventId, sponsorId);

        Assert.That(links,              Has.Count.EqualTo(1));
        Assert.That(links[0].EventId,   Is.EqualTo(eventId));
        Assert.That(links[0].SponsorId, Is.EqualTo(sponsorId));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssociateSponsorWithEventAsync_AlreadyExists_DoesNotDuplicate()
    {
        var eventId   = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var existing  = new EventSponsor { EventId = eventId, SponsorId = sponsorId };
        var links     = new List<EventSponsor> { existing };
        _db.Setup(d => d.EventSponsors).Returns(links.AsQueryable().BuildMockDbSet().Object);

        await _sut.AssociateSponsorWithEventAsync(eventId, sponsorId);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RemoveSponsorFromEventAsync ──────────────────────────────────────────

    [Test]
    public async Task RemoveSponsorFromEventAsync_ExistingLink_RemovesIt()
    {
        var eventId   = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var link      = new EventSponsor { EventId = eventId, SponsorId = sponsorId };
        var links     = new List<EventSponsor> { link };
        var mockSet   = links.AsQueryable().BuildMockDbSet();
        mockSet.Setup(s => s.Remove(It.IsAny<EventSponsor>())).Callback<EventSponsor>(l => links.Remove(l));
        _db.Setup(d => d.EventSponsors).Returns(mockSet.Object);

        await _sut.RemoveSponsorFromEventAsync(eventId, sponsorId);

        Assert.That(links, Is.Empty);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveSponsorFromEventAsync_LinkDoesNotExist_DoesNothing()
    {
        _db.Setup(d => d.EventSponsors)
           .Returns(new List<EventSponsor>().AsQueryable().BuildMockDbSet().Object);

        await _sut.RemoveSponsorFromEventAsync(Guid.NewGuid(), Guid.NewGuid());

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IncrementImpressionAsync ─────────────────────────────────────────────

    [Test]
    public async Task IncrementImpressionAsync_LinkExists_IncrementsCount()
    {
        var eventId   = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var link      = new EventSponsor { EventId = eventId, SponsorId = sponsorId, ImpressionCount = 5 };
        _db.Setup(d => d.EventSponsors)
           .Returns(new List<EventSponsor> { link }.AsQueryable().BuildMockDbSet().Object);

        await _sut.IncrementImpressionAsync(eventId, sponsorId);

        Assert.That(link.ImpressionCount, Is.EqualTo(6));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IncrementImpressionAsync_LinkDoesNotExist_DoesNothing()
    {
        _db.Setup(d => d.EventSponsors)
           .Returns(new List<EventSponsor>().AsQueryable().BuildMockDbSet().Object);

        await _sut.IncrementImpressionAsync(Guid.NewGuid(), Guid.NewGuid());

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── IncrementClickAsync ──────────────────────────────────────────────────

    [Test]
    public async Task IncrementClickAsync_LinkExists_IncrementsClickCount()
    {
        var eventId   = Guid.NewGuid();
        var sponsorId = Guid.NewGuid();
        var link      = new EventSponsor { EventId = eventId, SponsorId = sponsorId, ClickCount = 2 };
        _db.Setup(d => d.EventSponsors)
           .Returns(new List<EventSponsor> { link }.AsQueryable().BuildMockDbSet().Object);

        await _sut.IncrementClickAsync(eventId, sponsorId);

        Assert.That(link.ClickCount, Is.EqualTo(3));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task IncrementClickAsync_LinkDoesNotExist_DoesNothing()
    {
        _db.Setup(d => d.EventSponsors)
           .Returns(new List<EventSponsor>().AsQueryable().BuildMockDbSet().Object);

        await _sut.IncrementClickAsync(Guid.NewGuid(), Guid.NewGuid());

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
