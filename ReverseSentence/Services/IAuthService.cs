using ReverseSentence.DTOs;

namespace ReverseSentence.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> AuthenticateAsync(string username, string password);
        string GenerateJwtToken(string username, string role);
    }
}
