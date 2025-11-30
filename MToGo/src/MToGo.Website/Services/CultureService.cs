namespace MToGo.Website.Services;

public class CultureService
{
    private int? _customerId;
    private string? _jwtToken;
    private string _currentCulture = "en";

    public event Action? OnChange;

    public bool IsLoggedIn => _customerId.HasValue;
    public int? CustomerId => _customerId;
    public string? JwtToken => _jwtToken;
    public string CurrentCulture => _currentCulture;

    public void SetCustomerId(int customerId)
    {
        _customerId = customerId;
        NotifyStateChanged();
    }

    public void SetJwtToken(string token)
    {
        _jwtToken = token;
        NotifyStateChanged();
    }

    public void ClearCustomerId()
    {
        _customerId = null;
        _jwtToken = null;
        NotifyStateChanged();
    }

    public void Logout()
    {
        ClearCustomerId();
    }

    public void SetCulture(string culture)
    {
        _currentCulture = culture;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
