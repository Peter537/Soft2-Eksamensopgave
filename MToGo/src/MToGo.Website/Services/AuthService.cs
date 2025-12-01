using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;

namespace MToGo.Website.Services;

public class AuthService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private string? _cachedToken;
    private JwtSecurityToken? _cachedDecodedToken;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public event Action? OnAuthStateChanged;

    /// <summary>
    /// Alias for OnAuthStateChanged to provide consistent API with CultureService.
    /// </summary>
    public event Action? OnChange
    {
        add => OnAuthStateChanged += value;
        remove => OnAuthStateChanged -= value;
    }

    public AuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize the service by loading the token from localStorage.
    /// Safe to call multiple times - will only initialize once.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;
            
            _cachedToken = await _jsRuntime.InvokeAsync<string?>("authStorage.getToken");
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                DecodeToken(_cachedToken);
            }
            _isInitialized = true;
            
            // Notify subscribers that auth state has been loaded
            NotifyAuthStateChanged();
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet (prerendering)
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Whether the service has been initialized with data from localStorage.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    public bool IsLoggedIn => !string.IsNullOrEmpty(_cachedToken) && !IsTokenExpired;

    public string? Token => _cachedToken;

    public int? Id => GetClaimAsInt("id");

    public string? Email => GetClaim("email");

    public string? Role => GetClaim("role");

    public string? Name => GetClaim("name");

    public bool IsTokenExpired
    {
        get
        {
            if (_cachedDecodedToken == null) return true;
            return _cachedDecodedToken.ValidTo < DateTime.UtcNow;
        }
    }

    public bool IsInRole(string role) => 
        string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    public bool IsCustomer => IsInRole("Customer");

    public bool IsAgent => IsInRole("Agent");

    public bool IsPartner => IsInRole("Partner");

    public bool IsManagement => IsInRole("Management");

    public async Task LoginAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Token cannot be null or empty", nameof(token));

        _cachedToken = token;
        DecodeToken(token);

        try
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.setToken", token);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available
        }

        NotifyAuthStateChanged();
    }

    public async Task LogoutAsync()
    {
        _cachedToken = null;
        _cachedDecodedToken = null;

        try
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.removeToken");
        }
        catch (InvalidOperationException)
        {
            // JS interop not available
        }

        NotifyAuthStateChanged();
    }

    public string? GetClaim(string claimType)
    {
        return _cachedDecodedToken?.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    public int? GetClaimAsInt(string claimType)
    {
        var value = GetClaim(claimType);
        return int.TryParse(value, out var result) ? result : null;
    }

    private void DecodeToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            _cachedDecodedToken = handler.ReadJwtToken(token);
        }
        catch
        {
            _cachedDecodedToken = null;
        }
    }

    private void NotifyAuthStateChanged() => OnAuthStateChanged?.Invoke();

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
