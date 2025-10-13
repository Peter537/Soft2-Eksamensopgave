namespace LegacyMToGoSystem.Infrastructure;

public interface IDatabase
{
    Task<T?> LoadAsync<T>(string collectionName) where T : class;
    Task SaveAsync<T>(string collectionName, T data) where T : class;
    Task InitializeAsync();
}
