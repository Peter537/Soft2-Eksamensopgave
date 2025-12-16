using MToGo.ManagementService.Exceptions;
using MToGo.ManagementService.Models;
using MToGo.ManagementService.Repositories;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Password;

namespace MToGo.ManagementService.Services;

public interface IManagementService
{
    Task<ManagementLoginResponse> LoginAsync(ManagementLoginRequest request);
}

public class ManagementService : IManagementService
{
    private readonly IManagementUserRepository _managementUserRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;

    public ManagementService(
        IManagementUserRepository managementUserRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher)
    {
        _managementUserRepository = managementUserRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ManagementLoginResponse> LoginAsync(ManagementLoginRequest request)
    {
        var normalizedUsername = request.Username.ToLowerInvariant();
        var user = await _managementUserRepository.GetByUsernameAsync(normalizedUsername);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            throw new InvalidCredentialsException();
        }

        // Generate JWT token for management user
        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Username,
            UserRoles.Management,
            user.Name
        );

        return new ManagementLoginResponse { Jwt = token };
    }
}
