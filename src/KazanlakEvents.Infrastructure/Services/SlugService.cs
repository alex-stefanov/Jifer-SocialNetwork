using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KazanlakEvents.Application.Common.Interfaces;

namespace KazanlakEvents.Infrastructure.Services;

public partial class SlugService : ISlugService
{
    public string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = RemoveDiacritics(slug);
        slug = InvalidCharsRegex().Replace(slug, "");
        slug = MultipleHyphensRegex().Replace(slug, "-");
        slug = slug.Trim('-');
        return slug.Length > 200 ? slug[..200].TrimEnd('-') : slug;
    }

    public async Task<string> GenerateUniqueSlugAsync<T>(
        string text, Func<string, Task<bool>> existsCheck, CancellationToken ct = default)
    {
        var baseSlug = GenerateSlug(text);
        var slug = baseSlug;
        var counter = 1;

        while (await existsCheck(slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace(' ', '-');
    }

    [GeneratedRegex(@"[^a-z0-9\-\u0400-\u04FF]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();
}
