namespace MToGo.PartnerService.Exceptions;

public class PartnerNotFoundException : Exception
{
    public PartnerNotFoundException(string message) : base(message)
    {
    }
}
