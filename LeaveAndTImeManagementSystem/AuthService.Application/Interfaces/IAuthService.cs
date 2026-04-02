using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ApiResponseDto<LoginResponseDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponseDto<string>> LogoutAsync(string refreshToken);
        Task<ApiResponseDto<UserDto>> RegisterUserAsync(RegisterUserDto dto);
        Task<ApiResponseDto<string>> ForgotPasswordAsync(string email);
        Task<ApiResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> AdminExistsAsync();
    }
}