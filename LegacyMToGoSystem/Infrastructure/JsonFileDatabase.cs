using System.Text.Json;

namespace LegacyMToGoSystem.Infrastructure;

public class JsonFileDatabase : IDatabase
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonFileDatabase(IConfiguration configuration)
    {
        _dataDirectory = configuration["Database:DataDirectory"] ?? "Data";
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task InitializeAsync()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        return Task.CompletedTask;
    }

    public async Task<T?> LoadAsync<T>(string collectionName) where T : class
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(collectionName);
            
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync<T>(string collectionName, T data) where T : class
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(collectionName);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetFilePath(string collectionName)
    {
        return Path.Combine(_dataDirectory, $"{collectionName}.json");
    }
}
