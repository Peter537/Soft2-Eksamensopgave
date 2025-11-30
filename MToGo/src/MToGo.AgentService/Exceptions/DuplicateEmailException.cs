namespace MToGo.AgentService.Exceptions;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message) : base(message)
    {
    }
}
