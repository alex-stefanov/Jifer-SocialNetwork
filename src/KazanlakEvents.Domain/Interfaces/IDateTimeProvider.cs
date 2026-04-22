namespace KazanlakEvents.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
