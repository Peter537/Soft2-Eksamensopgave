using Microsoft.AspNetCore.Http;

namespace MToGo.Shared.Security;

public interface IUserContextAccessor
{
    IUserContext UserContext { get; }
}

public class UserContextAccessor : IUserContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IUserContext UserContext => 
        new UserContext(_httpContextAccessor.HttpContext?.User);
}
