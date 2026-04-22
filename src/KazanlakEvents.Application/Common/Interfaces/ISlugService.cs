namespace KazanlakEvents.Application.Common.Interfaces;

public interface ISlugService
{
    string GenerateSlug(string text);
    Task<string> GenerateUniqueSlugAsync<T>(string text, Func<string, Task<bool>> existsCheck, CancellationToken ct = default);
}
