namespace MToGo.Website.Services;

public class CultureService
{
    private int? _customerId;
    private string? _jwtToken;
    private int? _agentId;
    private string? _agentJwtToken;
    private string _currentCulture = "en";

    public event Action? OnChange;

    public bool IsLoggedIn => _customerId.HasValue;
    public int? CustomerId => _customerId;
    public string? JwtToken => _jwtToken;
    
    public bool IsAgentLoggedIn => _agentId.HasValue;
    public int? AgentId => _agentId;
    public string? AgentJwtToken => _agentJwtToken;
    
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

    public void SetAgentId(int agentId)
    {
        _agentId = agentId;
        NotifyStateChanged();
    }

    public void SetAgentJwtToken(string token)
    {
        _agentJwtToken = token;
        NotifyStateChanged();
    }

    public void ClearAgentId()
    {
        _agentId = null;
        _agentJwtToken = null;
        NotifyStateChanged();
    }

    public void AgentLogout()
    {
        ClearAgentId();
    }

    public void SetCulture(string culture)
    {
        _currentCulture = culture;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
