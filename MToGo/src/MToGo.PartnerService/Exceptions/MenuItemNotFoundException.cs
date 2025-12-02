namespace MToGo.PartnerService.Exceptions;

public class MenuItemNotFoundException : Exception
{
    public MenuItemNotFoundException(string message) : base(message)
    {
    }
}
