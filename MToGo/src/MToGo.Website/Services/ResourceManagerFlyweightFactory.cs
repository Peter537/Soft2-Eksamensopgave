using System.Collections.Concurrent;
using System.Resources;
using System.Threading;

namespace MToGo.Website.Services;

public interface IResourceManagerFlyweightFactory
{
    ResourceManager Get(string category, string pageName);
}

public sealed class ResourceManagerFlyweightFactory : IResourceManagerFlyweightFactory
{
    private readonly ConcurrentDictionary<string, Lazy<ResourceManager>> _resourceManagers = new();

    public ResourceManager Get(string category, string pageName)
    {
        var cacheKey = $"{category}.{pageName}";

        return _resourceManagers
            .GetOrAdd(cacheKey, _ => new Lazy<ResourceManager>(
                () =>
                {
                    var baseName = $"MToGo.Website.Resources.{category}.{pageName}";
                    return new ResourceManager(baseName, typeof(LocalizerService).Assembly);
                },
                LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }
}
