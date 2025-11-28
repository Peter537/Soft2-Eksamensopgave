namespace MToGo.Website.Services;

/// <summary>
/// Simple service to track user login state (temporary until full auth is implemented).
/// </summary>
public class CultureService
{
    private int? _customerId;

    public bool IsLoggedIn => _customerId.HasValue;
    public int? CustomerId => _customerId;

    public void SetCustomerId(int customerId)
    {
        _customerId = customerId;
    }

    public void ClearCustomerId()
    {
        _customerId = null;
    }

    public void Logout()
    {
        ClearCustomerId();
    }
}
