using System.Globalization;
using System.Resources;

namespace MToGo.Website.Services;

public interface ILocalizerService
{
    string this[string key] { get; }
    string GetString(string key);
    string GetString(string key, params object[] args);
}

public class LocalizerService : ILocalizerService
{
    private readonly CultureService _cultureService;
    private readonly IResourceManagerFlyweightFactory _resourceManagerFactory;

    public LocalizerService(CultureService cultureService, IResourceManagerFlyweightFactory resourceManagerFactory)
    {
        _cultureService = cultureService;
        _resourceManagerFactory = resourceManagerFactory;
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        var parts = key.Split('.');
        if (parts.Length < 3)
            return key;

        // Key format: "Pages.Home.Greeting" or "Layout.NavMenu.Home" or "Pages.Partner.Orders.Title"
        var category = parts[0]; // "Pages" or "Layout"
        
        // Find the resource key - it's the last part after the resource path
        // For nested paths like "Pages.Partner.Orders.Title", we need to find where the path ends
        // The resource key is typically a single word like "Title", "PageTitle", etc.
        // So we take the last part as the resource key and everything in between as the path
        var resourceKey = parts[^1]; // Last part is the resource key
        var pathParts = parts.Skip(1).Take(parts.Length - 2).ToArray(); // Middle parts form the path
        var resourcePath = string.Join(".", pathParts);

        try
        {
            var resourceManager = _resourceManagerFactory.Get(category, resourcePath);
            var culture = CultureInfo.GetCultureInfo(_cultureService.CurrentCulture);
            var value = resourceManager.GetString(resourceKey, culture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }
}
