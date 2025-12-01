namespace MToGo.Website.Services;

/// <summary>
/// Service for managing the current culture/language setting.
/// </summary>
public class CultureService
{
    private string _currentCulture = "en";

    public event Action? OnChange;

    public string CurrentCulture => _currentCulture;

    public void SetCulture(string culture)
    {
        if (_currentCulture != culture)
        {
            _currentCulture = culture;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
