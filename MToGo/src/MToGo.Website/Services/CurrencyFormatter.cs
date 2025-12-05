using System.Globalization;

namespace MToGo.Website.Services;

/// <summary>
/// Service for formatting currency values with Danish Krone (kr) symbol.
/// </summary>
public static class CurrencyFormatter
{
    private static readonly CultureInfo DanishCulture = new CultureInfo("da-DK");

    /// <summary>
    /// Formats a decimal value as Danish Krone currency.
    /// </summary>
    /// <param name="value">The decimal value to format.</param>
    /// <returns>A string formatted as "X,XX kr" (Danish currency format).</returns>
    public static string FormatDKK(this decimal value)
    {
        return value.ToString("N2", DanishCulture) + " kr";
    }

    /// <summary>
    /// Formats a decimal value as Danish Krone currency with no decimal places.
    /// </summary>
    /// <param name="value">The decimal value to format.</param>
    /// <returns>A string formatted as "X kr" (Danish currency format without decimals).</returns>
    public static string FormatDKKNoDecimals(this decimal value)
    {
        return value.ToString("N0", DanishCulture) + " kr";
    }
}
