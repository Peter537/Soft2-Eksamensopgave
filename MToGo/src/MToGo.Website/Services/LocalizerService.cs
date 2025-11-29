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
    private readonly Dictionary<string, ResourceManager> _resourceManagers = new();

    public LocalizerService(CultureService cultureService)
    {
        _cultureService = cultureService;
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        var parts = key.Split('.');
        if (parts.Length < 3)
            return key;

        // Key format: "Pages.Home.Greeting" or "Layout.NavMenu.Home"
        var category = parts[0]; // "Pages" or "Layout"
        var pageName = parts[1]; // "Home", "Login", "NavMenu", etc.
        var resourceKey = string.Join(".", parts.Skip(2)); // "Greeting", "Title", etc.

        try
        {
            var resourceManager = GetOrCreateResourceManager(category, pageName);
            var culture = new CultureInfo(_cultureService.CurrentCulture);
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

    private ResourceManager GetOrCreateResourceManager(string category, string pageName)
    {
        var cacheKey = $"{category}.{pageName}";
        
        if (_resourceManagers.TryGetValue(cacheKey, out var cached))
            return cached;

        // Resource base name format: MToGo.Website.Resources.Pages.Home
        var baseName = $"MToGo.Website.Resources.{category}.{pageName}";
        var resourceManager = new ResourceManager(baseName, typeof(LocalizerService).Assembly);
        _resourceManagers[cacheKey] = resourceManager;
        return resourceManager;
    }
}
