using Ganss.Xss;
using KazanlakEvents.Application.Common.Interfaces;

namespace KazanlakEvents.Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

    private static HtmlSanitizer BuildSanitizer()
    {
        var s = new HtmlSanitizer();

        s.AllowedTags.Clear();
        foreach (var tag in new[]
            { "p", "br", "strong", "em", "ul", "ol", "li", "h2", "h3", "h4", "a", "img" })
            s.AllowedTags.Add(tag);

        // Belt-and-suspenders: HtmlSanitizer drops anything not in AllowedTags, but
        // explicit removal documents intent for dangerous tags.
        foreach (var blocked in new[]
            { "script", "style", "iframe", "form", "input", "button", "object", "embed" })
            s.AllowedTags.Remove(blocked);

        s.AllowedAttributes.Clear();
        s.AllowedAttributes.Add("href");
        s.AllowedAttributes.Add("src");
        s.AllowedAttributes.Add("alt");

        s.AllowedSchemes.Clear();
        s.AllowedSchemes.Add("https");
        s.AllowedSchemes.Add("http");  // kept for img src over plain HTTP
        s.AllowedSchemes.Add("mailto");

        s.AllowedCssProperties.Clear();

        return s;
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}
