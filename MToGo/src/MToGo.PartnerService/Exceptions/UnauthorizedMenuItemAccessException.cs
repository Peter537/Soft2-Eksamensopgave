namespace MToGo.PartnerService.Exceptions;

public class UnauthorizedMenuItemAccessException : Exception
{
    public UnauthorizedMenuItemAccessException(string message) : base(message)
    {
    }
}
