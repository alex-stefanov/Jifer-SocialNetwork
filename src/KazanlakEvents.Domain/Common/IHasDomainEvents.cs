namespace KazanlakEvents.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<BaseDomainEvent> DomainEvents { get; }
    void AddDomainEvent(BaseDomainEvent domainEvent);
    void RemoveDomainEvent(BaseDomainEvent domainEvent);
    void ClearDomainEvents();
}
